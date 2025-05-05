using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EGS.Tile;
using EGS.Util;

namespace EGS.Core
{
    [DisallowMultipleComponent]
    public class TileManager : MonoBehaviour
    {
        private GameObject m_tilePrefab;
        private int m_tileSize;
        private TileDownloader m_tileDownloader;
        private MapManager m_mapManager;

        private readonly Dictionary<string, GameObject> m_activeTiles = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Texture2D> m_textureCache = new Dictionary<string, Texture2D>();
        private readonly HashSet<string> m_prevVisible = new HashSet<string>();

        public float Zoom { get; set; }
        public double CenterLat { get; set; }
        public double CenterLon { get; set; }
        public int Range { get; set; }

        private int m_pendingDownloads = 0;

        public bool AllDownloadsComplete => m_pendingDownloads == 0;

        public void Initialize(MapManager mapManager, GameObject tilePrefab, int tileSize, Texture2D defaultTexture, TileDownloader tileDownloader)
        {
            m_tilePrefab = tilePrefab;
            m_tileSize = tileSize;
            m_tileDownloader = tileDownloader;
            m_mapManager = mapManager;
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

                Vector2 centerPx = MapUtils.LatLonToPixels(CenterLat, CenterLon, z, m_mapManager.MapSettings.TilePixelSize);
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
                    GameObject go = Instantiate(m_tilePrefab, transform, false);
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

            foreach (var oldKey in m_prevVisible.Except(visibleNow).ToList())
            {
                if (m_activeTiles.TryGetValue(oldKey, out var oldGO))
                {
                    Destroy(oldGO);
                    m_activeTiles.Remove(oldKey);
                }
                m_prevVisible.Remove(oldKey);
            }

            for (int dz = -m_mapManager.MapSettings.ZoomBuffer; dz <= m_mapManager.MapSettings.ZoomBuffer; dz++)
            {
                if (dz == 0) continue;
                int z = currentZoom + dz;
                if (z < minZoom || z > maxZoom) continue;
                PrefetchZoom(z);
            }
        }

        private HashSet<string> GetVisibleKeySet(int zoom)
        {
            var keys = new HashSet<string>();
            Vector2 centerPx = MapUtils.LatLonToPixels(CenterLat, CenterLon, zoom, m_mapManager.MapSettings.TilePixelSize);
            int centerX = Mathf.FloorToInt(centerPx.x / m_mapManager.MapSettings.TilePixelSize);
            int centerY = Mathf.FloorToInt(centerPx.y / m_mapManager.MapSettings.TilePixelSize);
            int totalRadius = Range + m_mapManager.MapSettings.BufferMargin;

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
            Vector2 centerPx = MapUtils.LatLonToPixels(CenterLat, CenterLon, zoom, m_mapManager.MapSettings.TilePixelSize);
            int centerX = Mathf.FloorToInt(centerPx.x / m_mapManager.MapSettings.TilePixelSize);
            int centerY = Mathf.FloorToInt(centerPx.y / m_mapManager.MapSettings.TilePixelSize);
            int totalRadius = Range + m_mapManager.MapSettings.BufferMargin;

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

        private void ApplyTexture(GameObject go, Texture2D tex)
        {
            if (tex == null) return;
            go.GetComponent<TileRenderer>()?.SetTile(tex);
        }

        public void ReleaseAll()
        {
            foreach (var go in m_activeTiles.Values)
                Destroy(go);
            m_activeTiles.Clear();
            m_prevVisible.Clear();
        }
    }
}
