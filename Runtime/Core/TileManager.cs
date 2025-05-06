using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using EGS.Tile;
using EGS.Util;
using System.Collections;
using System;

namespace EGS.Core
{
    [DisallowMultipleComponent]
    public class TileManager : MonoBehaviour
    {
        [Header("Cache & Performance")]
        [Tooltip("Quantos tiles além do Range devem ser mantidos carregados para evitar áreas brancas.")]
        [SerializeField] private int bufferMargin = 2;

        //[Tooltip("Quantos níveis de zoom acima/abaixo devem ser pré-carregados.")]
        //[SerializeField] private int zoomBuffer = 2;

        [Tooltip("Limite de texturas em cache (evita consumir memória demais)")]
        [SerializeField] private int textureCacheLimit = 2500;

        private TileRenderer m_tilePrefab;
        private int m_tileSize;
        private TileDownloader m_tileDownloader;
        private MapManager m_mapManager;
        private ObjectPool<TileRenderer> m_tilePool;

        private readonly Dictionary<string, TileRenderer> m_activeTiles = new Dictionary<string, TileRenderer>();
        private readonly Dictionary<string, Texture2D> m_textureCache = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> m_pendingTextures = new();
        private readonly Queue<string> m_cacheQueue = new Queue<string>(); // Para FIFO do cache
        private readonly HashSet<string> m_prevVisible = new HashSet<string>();

        public float Zoom { get; set; }
        public double CenterLat { get; set; }
        public double CenterLon { get; set; }
        public int Range { get; set; }

        private int m_pendingDownloads = 0;
        public bool AllDownloadsComplete => m_pendingDownloads == 0;
        private bool m_isZoomRendering = false;

        public event Action OnZoomRenderingFinish;

        public void Initialize(MapManager mapManager, TileRenderer tilePrefab, int tileSize, Texture2D defaultTexture)
        {
            m_tilePrefab = tilePrefab;
            m_tileSize = tileSize;
            m_tileDownloader = new TileDownloader();
            m_mapManager = mapManager;

            m_tilePool = new ObjectPool<TileRenderer>(
                createFunc: () => Instantiate(m_tilePrefab),
                actionOnGet: tile =>
                {
                    var img = tile.GetComponent<RawImage>();
                    if (img != null) img.texture = defaultTexture;
                    tile.gameObject.SetActive(false);
                },
                actionOnRelease: tile => tile.gameObject.SetActive(false),
                actionOnDestroy: Destroy,
                collectionCheck: false,
                defaultCapacity: 50,
                maxSize: 200
            );
        }

        public void RenderBatchZoom()
        {
            StartCoroutine(RenderTilesAfterBatchDownload());
        }

        private IEnumerator RenderTilesAfterBatchDownload()
        {
            float startTime = Time.realtimeSinceStartup;
            int currentZoom = Mathf.RoundToInt(Zoom);
            int minZoom = Mathf.RoundToInt(m_mapManager.MapSettings.ZoomMin);
            int maxZoom = Mathf.RoundToInt(m_mapManager.MapSettings.ZoomMax);

            var visibleNow = GetVisibleKeySet(currentZoom);
            var downloadsInProgress = new List<IEnumerator>();

            m_pendingTextures.Clear();

            // Fase 1: Inicia download para todos os tiles visíveis
            foreach (var key in visibleNow)
            {
                var parts = key.Split('/');
                int z = int.Parse(parts[0]);
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);

                if (!m_activeTiles.TryGetValue(key, out var tileGO))
                {
                    tileGO = m_tilePool.Get();
                    tileGO.transform.SetParent(transform, false);
                    m_activeTiles[key] = tileGO;
                }

                Vector2 centerPx = MapUtils.LatLonToPixels(CenterLat, CenterLon, z, m_mapManager.MapSettings.TilePixelSize);
                double half = m_mapManager.MapSettings.TilePixelSize * 0.5;
                double tilePxX = x * m_mapManager.MapSettings.TilePixelSize + half;
                double tilePxY = y * m_mapManager.MapSettings.TilePixelSize + half;
                double offsetX = tilePxX - centerPx.x;
                double offsetY = -(tilePxY - centerPx.y);
                double scale = (double)m_tileSize / m_mapManager.MapSettings.TilePixelSize;
                tileGO.transform.localPosition = new Vector3((float)(offsetX * scale), (float)(offsetY * scale), 0f);

                tileGO.gameObject.SetActive(false); // ainda não mostra

                if (m_textureCache.TryGetValue(key, out var cached))
                {
                    m_pendingTextures[key] = cached;
                }
                else
                {
                    m_pendingDownloads++;
                    StartCoroutine(m_tileDownloader.DownloadTile(
                        m_mapManager.MapSettings.URL,
                        x, y, z,
                        tex =>
                        {
                            if (tex != null)
                            {
                                m_textureCache[key] = tex;
                                m_cacheQueue.Enqueue(key);
                                TrimCacheIfNeeded();
                                m_pendingTextures[key] = tex;
                            }
                            m_pendingDownloads--;
                        }
                    ));
                }
            }

