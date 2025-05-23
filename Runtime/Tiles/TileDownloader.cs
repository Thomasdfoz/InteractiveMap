﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

namespace EGS.Tile
{
    public class TileDownloader
    {
        //[SerializeField] private string tileURL= "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
        //[Tooltip("URL parcial do mapa ex: 'da-ilha-das-oncas-a-manaus'")]
        //[SerializeField] private string tileURL;
    
        private int maxConcurrentDownloads = 10;
        private int activeDownloads = 0;

        //public string TileURL => tileURL;
        public IEnumerator DownloadTile(string baseUrl, float x, float y, float zoom, Action<Texture2D> callback)
        {
            while (activeDownloads >= maxConcurrentDownloads)
                yield return null;

            activeDownloads++;

            // ⏱️ Início da medição
            float startTime = Time.realtimeSinceStartup;

            string path = string.Concat(baseUrl, "/{0}/{1}/{2}.png");
            string fullUrl = string.Format(path, zoom, x, y);

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(fullUrl))
            {
                yield return uwr.SendWebRequest();

                // ⏱️ Fim da medição
                float duration = Time.realtimeSinceStartup - startTime;
                Debug.Log($"⏱️ Tempo de download do tile [{zoom}/{x}/{y}]: {duration:F2}s");

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Erro ao baixar tile: {fullUrl} => {uwr.error}");
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
}