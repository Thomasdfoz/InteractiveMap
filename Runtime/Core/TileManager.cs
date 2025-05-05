using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EGS.Tile;
using EGS.Util;

namespace EGS.Core
{
    /// <summary>
    /// Gerencia a cria��o, posicionamento, cache e pr�-carregamento de tiles dentro de um container.
    /// </summary>
    [DisallowMultipleComponent]
    public class TileManager : MonoBehaviour
    {
        [Header("Cache & Performance")]
        [Tooltip("Quantos tiles al�m do Range devem ser mantidos carregados para evitar �reas brancas.")]
        [SerializeField] private int bufferMargin = 1;

        [Tooltip("Quantos n�veis de zoom acima/abaixo devem ser pr�-carregados.")]
        [SerializeField] private int zoomBuffer = 2;

        private TileRenderer m_tilePrefab;
        private int m_tileSize;
        private TileDownloader m_tileDownloader;
        private MapManager m_mapManager;
        private ObjectPool<TileRenderer> m_tilePool;

        // Tiles ativos na cena (key -> GameObject)
        private readonly Dictionary<string, TileRenderer> m_activeTiles = new Dictionary<string, TileRenderer>();
        // Cache de texturas baixadas (key -> Texture2D)
        private readonly Dictionary<string, Texture2D> m_textureCache = new Dictionary<string, Texture2D>();
        // Conjunto de chaves vis�veis no frame anterior (para zoom atual)
        private readonly HashSet<string> m_prevVisible = new HashSet<string>();

        /// <summary>Zoom atual do mapa.</summary>
        public float Zoom { get; set; }
        /// <summary>Latitude central usada para renderiza��o.</summary>
        public double CenterLat { get; set; }
        /// <summary>Longitude central usada para renderiza��o.</summary>
        public double CenterLon { get; set; }
        /// <summary>Raio de tiles ao redor do centro.</summary>
        public int Range { get; set; }

        // Contador de downloads pendentes (vis�veis e pr�-carregados)
        private int m_pendingDownloads = 0;

        /// <summary>
        /// Indica se todos os downloads de tiles (vis�veis e pr�-carregados) foram conclu�dos.
        /// </summary>
        public bool AllDownloadsComplete => m_pendingDownloads == 0;

        /// <summary>
        /// Inicializa o TileManager com prefab, tamanho, textura default e servi�o de tiles.
        /// </summary>
        public void Initialize(MapManager mapManager,TileRenderer tilePrefab,int tileSize,Texture2D defaultTexture,TileDownloader tileDownloader)
        {
            m_tilePrefab = tilePrefab;
            m_tileSize = tileSize;
            m_tileDownloader = tileDownloader;
            m_mapManager = mapManager;

            m_tilePool = new ObjectPool<TileRenderer>(
                createFunc: () => Instantiate(m_tilePrefab),
                actionOnGet: tile =>
                {
                    var img = tile.GetComponent<RawImage>();
                    if (img != null) img.texture = defaultTexture;
                    tile.gameObject.SetActive(true);
                },
                actionOnRelease: tile => tile.gameObject.SetActive(false),
                actionOnDestroy: Destroy,
                collectionCheck: false,
                defaultCapacity: 50,
                maxSize: 200
            );
        }

        /// <summary>
        /// Renderiza o mapa no zoom atual e pr�-carrega tiles de zoom adjacentes.
        /// </summary>
        public void Render()
        {
            int currentZoom = Mathf.RoundToInt(Zoom);
            int minZoom = Mathf.RoundToInt(m_mapManager.MapSettings.ZoomMin);
            int maxZoom = Mathf.RoundToInt(m_mapManager.MapSettings.ZoomMax);

            // 1) Renderiza tiles do zoom atual
            var visibleNow = GetVisibleKeySet(currentZoom);
            foreach (var key in visibleNow)
            {
                // key = "{zoom}/{x}/{y}"
                var parts = key.Split('/');
                int z = int.Parse(parts[0]);
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);

                // Calcula posi��o local
                Vector2 centerPx = MapUtils.LatLonToPixels(
                    CenterLat,
                    CenterLon,
                    z,
                    m_mapManager.MapSettings.TilePixelSize
                );
                double half = m_mapManager.MapSettings.TilePixelSize * 0.5;
                double tilePxX = x * m_mapManager.MapSettings.TilePixelSize + half;
                double tilePxY = y * m_mapManager.MapSettings.TilePixelSize + half;
                double offsetX = tilePxX - centerPx.x;
                double offsetY = -(tilePxY - centerPx.y);
                double scale = (double)m_tileSize / m_mapManager.MapSettings.TilePixelSize;
                Vector3 localPos = new Vector3(
                    (float)(offsetX * scale),
                    (float)(offsetY * scale),
                    0f
                );

                if (m_activeTiles.TryGetValue(key, out var tileGO))
                {
                    tileGO.transform.localPosition = localPos;
                }
                else
                {
                    // Instancia novo tile
                    TileRenderer go = m_tilePool.Get();
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = localPos;
                    m_activeTiles[key] = go;
                    m_prevVisible.Add(key);

                    m_pendingDownloads++;
                    if (m_textureCache.TryGetValue(key, out var cached))
                    {
                        ApplyTexture(go, cached);
                        m_pendingDownloads--;
                    }
                    else
                    {
                        // Download e cache
                        StartCoroutine(
                            m_tileDownloader.DownloadTile(
                                m_mapManager.MapSettings.URL,
                                x, y, z,
                                tex =>
                                {
                                    if (tex != null)
                                        m_textureCache[key] = tex;
                                    if (m_activeTiles.ContainsKey(key))
                                        go.GetComponent<TileRenderer>()?.SetTile(tex);
                                    m_pendingDownloads--;
                                }
                            )
                        );
                    }
                }
            }

