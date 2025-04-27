using UnityEngine;
using UnityEngine.UI;

public class GlobalManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Canvas que conter� o mapa")] private Canvas m_canvas;
    [SerializeField, Tooltip("Prefab do tile")] private GameObject m_tilePrefab;
    [SerializeField, Tooltip("Componente de download de tiles")] private TileDownloader m_downloader;
    [SerializeField, Tooltip("Componente de InputController para zoom/pan")] private InputController m_inputController;
    [SerializeField, Tooltip("Texture padrao para preencher os tiles que n�o possuem textura do servidor")] private Texture2D m_defaultTexture;

    [Header("Map Settings")]
    [SerializeField, Tooltip("Tamanho do tile em pixels")] private int m_tileSize;
    [SerializeField] private RectTransform m_centerReference;
    [SerializeField] private MapConfig m_mapGlobal;
    [SerializeField] private MapConfig[] m_maps;

    private PinManager m_pinManager;
    private MapManager m_mapManagerGlobal;
    private double m_centerLon;
    private double m_centerLat;
    private float m_zoom;

    /// <summary>
    /// Zoom atual do mapa.
    /// </summary>
    public float Zoom { get => m_zoom; set => m_zoom = value; }

    /// <summary>
    /// Latitude central do mapa.
    /// </summary>
    public double CenterLat { get => m_centerLat; set => m_centerLat = value; }

    /// <summary>
    /// Longitude central do mapa.
    /// </summary>
    public double CenterLon { get => m_centerLon; set => m_centerLon = value; }

    public Transform MapGlobalContentTransform => m_mapManagerGlobal.MapContent.transform;
    public RectTransform CenterReference { get => m_centerReference;}

    public int TilePixelSize => m_mapGlobal.TilePixelSize;
    public int TileSize => m_tileSize;


    private void Awake()
    {
        m_centerLat = m_mapGlobal.CenterLat;
        m_centerLon = m_mapGlobal.CenterLon;
        m_zoom = m_mapGlobal.Zoom;

        CreateBackground();

        m_mapManagerGlobal = CreateMapContent(m_mapGlobal.name, m_canvas.transform);
        m_mapManagerGlobal.Initialize(this, m_tilePrefab, m_tileSize, m_defaultTexture, m_downloader, m_mapGlobal);

        m_pinManager = m_mapManagerGlobal.gameObject.AddComponent<PinManager>();
        m_pinManager.Initialize(this);

        foreach (var map in m_maps)
        {
            MapManager mapManager = CreateMapContent(map.name, MapGlobalContentTransform);          
            AddPin(mapManager.gameObject, map.CenterLat, CenterLon);
            mapManager.Initialize(this, m_tilePrefab, m_tileSize, m_defaultTexture, m_downloader, map);
        }

        RenderMap();

        m_inputController.Initialize(this);
    }
    /// <summary>
    /// Adiciona um pin no mapa com coordenadas específicas.
    /// </summary>
    public void AddPin(GameObject pinPrefab, double lat, double lon)
    {       
        m_pinManager.AddPin(pinPrefab, lat, lon);
    }

    public void RenderMap()
    {
        m_mapManagerGlobal.RenderMap();
        m_pinManager.UpdateAllPins();
    }
   
    private MapManager CreateMapContent(string name, Transform parent)
    {
        GameObject bgGO = new GameObject(name);
        bgGO.transform.SetParent(parent, false);
        RectTransform rt = bgGO.AddComponent<RectTransform>();
        MapManager mapManager = bgGO.AddComponent<MapManager>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-500, -500);
        rt.offsetMax = new Vector2(500, 500);

        return mapManager;
    }
    private void CreateBackground()
    {
        GameObject bgGO = new GameObject("Background");
        Image img = bgGO.AddComponent<Image>();
        Sprite sprite = Sprite.Create(m_defaultTexture, new Rect(0, 0, m_defaultTexture.width, m_defaultTexture.height), new Vector2(0.5f, 0.5f), 100);
        img.sprite = sprite;
        img.transform.SetParent(m_canvas.transform, false);
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-500, -500);
        rt.offsetMax = new Vector2(500, 500);
    }
}
