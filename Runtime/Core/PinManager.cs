using System.Collections.Generic;
using UnityEngine;

public class PinManager : MonoBehaviour
{
    private MapManager m_mapManager;
    private List<GameObject> m_pins = new List<GameObject>();

    public void Initialize(MapManager mgr)
    {
        m_mapManager = mgr;
    }

    public void AddPin(GameObject pinPrefab, double lat, double lon)
    {
        var go = Instantiate(pinPrefab, m_mapManager.MapContent.transform);
        go.name = $"Pin_{lat}_{lon}";
        m_pins.Add(go);

        // AQUI! Setar os dados no PinData
        var pinData = go.GetComponent<PinData>();
        if (pinData != null)
        {
            pinData.Latitude = lat;
            pinData.Longitude = lon;
        }

        UpdatePin(go, lat, lon);
    }

    public void UpdateAllPins()
    {
        foreach (var go in m_pins)
        {
            // parse lat/lon dos dados que você armazenou
            double lat = go.GetComponent<PinData>().Latitude;
            double lon = go.GetComponent<PinData>().Longitude;
            UpdatePin(go, lat, lon);
        }
    }

    private void UpdatePin(GameObject pin, double lat, double lon)
    {
        // Pega as propriedades do mapa (ajuste conforme sua estrutura)
        double centerLat = m_mapManager.CenterLat;
        double centerLon = m_mapManager.CenterLon;
        float zoom = m_mapManager.Zoom;
        int tilePixelSize = m_mapManager.TilePixelSize; // ex: 256

        // Calcula o centro em pixels globais
        Vector2 centerPx = MapUtils.LatLonToPixels(centerLat, centerLon, zoom, tilePixelSize);
        // Calcula o pin em pixels globais
        Vector2 pinPx = MapUtils.LatLonToPixels(lat, lon, zoom, tilePixelSize);

        // Calcula o offset em pixels globais
        Vector2 offset = pinPx - centerPx;
        offset.y = -offset.y; // Inverte o Y pra coordenadas do Unity

        // Converte de pixels pra unidades do Unity
        float scale = m_mapManager.TileSize / (float)tilePixelSize; // ex: TileSize em unidades do Unity
        offset *= scale;

        // Aplica a posição no pin
        RectTransform rt = pin.GetComponent<RectTransform>();
        rt.localPosition = offset;
    }
}
