using EGS.Util;
using System;
using UnityEngine;

namespace EGS.Core
{
    public class MapController : MonoBehaviour
    {
        [Header("Zoom Limits")]
        [SerializeField] private int zoomMin = 4;
        [SerializeField] private int zoomMax = 18;

        [Header("Pan Sensitivity")]
        [SerializeField] private float panThreshold = 0.8f; // Quantidade de pan antes de re-renderizar
        [SerializeField] private float panSpeed = 1f;

        private GlobalManager m_globalManager;
        private RectTransform m_mapContent;
        private Vector3 lastMousePosition;
        private Vector2 m_panOffset;

        public bool Initialized { get; private set; }

        public void Initialize(GlobalManager globalManager)
        {
            m_globalManager = globalManager;
            m_mapContent = m_globalManager.MapGlobalContentTransform.GetComponent<RectTransform>();
            Initialized = true;
        }

        public void Zoom(float value)
        {
            if (Mathf.Approximately(value, 0f)) return;

            // 1) Sincroniza o pan atual
            UpdateCenterByVisualReference();

            // 2) Calcula zoom antigo e novo
            float oldZoom = m_globalManager.Zoom;
            float newZoom = Mathf.Clamp(oldZoom + (value > 0f ? 1 : -1), zoomMin, zoomMax);
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

        public void PanDwon(Vector3 mousePos)
        {
            lastMousePosition = mousePos;
        }

        public void PanMove(Vector3 mousePos)
        {
            Vector2 delta = (Vector2)(mousePos - lastMousePosition);
            m_panOffset += delta;
            m_mapContent.anchoredPosition = m_panOffset;
            lastMousePosition = mousePos;
        }

        public void PanUp()
        {
            // só re-renderiza se o pan visual foi maior que o threshold
            if (m_panOffset.magnitude >= panThreshold)
            {
                UpdateCenterByVisualReference();
                m_globalManager.RenderMap();
            }

            // limpa sempre o deslocamento visual
            m_panOffset = Vector2.zero;
            m_mapContent.anchoredPosition = Vector2.zero;

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

        public void AddPin(Vector2 position, GameObject pinPrefab)
        {
            Vector2 mouseCanvasPos = position;
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
            m_globalManager.AddPin(pinPrefab, latlon.x, latlon.y, 99);
        }

        public void FlyTo(double lat, double lon)
        {
            m_globalManager.CenterLat = lat;
            m_globalManager.CenterLon = lon;
            UpdateCenterByVisualReference();
            m_globalManager.RenderMap();
        }
    }
}