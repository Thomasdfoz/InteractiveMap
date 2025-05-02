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
    /// Gerencia a criação, posicionamento e reciclagem de tiles dentro de um container.
    /// Agora com cache em memória, diff incremental, reposicionamento de tiles existentes
    /// e buffer de margem para pan/zoom suaves.
    /// </summary>
    [DisallowMultipleComponent]
    public class TileManager : MonoBehaviour
    {
        [Header("Cache & Performance")]
        [Tooltip("Quantos tiles além do Range devem ser mantidos carregados para evitar áreas brancas.")]
        [SerializeField] private int bufferMargin = 1;

        private GameObject m_tilePrefab;
        private int m_tileSize;
        private TileDownloader m_tileDownloader;
        private MapManager m_mapManager;
        private ObjectPool<GameObject> m_tilePool;

        // Tiles ativos na cena (key -> GameObject)
        private readonly Dictionary<string, GameObject> m_activeTiles = new Dictionary<string, GameObject>();
        // Cache de texturas baixadas (key -> Texture2D)
        private readonly Dictionary<string, Texture2D> m_textureCache = new Dictionary<string, Texture2D>();
        // Conjunto de tiles visíveis no frame anterior
        private readonly HashSet<string> m_prevVisible = new HashSet<string>();

        /// <summary>Zoom atual do mapa.</summary>
        public float Zoom { get; set; }
        /// <summary>Latitude central usada para renderização.</summary>
        public double CenterLat { get; set; }
        /// <summary>Longitude central usada para renderização.</summary>
        public double CenterLon { get; set; }
        /// <summary>Raio de tiles ao redor do centro (nível de detalhe).</summary>
        public int Range { get; set; }

        /// <summary>
        /// Inicializa o TileManager com prefab, tamanho, textura default e serviço de tiles.
        /// </summary>
        public void Initialize(
            MapManager mapManager,
            GameObject tilePrefab,
            int tileSize,
            Texture2D defaultTexture,
            TileDownloader tileDownloader)
        {
            m_tilePrefab = tilePrefab;
            m_tileSize = tileSize;
            m_tileDownloader = tileDownloader;
            m_mapManager = mapManager;

            m_tilePool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(m_tilePrefab),
                actionOnGet: tile =>
                {
                    var img = tile.GetComponent<RawImage>();
                    if (img != null) img.texture = defaultTexture;
                    tile.SetActive(true);
                },
                actionOnRelease: tile => tile.SetActive(false),
                actionOnDestroy: Destroy,
                collectionCheck: false,
                defaultCapacity: 100,
                maxSize: 500
            );
        }

        /// <summary>
        /// Renderiza tiles ao redor das coordenadas centrais, reposicionando existentes
        /// e carregando só novos, removendo apenas os que saíram da visão.
        /// </summary>
        public void Render()
        {
            // Centro em pixels globais
            Vector2 centerPx = MapUtils.LatLonToPixels(
                CenterLat,
                CenterLon,
                Zoom,
                m_mapManager.MapSettings.TilePixelSize
            );

            int centerTileX = Mathf.FloorToInt(centerPx.x / m_mapManager.MapSettings.TilePixelSize);
            int centerTileY = Mathf.FloorToInt(centerPx.y / m_mapManager.MapSettings.TilePixelSize);

            int totalRadius = Range + bufferMargin;
            var newVisible = new HashSet<string>();

            for (int dx = -totalRadius; dx <= totalRadius; dx++)
            {
                for (int dy = -totalRadius; dy <= totalRadius; dy++)
                {
                    int x = centerTileX + dx;
                    int y = centerTileY + dy;
                    if (!MapUtils.IsValidTile(x, y, (int)Zoom)) continue;

                    string key = $"{Zoom}/{x}/{y}";
                    newVisible.Add(key);

                    // Calcula posição local
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
                        // Reposiciona tile existente
                        tileGO.transform.localPosition = localPos;
                    }
                    else
                    {
                        // Instancia e posiciona novo tile
                        GameObject newGO = m_tilePool.Get();
                        newGO.transform.SetParent(transform, false);
                        newGO.transform.localPosition = localPos;
                        m_activeTiles[key] = newGO;
                        m_prevVisible.Add(key);

                        // Aplica cache ou dispara download
                        if (m_textureCache.TryGetValue(key, out var cachedTex))
                        {
                            ApplyTexture(newGO, cachedTex);
                        }
                        else
                        {
                            StartCoroutine(
                                m_tileDownloader.DownloadTile(
                                    m_mapManager.MapSettings.URL,
                                    x, y, Zoom,
                                    tex =>
                                    {
                                        if (tex != null)
                                            m_textureCache[key] = tex;

                                        // só aplica se:
                                        // 1) essa chave ainda estiver visível
                                        // 2) ainda existir um GameObject associado a ela
                                        if (newVisible.Contains(key) && m_activeTiles.TryGetValue(key, out var currentGO) && currentGO != null)
                                        {
                                            // usa diretamente o TileRenderer, sem capturar o go antigo
                                            currentGO.GetComponent<TileRenderer>()?.SetTile(tex);
                                        }
                                    }
                                    )
                                );
                        }
                    }
                }
            }

            // Remove tiles fora da visão
            foreach (var oldKey in m_prevVisible.Except(newVisible).ToList())
            {
                if (m_activeTiles.TryGetValue(oldKey, out var oldGO))
                {
                    m_tilePool.Release(oldGO);
                    m_activeTiles.Remove(oldKey);
                }
                m_prevVisible.Remove(oldKey);
            }
        }

        /// <summary>Aplica a textura baixada no TileRenderer.</summary>
        private void ApplyTexture(GameObject go, Texture2D tex)
        {
            if (tex == null) return;
            go.GetComponent<TileRenderer>()?.SetTile(tex);
        }

        /// <summary>Libera todos os tiles e limpa o histórico de visibilidade.</summary>
        public void ReleaseAll()
        {
            foreach (var go in m_activeTiles.Values)
                m_tilePool.Release(go);
            m_activeTiles.Clear();
            m_prevVisible.Clear();
        }
    }
}
