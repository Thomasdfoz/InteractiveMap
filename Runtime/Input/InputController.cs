using UnityEngine;

public class InputController : MonoBehaviour
{
    public int zoomMin;
    public int zoomMax;
    public float degreesAtMin = 0.001f;  // valor para m_zoom mínimo
    public float degreesAtMax = 0.0005f;
    public TileManager m_tileManager;

    private Vector3 lastMousePosition;

    void Update()
    {
         float scroll = Input.GetAxis("Mouse ScrollWheel");

         if (scroll != 0)
         {
             // Calcula o novo m_zoom sem atualizar ainda
             int newZoom = Mathf.Clamp(m_tileManager.Zoom + (scroll > 0 ? 1 : -1), zoomMin, zoomMax);

             // Só atualiza se o m_zoom realmente mudou
             if (newZoom != m_tileManager.Zoom)
             {
                 m_tileManager.Zoom = newZoom;

                 Debug.Log(newZoom);

                 // Carrega os novos tiles com o novo m_zoom
                 //m_tileManager.LoadTiles();
             }
         }

         // Captura o início do drag
         if (Input.GetMouseButtonDown(0))
         {
             lastMousePosition = Input.mousePosition;
         }

         // Fim do drag
         if (Input.GetMouseButtonUp(0))
         {
             // Calcula a variação do drag em tela (pixels) convertida para unidades do mundo
             Vector3 currentMousePos = Input.mousePosition;
             Vector3 deltaScreen = currentMousePos - lastMousePosition;

             /*  
                 Converta o deltaScreen para variação em coordenadas geográficas.
                 A lógica aqui depende da escala do seu mapa. Nesse exemplo, 
                 cada unidade do mundo movimentada corresponde a "degreesPerUnit" graus.
             */

             float t = Mathf.InverseLerp(zoomMin, zoomMax, m_tileManager.Zoom);
             float dynamicDegreesPerUnit = Mathf.Lerp(degreesAtMin, degreesAtMax, t);

             float deltaLon = -deltaScreen.x * dynamicDegreesPerUnit;
             float deltaLat = -deltaScreen.y * dynamicDegreesPerUnit;

             // Atualiza o centro do mapa
             m_tileManager.CenterLon += deltaLon;
             m_tileManager.CenterLat += deltaLat;

             // Limpa os tiles antigos e recarrega com o novo centro
           //  m_tileManager.LoadTiles();
         }
    }
}


