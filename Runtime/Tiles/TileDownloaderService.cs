using System.Collections;
using UnityEngine;

/// <summary>
/// Implementação de ITileService que utiliza TileDownloader para buscar tiles.
/// </summary>
public class TileDownloaderService : ITileService
{
    private readonly TileDownloader m_downloader;

    public TileDownloaderService(TileDownloader downloader)
    {
        m_downloader = downloader;
    }

    public IEnumerator DownloadTile(int x, int y, int zoom, System.Action<Texture2D> onComplete)
    {
        yield return m_downloader.DownloadTile(x, y, zoom, onComplete);
    }
}