using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Orquestra a criação da UI, Input e renderização via TileManager.
/// </summary>
[DisallowMultipleComponent]
public class MapManager : MonoBehaviour
{
    private TileManager m_tileManager;
    private GameObject m_mapContent;
    private MapConfig mapSettings;
    private GlobalManager m_globalManager;

    /// <summary>
    /// Raio de tiles.
    /// </summary>
    public GameObject MapContent { get => m_mapContent; }
    public MapConfig MapSettings { get => mapSettings; }

    public IEnumerator Initialize( GlobalManager globalManager, GameObject tilePrefab, int tileSize, Texture2D defaultTexture, TileDownloader downloader, MapConfig settings)
    {
        m_globalManager = globalManager;
        mapSettings = settings;

        // Inicia coroutine que vai buscar o JSON e só depois cria o TileManager
        yield return FetchConfigAndSetup(settings.URL, tilePrefab, tileSize, defaultTexture, downloader);
    }
    
    private IEnumerator FetchConfigAndSetup(string url, GameObject tilePrefab, int tileSize, Texture2D defaultTexture, TileDownloader downloader)
    {
        // Monta a URL completa (adicione .json se necessário)
        using var www = UnityWebRequest.Get($"{url}.json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Erro ao carregar Map JSON: {www.error}");
            yield break;
        }

        // Desserializa
        var dto = JsonUtility.FromJson<MapJsonDto>(www.downloadHandler.text);

        // Preenche o MapConfig
        mapSettings.ZoomMin = dto.minzoom;
        mapSettings.ZoomMax = dto.maxzoom;
        mapSettings.MinLon = dto.bounds[0];
        mapSettings.MinLat = dto.bounds[1];
        mapSettings.MaxLon = dto.bounds[2];
        mapSettings.MaxLat = dto.bounds[3];
        mapSettings.CenterLon = dto.center[0];
        mapSettings.CenterLat = dto.center[1];
        mapSettings.Zoom = (float)dto.center[2];

        // 3) Agora que MapConfig está completo, cria o container + TileManager
        m_mapContent = CreateMapContainer(transform);
        m_tileManager = m_mapContent.gameObject.AddComponent<TileManager>();
        m_tileManager.Initialize(
            this,
            tilePrefab,
            tileSize,
            defaultTexture,
            downloader);

        // Ajusta os valores iniciais
        m_tileManager.Zoom = mapSettings.Zoom;
        m_tileManager.CenterLat = mapSettings.CenterLat;
        m_tileManager.CenterLon = mapSettings.CenterLon;
        m_tileManager.Range = mapSettings.Range;
    }

    public void ReleaseMap()
    {
        m_tileManager.ReleaseAll();
    }
    /// <summary>
    /// Renderiza o mapa com configurações atuais.
    /// </summary>
    public void RenderMap()
    {
        m_tileManager.Zoom = m_globalManager.Zoom;
        m_tileManager.CenterLat = m_globalManager.CenterLat;
        m_tileManager.CenterLon = m_globalManager.CenterLon;
        m_tileManager.Range = mapSettings.Range;
        m_tileManager.Render();
    }
   

    private GameObject CreateMapContainer(Transform parent)
    {
        GameObject go = new GameObject("MapContent");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localPosition = Vector3.zero; // Posição inicial no centro
        return go;
    }
}