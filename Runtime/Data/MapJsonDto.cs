using System;

namespace EGS.Data
{
    // 1) DTO para desserializar o JSON
    [Serializable]
    public class MapJsonDto
    {
        public string[] tiles;
        public string name;
        public string format;
        public string basename;
        public string id;
        public string type;
        public string description;
        public string version;
        public double[] bounds;   // [minLon, minLat, maxLon, maxLat]
        public int maxzoom;
        public int minzoom;
        public double[] center;   // [lon, lat, zoom]
        public string tilejson;
    }
}