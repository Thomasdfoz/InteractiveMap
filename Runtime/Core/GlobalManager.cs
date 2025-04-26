using UnityEngine;
using UnityEngine.UI;

public class GlobalManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Canvas que conter� o mapa")] private Canvas m_canvas;
    [SerializeField, Tooltip("Prefab do tile")] private GameObject m_tilePrefab;
    [SerializeField, Tooltip("Componente de download de tiles")] private TileDownloader m_downloader;
    [SerializeField, Tooltip("Componente de InputController para zoom/pan")] private InputController m_inputController;
    [SerializeField, Tooltip("Sprite padr�o para preencher os tiles que n�o possuem textura do servidor")] private Sprite m_defaultSprite;

    [Header("Map Settings")]
    [SerializeField, Tooltip("Tamanho do tile em pixels")] private int m_tileSize;
    [SerializeField] private RectTransform m_centerReference;
    [SerializeField] private MapSettings[] maps;

    [SerializeField] private MapManager mapManager;
    private PinManager m_pinManager;
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


    private void Awake()
    {
        CreateBackground();
        mapManager.Initialize(m_canvas.transform, m_tilePrefab, m_tileSize, m_defaultSprite, m_downloader, maps[0]);
    }
    /// <summary>
    /// Adiciona um pin no mapa com coordenadas específicas.
    /// </summary>
    public void AddPin(GameObject pinPrefab, double lat, double lon)
    {       
        m_pinManager.AddPin(pinPrefab, lat, lon);
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
}
