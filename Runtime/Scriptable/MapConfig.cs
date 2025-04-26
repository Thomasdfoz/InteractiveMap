using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig", menuName = "Maps Objects/MapConfig")]
public class MapConfig : ScriptableObject
{
    public string Name;
    public string URL;
    public float Zoom;
    public int Range;
    public double CenterLat;
    public double CenterLon;
    public int TilePixelSize;
}
