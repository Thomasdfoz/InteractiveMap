using EGS.Core;
using UnityEngine;
using UnityEngine.UI;
using EGS.Sample;

namespace EGS.Sample
{
    public class PinUI : MonoBehaviour
    {
        private Image m_icon;
        private Button m_button;
        private PinConfig m_pinConfig;
        private InputController m_input;
    
        public void Init(PinConfig pinConfig, InputController input)
        {
            m_icon = GetComponent<Image>();
            m_button = GetComponent<Button>();
            m_pinConfig = pinConfig;
            m_input = input;
            m_icon.sprite = pinConfig.Icon;
            m_button.onClick.AddListener(OnClick);
        }
    
        public void OnClick()
        {
            m_input.SetPin(m_pinConfig.Prefab);
        }
    
    }
}