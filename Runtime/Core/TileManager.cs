using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerencia a criação, posicionamento e reciclagem de tiles dentro de um container.
/// </summary>
[DisallowMultipleComponent]
public class TileManager : MonoBehaviour
{
    private GameObject m_tilePrefab;
    private int m_tileSize;
    private Sprite m_defaultSprite;
    private ITileService m_tileService;
    private ObjectPool<GameObject> m_tilePool;
    private readonly Dictionary<string, GameObject> m_activeTiles = new Dictionary<string, GameObject>();

    /// <summary>
    /// Zoom atual do mapa.
    /// </summary>
    public int Zoom { get; set; }

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
    public void Initialize(GameObject tilePrefab, int tileSize, Sprite m_defaultSprite, ITileService tileService)
    {
        m_tilePrefab = tilePrefab;
        m_tileSize = tileSize;
        m_tileService = tileService;

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
        Vector2Int center = MapUtils.LatLonToTile(CenterLat, CenterLon, Zoom);

        for (int dx = -Range; dx <= Range; dx++)
            for (int dy = -Range; dy <= Range; dy++)
            {
                int x = center.x + dx;
                int y = center.y + dy;
                if (!MapUtils.IsValidTile(x, y, Zoom)) continue;

                string key = $"{Zoom}/{x}/{y}";
                if (m_activeTiles.ContainsKey(key)) continue;

                GameObject go = m_tilePool.Get();
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(dx * m_tileSize, -dy * m_tileSize, 0);
                m_activeTiles[key] = go;

                StartCoroutine(m_tileService.DownloadTile(x, y, Zoom, tex =>
                    ApplyTexture(key, go, x, y, Zoom, tex)
                ));
            }
    }

    private void ApplyTexture(string key, GameObject go, int x, int y, int zoom, Texture2D tex)
    {
        if (!m_activeTiles.TryGetValue(key, out GameObject active)) return; 

        /*if (tex == null)
        {
            active.GetComponent<Tile>()?.SetTile(m_defaultSprite);
            return;
        }*/
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