            // 2) Remove tiles que sa�ram da vis�o no zoom atual
            foreach (var oldKey in m_prevVisible.Except(visibleNow).ToList())
            {
                if (m_activeTiles.TryGetValue(oldKey, out var oldGO))
                {
                    m_tilePool.Release(oldGO);
                    m_activeTiles.Remove(oldKey);
                }
                m_prevVisible.Remove(oldKey);
            }

            // 3) Pr�-carrega tiles para n�veis de zoom adjacentes
            for (int dz = -zoomBuffer; dz <= zoomBuffer; dz++)
            {
                if (dz == 0) continue;
                int z = currentZoom + dz;
                if (z < minZoom || z > maxZoom) continue;
                PrefetchZoom(z);
            }
        }

        /// <summary>
        /// Gera o conjunto de chaves "zoom/x/y" vis�veis no mapa para um dado n�vel de zoom.
        /// </summary>
        private HashSet<string> GetVisibleKeySet(int zoom)
        {
            var keys = new HashSet<string>();
            Vector2 centerPx = MapUtils.LatLonToPixels(
                CenterLat,
                CenterLon,
                zoom,
                m_mapManager.MapSettings.TilePixelSize
            );
            int centerX = Mathf.FloorToInt(centerPx.x / m_mapManager.MapSettings.TilePixelSize);
            int centerY = Mathf.FloorToInt(centerPx.y / m_mapManager.MapSettings.TilePixelSize);
            int totalRadius = Range + bufferMargin;

            for (int dx = -totalRadius; dx <= totalRadius; dx++)
                for (int dy = -totalRadius; dy <= totalRadius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (!MapUtils.IsValidTile(x, y, zoom)) continue;
                    keys.Add($"{zoom}/{x}/{y}");
                }
            return keys;
        }

        /// <summary>
        /// Inicia o download em background para preencher cache de um n�vel de zoom.
        /// </summary>
        private void PrefetchZoom(int zoom)
        {
            Vector2 centerPx = MapUtils.LatLonToPixels(
                CenterLat,
                CenterLon,
                zoom,
                m_mapManager.MapSettings.TilePixelSize
            );
            int centerX = Mathf.FloorToInt(centerPx.x / m_mapManager.MapSettings.TilePixelSize);
            int centerY = Mathf.FloorToInt(centerPx.y / m_mapManager.MapSettings.TilePixelSize);
            int totalRadius = Range + bufferMargin;

            for (int dx = -totalRadius; dx <= totalRadius; dx++)
                for (int dy = -totalRadius; dy <= totalRadius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (!MapUtils.IsValidTile(x, y, zoom)) continue;
                    string key = $"{zoom}/{x}/{y}";
                    if (m_textureCache.ContainsKey(key)) continue;

                    m_pendingDownloads++;
                    StartCoroutine(
                        m_tileDownloader.DownloadTile(
                            m_mapManager.MapSettings.URL,
                            x, y, zoom,
                            tex =>
                            {
                                if (tex != null)
                                    m_textureCache[key] = tex;
                                m_pendingDownloads--;
                            }
                        )
                    );
                }
        }

        /// <summary>
        /// Aplica textura baixada no RawImage do TileRenderer.
        /// </summary>
        private void ApplyTexture(TileRenderer go, Texture2D tex)
        {
            if (tex == null) return;
            go.SetTile(tex);
        }

        /// <summary>
        /// Libera todos os tiles ativos e limpa o hist�rico de visibilidade.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var go in m_activeTiles.Values)
                m_tilePool.Release(go);
            m_activeTiles.Clear();
            m_prevVisible.Clear();
        }
    }
}