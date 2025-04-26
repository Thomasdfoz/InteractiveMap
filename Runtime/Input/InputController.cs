using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class InputController : MonoBehaviour
{
    [Header("Zoom Limits")]
    [SerializeField] private int zoomMin = 0;
    [SerializeField] private int zoomMax = 18;

    [Header("Pan Sensitivity")]
    [SerializeField] private float panThreshold = 0.8f; // Quantidade de pan antes de re-renderizar
    [SerializeField] private float panSpeed = 1f;

    private MapManager m_mapManager;
    private Vector3 lastMousePosition;
    private Vector2 m_panOffset;
    private RectTransform m_mapContent;

    [SerializeField] private bool m_addPin;
    [SerializeField] private bool m_pinwait;
    public GameObject PinPrefab;

    public void Initialize(MapManager mapManager)
    {
        m_mapManager = mapManager;
        m_mapContent = m_mapManager.MapContent.GetComponent<RectTransform>();
    }
    /*
    private void Update()
    {
        if (m_addPin)
        {
            HandlePin();
            return;
        }

        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        // 1) Converte mouse ➔ lat/lon
        Vector2 mousePos = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 canvasOffset = mousePos - screenCenter;
        canvasOffset.y = -canvasOffset.y;

        float scale = m_mapManager.TileSize / (float)m_mapManager.TilePixelSize;
        Vector2 centerPx = MapUtils.LatLonToPixels(m_mapManager.CenterLat, m_mapManager.CenterLon, m_mapManager.Zoom, m_mapManager.TilePixelSize);
        Vector2 clickGlobalPx = centerPx + (canvasOffset / scale);
        Vector2 clickLatLon = MapUtils.PixelsToLatLon(clickGlobalPx, m_mapManager.Zoom, m_mapManager.TilePixelSize);

        // 2) Ajusta zoom
        float oldZoom = m_mapManager.Zoom;
        float newZoom = Mathf.Clamp(oldZoom + (scroll > 0f ? 1 : -1), zoomMin, zoomMax);
        if (newZoom == oldZoom) return;

        // 3) Atualiza o centro para a posição do mouse
        m_mapManager.CenterLat = clickLatLon.x;
        m_mapManager.CenterLon = clickLatLon.y;
      
        m_mapManager.Zoom = newZoom;

        // 4) Renderiza
        m_mapManager.RenderMap();
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 delta = (Vector2)(Input.mousePosition - lastMousePosition);
            m_panOffset += delta;
            m_mapContent.anchoredPosition = m_panOffset;
            lastMousePosition = Input.mousePosition;
        }

        CheckPanThreshold(); // agora fora do botão do mouse
    }

    private void CheckPanThreshold()
    {
        float limit = m_mapManager.TileSize * panThreshold;
        if (Mathf.Abs(m_panOffset.x) > limit || Mathf.Abs(m_panOffset.y) > limit)
        {
            UpdateCenterByVisualReference();
            m_mapManager.RenderMap();

            m_panOffset = Vector2.zero;
            m_mapContent.anchoredPosition = Vector2.zero;
        }
    }

    private void UpdateCenterByVisualReference()
    {
        Vector2 localCenter =m_mapManager.CenterReference.anchoredPosition;
        Vector2 mapOffset = localCenter - m_mapContent.anchoredPosition;

        // Converte para pixels globais
        float scale = m_mapManager.TileSize / (float)m_mapManager.TilePixelSize;
        Vector2 offsetPx = mapOffset / scale;
        offsetPx.y = -offsetPx.y; // Corrige o Y pro sistema do mapa

        // Calcula o novo centro em pixels
        Vector2 centerPx = MapUtils.LatLonToPixels(
            m_mapManager.CenterLat,
            m_mapManager.CenterLon,
            m_mapManager.Zoom,
            m_mapManager.TilePixelSize);

        Vector2 newCenterPx = centerPx + offsetPx;
        Vector2 newLatLon = MapUtils.PixelsToLatLon(newCenterPx, m_mapManager.Zoom, m_mapManager.TilePixelSize);

        m_mapManager.CenterLat = newLatLon.x;
        m_mapManager.CenterLon = newLatLon.y;
    }

    private void HandlePin()
    {
        if (Input.GetMouseButtonDown(0) && !m_pinwait)
        {
            Vector2 mouseCanvasPos = Input.mousePosition;
            Vector2 centerPx = MapUtils.LatLonToPixels(
                m_mapManager.CenterLat,
                m_mapManager.CenterLon,
                m_mapManager.Zoom,
                m_mapManager.TilePixelSize);

            float scale = m_mapManager.TileSize / (float)m_mapManager.TilePixelSize;
            Vector2 offset = (mouseCanvasPos - new Vector2(Screen.width / 2f, Screen.height / 2f));
            offset.y = -offset.y;
            offset /= scale;

            Vector2 pinPx = centerPx + offset;
            Vector2 latlon = MapUtils.PixelsToLatLon(pinPx, m_mapManager.Zoom, m_mapManager.TilePixelSize);
            m_mapManager.AddPin(PinPrefab, latlon.x, latlon.y);

            StartCoroutine(PinWait());
        }
    }*/

    private IEnumerator PinWait()
    {
        m_pinwait = true;
        yield return new WaitForSeconds(1f);
        m_addPin = false;
        m_pinwait = false;
    }
}
