using EGS.Core;
using UnityEngine;

namespace EGS.Data
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Maps Objects/MapConfig")]
    public class MapConfig : ScriptableObject
    {
        [Header("Map Settings")]
        public string Name;
        public string URL;
        public float ZoomMin;
        public float ZoomMax;
        public int Range;
        public int TilePixelSize;
        [Header("Map Center")]
        public float Zoom;
        public double CenterLat;
        public double CenterLon;
        [Header("Map Bounds")]
        public double MinLon;
        public double MaxLon;
        public double MinLat;
        public double MaxLat;
        [Header("Cache & Performance")]
        [Tooltip("Quantos tiles além do Range devem ser mantidos carregados para evitar áreas brancas.")]
        [SerializeField] public int BufferMargin;
        [Tooltip("Quantos níveis de zoom acima/abaixo devem ser pré-carregados.")]
        [SerializeField] public int ZoomBuffer;
        [HideInInspector] public MapManager MapManager;
    }
}