            // Fase 2: Espera todos os downloads terminarem
            while (m_pendingDownloads > 0)
                yield return null;

            float duration = Time.realtimeSinceStartup - startTime;

            if (duration < 1.5f)
                yield return new WaitForSeconds(1.5f - duration);

            // Fase 3: Remove tiles que saíram da visão
            foreach (var oldKey in m_prevVisible.Except(visibleNow).ToList())
            {
                if (m_activeTiles.TryGetValue(oldKey, out var oldGO))
                {
                    m_tilePool.Release(oldGO);
                    m_activeTiles.Remove(oldKey);
                }
                m_prevVisible.Remove(oldKey);
            }

            // Fase 4: Aplica as texturas e ativa os tiles
            foreach (var key in visibleNow)
            {
                if (m_activeTiles.TryGetValue(key, out var go) && m_pendingTextures.TryGetValue(key, out var tex))
                {
                    ApplyTexture(go, tex); // aqui o tile é ativado dentro do método
                }
                m_prevVisible.Add(key);
            }
            // espera o tempo mínimo para suavidade
            OnZoomRenderingFinish?.Invoke();

            //// Fase 5: Pré-carrega outros zooms (opcional, pode mover para o fim ou deixar fora se quiser suavidade total)
            //for (int dz = -zoomBuffer; dz <= zoomBuffer; dz++)
            //{
            //    if (dz == 0) continue;
            //    int z = currentZoom + dz;
            //    if (z < minZoom || z > maxZoom) continue;
            //    PrefetchZoom(z);
            //}
        }
        public void Render()
        {
            int currentZoom = Mathf.RoundToInt(Zoom);
            int minZoom = Mathf.RoundToInt(m_mapManager.MapSettings.ZoomMin);
            int maxZoom = Mathf.RoundToInt(m_mapManager.MapSettings.ZoomMax);

            var visibleNow = GetVisibleKeySet(currentZoom);

            foreach (var key in visibleNow)
            {
                var parts = key.Split('/');
                int z = int.Parse(parts[0]);
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);

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
                        StartCoroutine(
                            m_tileDownloader.DownloadTile(
                                m_mapManager.MapSettings.URL,
                                x, y, z,
                                tex =>
                                {
                                    if (tex == null)
                                    {
                                        Debug.LogWarning($"Tile download failed: {key}");
                                    }
                                    else
                                    {
                                        m_textureCache[key] = tex;
                                        m_cacheQueue.Enqueue(key);
                                        TrimCacheIfNeeded();
                                    }

                                    if (m_activeTiles.TryGetValue(key, out var t))
                                        ApplyTexture(t, tex);

                                    m_pendingDownloads--;
                                }
                            )
                        );
                    }
                }
            }

            foreach (var oldKey in m_prevVisible.Except(visibleNow).ToList())
            {
                if (m_activeTiles.TryGetValue(oldKey, out var oldGO))
                {
                    m_tilePool.Release(oldGO);
                    m_activeTiles.Remove(oldKey);
                }
                m_prevVisible.Remove(oldKey);
            }

            //for (int dz = -zoomBuffer; dz <= zoomBuffer; dz++)
            //{
            //    if (dz == 0) continue;
            //    int z = currentZoom + dz;
            //    if (z < minZoom || z > maxZoom) continue;
            //    PrefetchZoom(z);
            //}
        }

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
                                if (tex == null)
                                {
                                    Debug.LogWarning($"Prefetch failed: {key}");
                                }
                                else
                                {
                                    m_textureCache[key] = tex;
                                    m_cacheQueue.Enqueue(key);
                                    TrimCacheIfNeeded();
                                }
                                m_pendingDownloads--;
                            }
                        )
                    );
                }
        }

        private void TrimCacheIfNeeded()
        {
            while (m_cacheQueue.Count > textureCacheLimit)
            {
                var oldKey = m_cacheQueue.Dequeue();
                if (m_textureCache.ContainsKey(oldKey))
                    m_textureCache.Remove(oldKey);
            }
        }

        private void ApplyTexture(TileRenderer go, Texture2D tex)
        {
            if (go == null || tex == null) return;
            go.SetTile(tex);
            go.gameObject.SetActive(true);
        }

        public void ReleaseAll()
        {
            foreach (var go in m_activeTiles.Values)
                m_tilePool.Release(go);
            m_activeTiles.Clear();
            m_prevVisible.Clear();
        }
    }
}
