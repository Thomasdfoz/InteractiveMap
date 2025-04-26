using System;

[Serializable]
public struct MapSettings
{
    public string URL;
    public float Zoom;
    public int Range;
    public double CenterLat;
    public double CenterLon;
    public int TilePixelSize;
}
