using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orquestra a criação da UI, Input e renderização via TileManager.
/// </summary>
[DisallowMultipleComponent]
public class MapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Canvas que conterá o mapa")] private Canvas m_canvas;
    [SerializeField, Tooltip("Prefab do tile")] private GameObject m_tilePrefab;
    [SerializeField, Tooltip("Componente de download de tiles")] private TileDownloader m_downloader;
    [SerializeField, Tooltip("Componente de InputController para zoom/pan")] private InputController m_inputController;
    [SerializeField, Tooltip("Sprite padrão para preencher os tiles que não possuem textura do servidor")] private Sprite m_defaultSprite;

    [Header("Map Settings")]
    [SerializeField, Tooltip("Zoom inicial do mapa")] private int m_zoom = 2;
    [SerializeField, Tooltip("Raio de tiles renderizados")] private int m_range = 2;
    [SerializeField, Tooltip("Latitude central inicial")] private double m_centerLat = 0;
    [SerializeField, Tooltip("Longitude central inicial")] private double m_centerLon = 0;
    [SerializeField, Tooltip("Tamanho do tile em pixels")] private int m_tileSize = 256;

    private TileManager m_tileManager;

    /// <summary>
    /// Zoom atual do mapa.
    /// </summary>
    public int Zoom { get => m_zoom; set => m_zoom = value; }

    /// <summary>
    /// Latitude central do mapa.
    /// </summary>
    public double CenterLat { get => m_centerLat; set => m_centerLat = value; }

    /// <summary>
    /// Longitude central do mapa.
    /// </summary>
    public double CenterLon { get => m_centerLon; set => m_centerLon = value; }

    /// <summary>
    /// Raio de tiles.
    /// </summary>
    public int Range { get => m_range; set => m_range = value; }

    private void Awake()
    {
        // 1) Background UI
        CreateBackground();
        // 2) Container para tiles (e TileManager)
        var container = CreateMapContainer();

        // 3) Configura tile service e manager
        ITileService service = new TileDownloaderService(m_downloader);
        m_tileManager = container.AddComponent<TileManager>();
        m_tileManager.Initialize(m_tilePrefab, m_tileSize, m_defaultSprite, service);
        m_tileManager.Zoom = m_zoom;
        m_tileManager.CenterLat = m_centerLat;
        m_tileManager.CenterLon = m_centerLon;
        m_tileManager.Range = m_range;

        // 4) Inicializa InputController
        m_inputController.Initialize(this);
    }

    private void Start() => RenderMap();

    /// <summary>
    /// Renderiza o mapa com configurações atuais.
    /// </summary>
    public void RenderMap()
    {
        m_tileManager.Zoom = m_zoom;
        m_tileManager.CenterLat = m_centerLat;
        m_tileManager.CenterLon = m_centerLon;
        m_tileManager.Range = m_range;
        m_tileManager.Render();
    }

    private void CreateBackground()
    {
        GameObject bgGO = new GameObject("Background");
        Image img = bgGO.AddComponent<Image>();
        img.sprite = m_defaultSprite;
        img.transform.SetParent(m_canvas.transform, false);
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-500, -500);
        rt.offsetMax = new Vector2(500, 500);
    }

    private GameObject CreateMapContainer()
    {
        GameObject go = new GameObject("MapContent");
        go.transform.SetParent(m_canvas.transform, false);
        return go;
    }
}