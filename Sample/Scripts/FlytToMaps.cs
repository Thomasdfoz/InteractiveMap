using EGS.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlytToMaps : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown m_dropdown;
    [SerializeField] private GlobalManager m_globalManager;
    [SerializeField] private MapController m_mapController;
    [SerializeField] private Button m_button;

    void Start()
    {
        m_button.onClick.AddListener(OnButtonClick);
        m_dropdown.ClearOptions();

        foreach (var item in m_globalManager.Maps)
        {
            AdicionarOpcao(item.Name);
        }

    }

    private void OnButtonClick()
    {
        int index = m_dropdown.value;
        var map = m_globalManager.Maps[index];
        m_mapController.FlyTo(map.CenterLat, map.CenterLon, map.Zoom);

    }

    /// <summary>
    /// Adiciona uma nova opção (texto e, opcionalmente, imagem) ao m_dropdown.
    /// </summary>
    private void AdicionarOpcao(string texto)
    {
        var opcao = new TMP_Dropdown.OptionData(texto);
        m_dropdown.options.Add(opcao);
        AtualizarDropdown();
    }

    /// <summary>
    /// Força o TMP a redesenhar o valor selecionado.
    /// </summary>
    private void AtualizarDropdown()
    {
        m_dropdown.RefreshShownValue();
    }
}

