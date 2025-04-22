using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class TileDownloader : MonoBehaviour
{
    private string tileURLTemplate = "http://localhost:9090/data/da-ilha-das-oncas-a-manaus/{0}/{1}/{2}.png";
    //private string tileURLTemplate = "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
    private int maxConcurrentDownloads = 4;
    private int activeDownloads = 0;

    public IEnumerator DownloadTile(int x, int y, float zoom, Action<Texture2D> callback)
    {
        while (activeDownloads >= maxConcurrentDownloads)
            yield return null;

        activeDownloads++;

        string url = string.Format(tileURLTemplate, zoom, x, y);

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Erro ao baixar tile {zoom}/{x}/{y}: {uwr.error}");
                callback(null);
            }
            else
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                callback(tex);
            }
        }

        activeDownloads--;
    }

}
