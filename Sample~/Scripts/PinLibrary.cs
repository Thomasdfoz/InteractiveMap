using EGS.MapInput;
using UnityEngine;

namespace EGS.Sample
{
    public class PinLibrary : MonoBehaviour
    {
        [SerializeField] private PinConfig[] m_pins;
        [SerializeField] private Transform m_content;
        [SerializeField] private PinUI m_prefab;
        [SerializeField] private InputController m_inputController;
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            foreach (var pin in m_pins)
            {
                var pinUI = Instantiate(m_prefab, m_content);
                pinUI.Init(pin, m_inputController);
            }
        }
    }
}