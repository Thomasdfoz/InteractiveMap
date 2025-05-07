using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EGS.Sample
{
    public class FlyTo : MonoBehaviour
    {
        [SerializeField] private InputController inputController;
        [SerializeField] private TMP_InputField latInput;
        [SerializeField] private TMP_InputField longInput;
        [SerializeField] private Button flyToButton;

        private bool isFlying = false;
        private void Start()
        {
            flyToButton.onClick.AddListener(Flyto);
        }

        private void Flyto()
        {
            if (isFlying) return; // Prevent multiple clicks

            isFlying = true;
            double lat, lon;

            if (TryParseCoord(latInput.text, out lat) && TryParseCoord(longInput.text, out lon))
            {
                inputController.FlyTo(lat, lon);
            }
            else
            {
                Debug.LogError("Coordenadas inválidas");
            }

            StartCoroutine(FlyComplete());
        }

        private IEnumerator FlyComplete()
        {
            yield return new WaitForSeconds(1f);
            isFlying = false;
        }

        public bool TryParseCoord(string text, out double value)
        {
            // 1) Remove espaços e troca vírgula por ponto
            var s = text.Trim().Replace(',', '.');
            // 2) Tenta parse com InvariantCulture (que só aceita ponto como decimal)
            return double.TryParse(s,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out value);
        }

    }
}
