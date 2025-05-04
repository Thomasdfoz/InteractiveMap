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
        [SerializeField, Tooltip("Prefab do tile")] private GameObject m_tilePrefab;
        [SerializeField, Tooltip("Componente de download de tiles")] private TileDownloader m_downloader;
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

        /// <summary>
        /// ZoomMouse atual do mapa.
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
            m_centerLat = m_initialLat;
            m_centerLon = m_initialLon;
            m_zoom = m_initialZoom;

            CreateBackground();

            m_mapGlobal.MapManager = CreateMapContent(m_mapGlobal.name, m_canvasMap.transform, 1);


            yield return m_mapGlobal.MapManager.Initialize(this, m_tilePrefab, m_tileSize, m_defaultTexture, m_downloader, m_mapGlobal);

            if (!m_mapGlobal.MapManager.IsFinish) yield break;

            MapGlobalContentTransform = m_mapGlobal.MapManager.MapContent.transform;
            m_pinManager = m_mapGlobal.MapManager.gameObject.AddComponent<PinManager>();
            m_pinManager.Initialize(this);

            foreach (var map in m_maps)
            {
                map.MapManager = CreateMapContent(map.name, MapGlobalContentTransform, 10);
                AddPin(map.MapManager.gameObject, map.CenterLat, CenterLon);
                yield return map.MapManager. Initialize(this, m_tilePrefab, m_tileSize, m_defaultTexture, m_downloader, map);
            }

            RenderMap();

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

        public void RenderMap()
        {
            // 1) renderiza global
            m_mapGlobal.MapManager.RenderMap();

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
                    mgr.RenderMap();
                else
                    mgr.ReleaseMap();
            }

            // 4) atualiza pins (caso eles dependam de todos os mapas)
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
    }
}