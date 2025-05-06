using EGS.Core;
using TMPro;
using UnityEngine;

public class Info : MonoBehaviour
{
    [SerializeField] private GlobalManager m_globalManager;
    [SerializeField] private TextMeshProUGUI m_latitudeValue;
    [SerializeField] private TextMeshProUGUI m_longitudeValue;
    [SerializeField] private TextMeshProUGUI m_zoomValue;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_latitudeValue.text = m_globalManager.CenterLat.ToString("F6");
        m_longitudeValue.text = m_globalManager.CenterLon.ToString("F6");
        m_zoomValue.text = m_globalManager.Zoom.ToString("F0");
    }
}
