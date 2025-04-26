using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class InputController : MonoBehaviour
{
    [Header("Zoom Limits")]
    [SerializeField] private int zoomMin = 0;
    [SerializeField] private int zoomMax = 18;

    [Header("Pan Sensitivity")]
    [SerializeField] private float panThreshold = 0.8f; // Quantidade de pan antes de re-renderizar
    [SerializeField] private float panSpeed = 1f;

    private GlobalManager m_globalManager;
    private Vector3 lastMousePosition;
    private Vector2 m_panOffset;
    private RectTransform m_mapContent;

    [SerializeField] private bool m_addPin;
    [SerializeField] private bool m_pinwait;
    public GameObject PinPrefab;
    private bool m_initialized;

    public void Initialize(GlobalManager globalManager)
    {
        m_globalManager = globalManager;
        m_mapContent = m_globalManager.MapGlobalContentTransform.GetComponent<RectTransform>();
        m_initialized = true;
    }
    
    private void Update()
    {
        if (!m_initialized) return;

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

        // 1) Sincroniza o pan atual
        UpdateCenterByVisualReference();

        // 2) Calcula zoom antigo e novo
        float oldZoom = m_globalManager.Zoom;
        float newZoom = Mathf.Clamp(oldZoom + (scroll > 0f ? 1 : -1), zoomMin, zoomMax);
        if (newZoom == oldZoom) return;

        // 3) Converte mouse screen → ponto local no viewport do mapa
        Vector2 localMousePos;
        RectTransform mapViewport = m_mapContent.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapViewport,
            Input.mousePosition,
            null,              // se o Canvas for Screen Space - Camera, coloque aqui a câmera UI
            out localMousePos
        );
        // note: se o pivot de mapViewport for (0.5,0.5), localMousePos já é o offset em UI px a partir do centro

        // 4) UI-px → tile-px
        float scale = m_globalManager.TileSize / (float)m_globalManager.TilePixelSize;
        // inverte Y porque o sistema de tile-pixels geralmente cresce pra baixo
        Vector2 offsetPx = new Vector2(
            localMousePos.x / scale,
            -localMousePos.y / scale
        );

        // 5) calcula pixel global do centro ANTES do zoom
        Vector2 centerPxOld = MapUtils.LatLonToPixels(
            m_globalManager.CenterLat,
            m_globalManager.CenterLon,
            oldZoom,
            m_globalManager.TilePixelSize
        );

        // 6) identifica o pixel global embaixo do cursor
        Vector2 clickGlobalPx = centerPxOld + offsetPx;

        // 7) converte para lat/lon (no zoom antigo)
        Vector2 clickLatLon = MapUtils.PixelsToLatLon(
            clickGlobalPx,
            oldZoom,
            m_globalManager.TilePixelSize
        );

        // 8) aplica novo centro e zoom, e re-renderiza
        m_globalManager.CenterLat = clickLatLon.x;
        m_globalManager.CenterLon = clickLatLon.y;
        m_globalManager.Zoom = newZoom;
        UpdateCenterByVisualReference();
        m_globalManager.RenderMap();
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
        float limit = m_globalManager.TileSize * panThreshold;
        if (Mathf.Abs(m_panOffset.x) > limit || Mathf.Abs(m_panOffset.y) > limit)
        {
            UpdateCenterByVisualReference();
            m_globalManager.RenderMap();

            m_panOffset = Vector2.zero;
            m_mapContent.anchoredPosition = Vector2.zero;
        }
    }

    private void UpdateCenterByVisualReference()
    {
        Vector2 localCenter = m_globalManager.CenterReference.anchoredPosition;
        Vector2 mapOffset = localCenter - m_mapContent.anchoredPosition;

        // Converte para pixels globais
        float scale = m_globalManager.TileSize / (float)m_globalManager.TilePixelSize;
        Vector2 offsetPx = mapOffset / scale;
        offsetPx.y = -offsetPx.y; // Corrige o Y pro sistema do mapa

        // Calcula o novo centro em pixels
        Vector2 centerPx = MapUtils.LatLonToPixels(
            m_globalManager.CenterLat,
            m_globalManager.CenterLon,
            m_globalManager.Zoom,
            m_globalManager.TilePixelSize);

        Vector2 newCenterPx = centerPx + offsetPx;
        Vector2 newLatLon = MapUtils.PixelsToLatLon(
            newCenterPx,
            m_globalManager.Zoom,
            m_globalManager.TilePixelSize);

        m_globalManager.CenterLat = newLatLon.x;
        m_globalManager.CenterLon = newLatLon.y;

               // —————— Limpa o deslocamento visual do pan ——————
        m_panOffset = Vector2.zero;
        m_mapContent.anchoredPosition = Vector2.zero;
    }

    private void HandlePin()
    {
        if (Input.GetMouseButtonDown(0) && !m_pinwait)
        {
            Vector2 mouseCanvasPos = Input.mousePosition;
            Vector2 centerPx = MapUtils.LatLonToPixels(
                m_globalManager.CenterLat,
                m_globalManager.CenterLon,
                m_globalManager.Zoom,
                m_globalManager.TilePixelSize);

            float scale = m_globalManager.TileSize / (float)m_globalManager.TilePixelSize;
            Vector2 offset = (mouseCanvasPos - new Vector2(Screen.width / 2f, Screen.height / 2f));
            offset.y = -offset.y;
            offset /= scale;

            Vector2 pinPx = centerPx + offset;
            Vector2 latlon = MapUtils.PixelsToLatLon(pinPx, m_globalManager.Zoom, m_globalManager.TilePixelSize);
            m_globalManager.AddPin(PinPrefab, latlon.x, latlon.y);

            StartCoroutine(PinWait());
        }
    }

    private IEnumerator PinWait()
    {
        m_pinwait = true;
        yield return new WaitForSeconds(1f);
        m_addPin = false;
        m_pinwait = false;
    }
}
