using UnityEngine;

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
    [HideInInspector] public MapManager MapManager;
}
