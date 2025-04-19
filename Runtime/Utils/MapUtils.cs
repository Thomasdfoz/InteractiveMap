using UnityEngine;

/// <summary>
/// Fun��es utilit�rias para convers�es entre coordenadas geogr�ficas e tiles.
/// </summary>
public static class MapUtils
{
    /// <summary>
    /// Converte latitude e longitude para coordenadas de tile no n�vel de zoom.
    /// </summary>
    public static Vector2Int LatLonToTile(double lat, double lon, int zoom)
    {
        int x = (int)((lon + 180.0) / 360.0 * (1 << zoom));
        int y = (int)((1.0 - Mathf.Log(Mathf.Tan((float)(lat * Mathf.Deg2Rad)) + 1.0f / Mathf.Cos((float)(lat * Mathf.Deg2Rad))) / Mathf.PI) / 2.0 * (1 << zoom));
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Verifica se a coordenada tile � v�lida dentro do range permitido pelo zoom.
    /// </summary>
    public static bool IsValidTile(int x, int y, int zoom)
    {
        int max = 1 << zoom;
        return x >= 0 && x < max && y >= 0 && y < max;
    }
}
