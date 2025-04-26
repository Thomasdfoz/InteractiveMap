using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.RayTracingAccelerationStructure;

/// <summary>
/// Orquestra a criação da UI, Input e renderização via TileManager.
/// </summary>
[DisallowMultipleComponent]
public class MapManager : MonoBehaviour
{
    private TileManager m_tileManager;
    private GameObject m_mapContent;
    private MapConfig mapSettings;
    private GlobalManager m_globalManager;

    /// <summary>
    /// Raio de tiles.
    /// </summary>
    public GameObject MapContent { get => m_mapContent; }
    public MapConfig MapSettings { get => mapSettings; }

    public void Initialize(GlobalManager globalManager, Transform Contentparent,GameObject tileprefab, int tileSize, Sprite m_defaultSprite, TileDownloader m_downloader, MapConfig settings)
    {
        mapSettings = settings;
        m_globalManager = globalManager;
        // 1) Container para tiles (e TileManager)
        m_mapContent = CreateMapContainer(Contentparent);

        m_tileManager = m_mapContent.AddComponent<TileManager>();
        m_tileManager.Initialize(this, tileprefab, tileSize, m_defaultSprite, m_downloader);
        m_tileManager.Zoom = settings.Zoom;
        m_tileManager.CenterLat = settings.CenterLat;
        m_tileManager.CenterLon = settings.CenterLon;
        m_tileManager.Range = settings.Range;

    }

    /// <summary>
    /// Renderiza o mapa com configurações atuais.
    /// </summary>
    public void RenderMap()
    {
        m_tileManager.Zoom = m_globalManager.Zoom;
        m_tileManager.CenterLat = m_globalManager.CenterLat;
        m_tileManager.CenterLon = m_globalManager.CenterLon;
        m_tileManager.Range = mapSettings.Range;
        m_tileManager.Render();
    }
   

    private GameObject CreateMapContainer(Transform parent)
    {
        GameObject go = new GameObject("MapContent");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localPosition = Vector3.zero; // Posição inicial no centro
        return go;
    }
}