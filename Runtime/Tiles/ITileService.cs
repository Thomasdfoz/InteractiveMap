using System.Collections;
using UnityEngine;

/// <summary>
/// Serviço responsável por fornecer sprites de tiles.
/// </summary>
public interface ITileService
{
    /// <summary>
    /// Inicia o download do tile e invoca o callback com a textura.
    /// </summary>
    IEnumerator DownloadTile(int x, int y, float zoom, System.Action<Texture2D> onComplete);
}

