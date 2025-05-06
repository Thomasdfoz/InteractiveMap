using EGS.Data;
using EGS.Tile;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EGS.Core
{
    public class GlobalManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Canvas que conter� o mapa")] private Canvas m_canvasMap;
        [SerializeField, Tooltip("Prefab do tile")] private TileRenderer m_tilePrefab;
        [SerializeField, Tooltip("Componente de InputController para zoom/pan")] private MapController m_mapController;
        [SerializeField, Tooltip("Texture padrao para preencher os tiles que n�o possuem textura do servidor")] private Texture2D m_defaultTexture;

        [Header("Map Settings")]
        [SerializeField, Tooltip("Tamanho do tile em pixels")] private int m_tileSize;
        [SerializeField] private RectTransform m_centerReference;
        [SerializeField] private double m_initialLat = -3.1f;
        [SerializeField] private double m_initialLon = -60.2f;
        [SerializeField] private int m_initialZoom = 6;
        [SerializeField] double lonOffset = 0.5;
        [SerializeField] double latOffset = 0.5;
        [SerializeField] private MapConfig m_mapGlobal;
        [SerializeField] private MapConfig[] m_maps;

        private PinManager m_pinManager;
        private double m_centerLon;
        private double m_centerLat;
        private float m_zoom;
        private bool m_isFinish;
        private bool m_isRender;

        /// <summary>
        /// ZoomMouse atual do mapa.
        /// </summary>
        public float Zoom { get => m_zoom; }

        /// <summary>
        /// Latitude central do mapa.
        /// </summary>
        public double CenterLat { get => m_centerLat; }

        /// <summary>
        /// Longitude central do mapa.
        /// </summary>
        public double CenterLon { get => m_centerLon; }

        public Transform MapGlobalContentTransform { get; private set; }
        public RectTransform CenterReference => m_centerReference;
        public MapConfig[] Maps => m_maps;
        public int TilePixelSize => m_mapGlobal.TilePixelSize;
        public int TileSize => m_tileSize;
        public bool IsFinish => m_isFinish;

        public bool AllDownloadsComplete()
        {
            if (!m_mapGlobal.MapManager.AllDownloadsComplete)
                return false;

            foreach (var map in m_maps)
            {
                if (!map.MapManager.AllDownloadsComplete)
                    return false;
            }

            return true;
        }

        private IEnumerator Start()
        {
           UpdateCordinates(m_initialLat, m_initialLon);
            m_zoom = m_initialZoom;

            CreateBackground();

            m_mapGlobal.MapManager = CreateMapContent(m_mapGlobal.name, m_canvasMap.transform, 1);


            yield return m_mapGlobal.MapManager.Initialize(this, m_tilePrefab, m_tileSize, m_defaultTexture, m_mapGlobal);

            if (!m_mapGlobal.MapManager.IsFinish) yield break;

            MapGlobalContentTransform = m_mapGlobal.MapManager.MapContent.transform;
            m_pinManager = m_mapGlobal.MapManager.gameObject.AddComponent<PinManager>();
            m_pinManager.Initialize(this);

            foreach (var map in m_maps)
            {
                map.MapManager = CreateMapContent(map.name, MapGlobalContentTransform, 10);
                AddPin(map.MapManager.gameObject, map.CenterLat, CenterLon);
                yield return map.MapManager.Initialize(this, m_tilePrefab, m_tileSize, m_defaultTexture, map);
            }

            m_mapGlobal.MapManager.RegisterEventOnZoomRenderingFinish(() => { m_isRender = false; m_mapGlobal.MapManager.gameObject.transform.localScale = Vector3.one; m_pinManager.UpdateAllPins(); });

            RenderMap(true);

            m_isFinish = true;

            m_mapController.Initialize(this);
        }
        /// <summary>
        /// Adiciona um pin no mapa com coordenadas específicas.
        /// </summary>
        public void AddPin(GameObject pinPrefab, double lat, double lon, int sortOrder = 0)
        {
            m_pinManager.AddPin(pinPrefab, lat, lon, sortOrder);
        }

        public void UpdateCordinates(double lat, double lon)
        {
            // Limites das Américas
            double MIN_LAT = -60.0;
            double MAX_LAT = 30.0;
            double MIN_LON = -170.0;
            double MAX_LON = -30.0;

            m_centerLat = Mathf.Clamp((float)lat, (float)MIN_LAT, (float)MAX_LAT);
            m_centerLon = Mathf.Clamp((float)lon, (float)MIN_LON, (float)MAX_LON);
        }

        public void RenderMap(double lat, double lon, float zoom)
        {
            if (m_isRender) return;
            bool zoomChanged = (zoom != Zoom);
            UpdateCordinates(lat, lon);
            m_zoom = zoom;
            RenderMap(false);
        }

        public void RenderMap(float zoom)
        {
            if (m_isRender) return;
            if (zoom != Zoom)
            {
                StartCoroutine(ScaleObject(zoom > Zoom));
                m_zoom = zoom;
                RenderMap(true);
            }
        }

        public void RenderMap(double lat, double lon)
        {
            if (m_isRender) return;
            UpdateCordinates(lat, lon);
            RenderMap();
        }
        private void RenderMap(bool zoomChanged = false)
        {
            m_isRender = zoomChanged;

            // 1) renderiza global
            m_mapGlobal.MapManager.RenderMap(zoomChanged);

            // 3) percorre cada mapa náutico
            foreach (var map in m_maps)
            {
                var mgr = map.MapManager;

                // zoom dentro do intervalo?
                bool inZoom = (Zoom >= map.ZoomMin && Zoom <= map.ZoomMax);

                // centro global dentro da caixa?
                bool inBounds = CenterLon >= (map.MinLon - lonOffset)
                      && CenterLon <= (map.MaxLon + lonOffset)
                      && CenterLat >= (map.MinLat - latOffset)
                      && CenterLat <= (map.MaxLat + latOffset);

                // só renderiza se zoom E bounds OK
                if (inZoom && inBounds)
                    mgr.RenderMap(zoomChanged);
                else
                    mgr.ReleaseMap();
            }

            // 4) atualiza pins (caso eles dependam de todos os mapas)
            if (!zoomChanged)
                m_pinManager.UpdateAllPins();
        }

        private MapManager CreateMapContent(string name, Transform parent, int sortOrder)
        {
            GameObject bgGO = new GameObject(name);
            bgGO.transform.SetParent(parent, false);
            RectTransform rt = bgGO.AddComponent<RectTransform>();
            MapManager mapManager = bgGO.AddComponent<MapManager>();
            Canvas can = bgGO.AddComponent<Canvas>();
            can.overrideSorting = true;
            can.sortingOrder = sortOrder;
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
            img.transform.SetParent(m_canvasMap.transform, false);
            RectTransform rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-500, -500);
            rt.offsetMax = new Vector2(500, 500);
        }

        IEnumerator ScaleObject(bool more)
        {
            Vector3 initialScale = transform.localScale;
            float size = 2f;
            float sizeMinus = 0.5f;
            Vector3 targetScale = more ? new(size, size, size) : new(sizeMinus, sizeMinus, sizeMinus);
            float elapsedTime = 0f;
            float duration = 2f;

            while (elapsedTime < duration)
            {
                m_mapGlobal.MapManager.gameObject.transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;

                if (!m_isRender)
                {
                    m_mapGlobal.MapManager.gameObject.transform.localScale = Vector3.one;
                    yield break;
                }
            }

            m_mapGlobal.MapManager.gameObject.transform.localScale = targetScale; // Garante que o valor final seja exato
        }
    }
}