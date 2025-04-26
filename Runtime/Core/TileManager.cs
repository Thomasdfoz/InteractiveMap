using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Gerencia a criação, posicionamento e reciclagem de tiles dentro de um container.
/// </summary>
[DisallowMultipleComponent]
public class TileManager : MonoBehaviour
{
    private GameObject m_tilePrefab;
    private int m_tileSize;
    private TileDownloader m_tileDownloader;
    private MapManager m_mapManager;
    private ObjectPool<GameObject> m_tilePool;
    private readonly Dictionary<string, GameObject> m_activeTiles = new Dictionary<string, GameObject>();

    /// <summary>
    /// Zoom atual do mapa.
    /// </summary>
    public float Zoom { get; set; }

    /// <summary>
    /// Latitude central usada para renderização.
    /// </summary>
    public double CenterLat { get; set; }

    /// <summary>
    /// Longitude central usada para renderização.
    /// </summary>
    public double CenterLon { get; set; }

    /// <summary>
    /// Raio de tiles ao redor do centro.
    /// </summary>
    public int Range { get; set; }

    /// <summary>
    /// Inicializa o TileManager com prefab, tamanho e serviço de tiles.
    /// </summary>
    public void Initialize(MapManager mapManager, GameObject tilePrefab, int tileSize, Sprite m_defaultSprite, TileDownloader tileDownloader)
    {
        m_tilePrefab = tilePrefab;
        m_tileSize = tileSize;
        m_tileDownloader = tileDownloader;
        m_mapManager = mapManager;

        m_tilePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(m_tilePrefab),
            actionOnGet: tile => tile.SetActive(true),
            actionOnRelease: tile => tile.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 100,
            maxSize: 500
        );
    }

    /// <summary>
    /// Renderiza tiles ao redor das coordenadas centrais.
    /// </summary>
    public void Render()
    {
        ReleaseAll();

        // Calcula o centro em pixels globais
        Vector2 centerPx = MapUtils.LatLonToPixels(CenterLat, CenterLon, Zoom, m_mapManager.MapSettings.TilePixelSize);


        // Calcula as coordenadas do tile central a partir do centro em pixels
        int centerTileX = (int)Math.Floor(centerPx.x / m_mapManager.MapSettings.TilePixelSize);
        int centerTileY = (int)Math.Floor(centerPx.y / m_mapManager.MapSettings.TilePixelSize);

        for (int dx = -Range; dx <= Range; dx++)
        {
            for (int dy = -Range; dy <= Range; dy++)
            {
                int x = centerTileX + dx;
                int y = centerTileY + dy;
                if (!MapUtils.IsValidTile(x, y, (int)Zoom)) continue;

                string key = $"{Zoom}/{x}/{y}";
                if (m_activeTiles.ContainsKey(key)) continue;

                GameObject go = m_tilePool.Get();
                go.transform.SetParent(transform, false);

                // Calcula a posição do tile em pixels globais
                double tilePxX = x * m_mapManager.MapSettings.TilePixelSize;
                double tilePxY = y * m_mapManager.MapSettings.TilePixelSize;

                // Calcula o offset em relação ao centro em pixels
                double offsetX = tilePxX - centerPx.x;
                double offsetY = -(tilePxY - centerPx.y); // Inverte Y para o sistema de coordenadas do Unity

                // Converte o offset para unidades do Unity
                float scale = m_tileSize / (float)m_mapManager.MapSettings.TilePixelSize;
                Vector2 position = new Vector2((float)offsetX * scale, (float)offsetY * scale);
                go.transform.localPosition = new Vector3(position.x, position.y, 0);

                m_activeTiles[key] = go;

                StartCoroutine(m_tileDownloader.DownloadTile(m_mapManager.MapSettings.URL, x, y, Zoom, tex =>
                    ApplyTexture(key, go, x, y, Zoom, tex)
                ));
            }
        }
    }

    private void ApplyTexture(string key, GameObject go, int x, int y, float zoom, Texture2D tex)
    {
        if (tex == null)
        {
            go.SetActive(false);
            return;
        }
        if (!m_activeTiles.TryGetValue(key, out GameObject active)) return;

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
        active.GetComponent<Tile>()?.SetTile(sprite);
    }

    private void ReleaseAll()
    {
        foreach (var go in m_activeTiles.Values)
            m_tilePool.Release(go);
        m_activeTiles.Clear();
    }
}
