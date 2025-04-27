using System;
using UnityEngine;

namespace EGS.Util
{
    /// <summary>
    /// Funções utilitárias para conversões entre coordenadas geográficas e tiles.
    /// </summary>
    public static class MapUtils
    {
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
    
        public static Vector2 PixelsToLatLon(Vector2 pixel, float zoom, int tileSize)
        {
            double mapSize = tileSize * Math.Pow(2, zoom);
    
            double x = pixel.x / mapSize;
            double y = pixel.y / mapSize;
    
            double lon = x * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y)));
            double lat = latRad * 180.0 / Math.PI;
    
            return new Vector2((float)lat, (float)lon);
        }
    }
}