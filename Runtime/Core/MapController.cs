using EGS.Util;
using System;
using System.Collections;
using UnityEngine;

namespace EGS.Core
{
    public class MapController : MonoBehaviour
    {
        [Header("ZoomMouse Limits")]
        [SerializeField] private int zoomMin = 5;
        [SerializeField] private int zoomMax = 18;

        [Header("Pan Sensitivity")]
        [SerializeField] private float panThreshold = 0.8f; // Quantidade de pan antes de re-renderizar
        [SerializeField] private float panSpeed = 1f;

        private GlobalManager m_globalManager;
        private RectTransform m_mapContent;
        private Vector3 lastMousePosition;
        private Vector2 m_panOffset;
        private int m_zoomMax = 18;
        private int m_zoomMin = 6;

        public bool Initialized { get; private set; } = true;

        public void Initialize(GlobalManager globalManager)
        {
            m_globalManager = globalManager;
            m_mapContent = m_globalManager.MapGlobalContentTransform.GetComponent<RectTransform>();

            if (m_globalManager.IsFinish)
                Initialized = true;
        }

        public void ZoomMouse(float value)
        {
            if (!Initialized) return;
            
            //if (!m_globalManager.AllDownloadsComplete())
            //    return;

            if (Mathf.Approximately(value, 0f)) return;

            // 1) Sincroniza o pan atual
            UpdateCenterByVisualReference();

            // 2) Calcula zoom antigo e novo
            float oldZoom = m_globalManager.Zoom;
            float newZoom = Mathf.Clamp(oldZoom + (value > 0f ? 1 : -1), zoomMin, zoomMax);
            if (newZoom == oldZoom) return;

            m_globalManager.RenderMap(newZoom);

            return;

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
            UpdateCenterByVisualReference();
            m_globalManager.RenderMap(clickLatLon.x, clickLatLon.y, newZoom);
        }

        public void Zoom(float value)
        {
            if (!Initialized) return;

            UpdateCenterByVisualReference();
            m_globalManager.RenderMap(m_globalManager.CenterLat, m_globalManager.CenterLon, value);
        }

        public void PanDown(Vector3 mousePos)
        {
            if (!Initialized) return;

            lastMousePosition = mousePos;
        }

        public void PanMove(Vector3 mousePos)
        {
            if (!Initialized) return;

            Vector2 delta = (Vector2)(mousePos - lastMousePosition);
            m_panOffset += delta;
            m_mapContent.anchoredPosition = m_panOffset;
            lastMousePosition = mousePos;
        }

        public void PanUp()
        {
            if (!Initialized) return;

            // Só reagimos se realmente houve pan considerável
            if (m_panOffset.magnitude >= panThreshold)
            {
                // 1) Calcula o deslocamento visual real em pixels de mapa usando o CenterReference
                Vector2 localCenter = m_globalManager.CenterReference.anchoredPosition;      // ponto fixo no meio da tela :contentReference[oaicite:0]{index=0}:contentReference[oaicite:1]{index=1}
                Vector2 mapOffset = localCenter - m_mapContent.anchoredPosition;         // quanto o container foi movido
                float scale = m_globalManager.TileSize / (float)m_globalManager.TilePixelSize;

                // Converte UI-pixels → “map-pixels” e inverte Y para o sistema de tiles
                Vector2 offsetPx = new Vector2(
                  mapOffset.x / scale,
                 -mapOffset.y / scale
                );

                // 2) Puxa o centro antigo em pixels globais
                Vector2 centerPx = MapUtils.LatLonToPixels(
                  m_globalManager.CenterLat,
                  m_globalManager.CenterLon,
                  m_globalManager.Zoom,
                  m_globalManager.TilePixelSize
                );

                // 3) Ajusta para onde realmente está o cursor/centro visual
                Vector2 newCenterPx = centerPx + offsetPx;
                Vector2 newLatLon = MapUtils.PixelsToLatLon(
                  newCenterPx,
                  m_globalManager.Zoom,
                  m_globalManager.TilePixelSize
                );

                m_panOffset = Vector2.zero;
                m_mapContent.anchoredPosition = Vector2.zero;

                // 4) Re-renderiza incremental com o novo centro
                m_globalManager.RenderMap(newLatLon.x, newLatLon.y);
            }
        }

        private void UpdateCenterByVisualReference()
        {
            if (!Initialized) return;

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

            m_globalManager.UpdateCordinates(newLatLon.x, newLatLon.y);
        }

        public void AddPin(Vector2 position, GameObject pinPrefab)
        {
            if (!Initialized) return;

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
            if (!Initialized) return;

            UpdateCenterByVisualReference();
            m_globalManager.RenderMap(lat, lon);
        }

        public void FlyTo(double lat, double lon, float zoom)
        {
            if (!Initialized) return;

            UpdateCenterByVisualReference();
            m_globalManager.RenderMap(lat, lon, zoom);
        }

        public void FlyTo(double minLat, double maxLat, double minLon, double maxLon)
        {
            if (!Initialized) return;

            double centerLat = (minLat + maxLat) / 2f;
            double centerLon = (minLon + maxLon) / 2f;

            int tilePixelSize = m_globalManager.TilePixelSize;
            int tileSize = m_globalManager.TileSize;
            RectTransform canvasRect = m_globalManager.CenterReference;
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;

            for (int zoom = m_zoomMax; zoom >= m_zoomMin; zoom--)
            {
                Vector2 pxA = MapUtils.LatLonToPixels(minLat, minLon, zoom, tilePixelSize);
                Vector2 pxB = MapUtils.LatLonToPixels(maxLat, maxLon, zoom, tilePixelSize);

                float width = Mathf.Abs(pxB.x - pxA.x) * tileSize / tilePixelSize;
                float height = Mathf.Abs(pxB.y - pxA.y) * tileSize / tilePixelSize;

                if (width <= canvasWidth && height <= canvasHeight)
                {
                    FlyTo(centerLat, centerLon, zoom);
                    return;
                }
            }

            // Se nenhum zoom encaixou, usa o menor possível
            FlyTo(centerLat, centerLon, m_zoomMin);
        }
    }
}