using System;
using UnityEngine;

/// <summary>
/// Controla zoom e pan do mapa via input do mouse.
/// </summary>
[DisallowMultipleComponent]
public class InputController : MonoBehaviour
{
    [Header("Zoom Limits")]
    [SerializeField, Tooltip("Zoom mínimo permitido")] private int zoomMin = 0;
    [SerializeField, Tooltip("Zoom máximo permitido")] private int zoomMax = 18;

    [Header("Pan Sensitivity")]
    [SerializeField, Tooltip("Grau por pixel no zoom mínimo")] private float degreesAtMin = 0.001f;
    [SerializeField, Tooltip("Grau por pixel no zoom máximo")] private float degreesAtMax = 0.0005f;

    private MapManager m_mapManager;
    private Vector3 lastMousePosition;
    public GameObject PinPrefab;

    /// <summary>
    /// Inicializa o InputController com o MapManager alvo.
    /// </summary>
    public void Initialize(MapManager mapManager)
    {
        m_mapManager = mapManager;
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
        HandlePin();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        int newZoom = Mathf.Clamp(m_mapManager.Zoom + (scroll > 0f ? 1 : -1), zoomMin, zoomMax);
        if (newZoom == m_mapManager.Zoom) return;

        m_mapManager.Zoom = newZoom;
        m_mapManager.RenderMap();
    }
    private Vector2 panOffset;
    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            panOffset = Vector2.zero;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            panOffset += new Vector2(delta.x, delta.y);
            // Move o MapContent em tempo real
            m_mapManager.MapContent.GetComponent<RectTransform>().anchoredPosition = panOffset;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Calcula o deslocamento em pixels globais
            double mapWidthInPixels = m_mapManager.TilePixelSize * Math.Pow(2, m_mapManager.Zoom);
            double degreesPerPixelLon = 360.0 / mapWidthInPixels;
            // Ajusta a escala da latitude com base na projeção Mercator
            double latRad = m_mapManager.CenterLat * Math.PI / 180.0;
            double degreesPerPixelLat = degreesPerPixelLon / Math.Cos(latRad);

            // Ajusta o centro com base no offset acumulado
            m_mapManager.CenterLon -= panOffset.x * degreesPerPixelLon;
            m_mapManager.CenterLat -= panOffset.y * degreesPerPixelLat;

            // Reseta o offset e re-renderiza
            panOffset = Vector2.zero;
            m_mapManager.MapContent.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            m_mapManager.RenderMap();
        }
    }

    private void HandlePin()
    {
        if (Input.GetMouseButtonDown(1))
        {
            m_mapManager.AddPin(PinPrefab, -3.3074, -59.800);
        }
    }
}