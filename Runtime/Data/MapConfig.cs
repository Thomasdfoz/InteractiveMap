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
        public int Range;
        public int TilePixelSize;
        [HideInInspector] public float ZoomMin;
        [HideInInspector] public float ZoomMax;
        [Header("Map Center")]
        [HideInInspector] public float Zoom;
        [HideInInspector] public double CenterLat;
        [HideInInspector] public double CenterLon;
        [Header("Map Bounds")]
        [HideInInspector] public double MinLon;
        [HideInInspector] public double MaxLon;
        [HideInInspector] public double MinLat;
        [HideInInspector] public double MaxLat;
        [Header("Cache & Performance")]
        [Tooltip("Quantos tiles al�m do Range devem ser mantidos carregados para evitar �reas brancas.")]
        [SerializeField] public int BufferMargin;
        [Tooltip("Quantos n�veis de zoom acima/abaixo devem ser pr�-carregados.")]
        [SerializeField] public int ZoomBuffer;
        [HideInInspector] public MapManager MapManager;
    }
}