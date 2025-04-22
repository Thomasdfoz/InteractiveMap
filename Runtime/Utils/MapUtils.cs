using System;
using UnityEngine;

/// <summary>
/// Funções utilitárias para conversões entre coordenadas geográficas e tiles.
/// </summary>
public static class MapUtils
{
    /// <summary>
    /// Converte latitude e longitude para coordenadas de tile no nível de zoom.
    /// </summary>
    public static Vector2Int LatLonToTile(double lat, double lon, int zoom)
    {
        int x = (int)((lon + 180.0) / 360.0 * (1 << zoom));
        int y = (int)((1.0 - Mathf.Log(Mathf.Tan((float)(lat * Mathf.Deg2Rad)) + 1.0f / Mathf.Cos((float)(lat * Mathf.Deg2Rad))) / Mathf.PI) / 2.0 * (1 << zoom));
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Verifica se a coordenada tile é válida dentro do range permitido pelo zoom.
    /// </summary>
    public static bool IsValidTile(int x, int y, int zoom)
    {
        int max = 1 << zoom;
        return x >= 0 && x < max && y >= 0 && y < max;
    }

    public static Vector2 LatLonToPixels(double lat, double lon, float zoom, int tileSize)
    {
        // Convert latitude/longitude to Mercator meters
        double x = (lon + 180.0) / 360.0;
        double sinLat = Math.Sin(lat * Math.PI / 180.0);
        double y = 0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI);

        // Calculate pixel coordinates at the given zoom level
        double mapSize = tileSize * Math.Pow(2, zoom);
        x = x * mapSize;
        y = y * mapSize;

        return new Vector2((float)x, (float)y);
    }
}
