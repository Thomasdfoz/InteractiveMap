using EGS.Core;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace EGS.Sample
{
    [DisallowMultipleComponent]
    public class InputController : MonoBehaviour
    {
        [SerializeField] private MapController mapController;
        [Header("→ Bloquear interações quando sobre essas layer")]
        [SerializeField] private LayerMask pinUiLayerMask;
        [SerializeField] private Canvas canvasMap;
        private GameObject PinHandle;
        private RectTransform pinHandleRectTransform;
        private bool pinning = false;


        private void Update()
        {
            if (IsPointerOverPinUI())
                return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (pinning)
            {
                if (pinHandleRectTransform == null)
                {
                    GameObject go = Instantiate(PinHandle, canvasMap.transform);
                    pinHandleRectTransform = go.GetComponent<RectTransform>();
                    Canvas can = pinHandleRectTransform.gameObject.AddComponent<Canvas>();
                    can.overrideSorting = true;
                    can.sortingOrder = 1000;
                }

                // Atualiza a posição do pinHandleRectTransform para seguir o mouse dentro do Canvas Overlay
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasMap.transform as RectTransform,
                    Input.mousePosition,
                    canvasMap.worldCamera,
                    out Vector2 localPoint
                );

                pinHandleRectTransform.anchoredPosition = localPoint;

                if (Input.GetMouseButtonDown(1))
                {
                    pinning = false;
                    AddPin(Input.mousePosition, PinHandle);
                    Destroy(pinHandleRectTransform.gameObject);
                    PinHandle = null;
                }              
            }

            if (!Mathf.Approximately(scroll, 0f))
            {
                ZoomMouse(scroll);
            }
            if (Input.GetMouseButtonDown(0))
            {
                PanDown(Input.mousePosition);
            }
            if (Input.GetMouseButton(0))
            {
                PanMove(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(0))
            {
                PanUp();
            }
        }

        public void ZoomMouse(float value)
        {
            mapController.ZoomMouse(value);
        }

        public void PanDown(Vector2 pos)
        {
            mapController.PanDown(pos);
        }

        public void PanMove(Vector2 pos)
        {
            mapController.PanMove(pos);
        }
        public void PanUp()
        {
            mapController.PanUp();
        }

        public void AddPin(Vector2 pos, GameObject prefab)
        {
            mapController.AddPin(Input.mousePosition, PinHandle);
        }

        public void FlyTo(double lat, double lon)
        {
            mapController.FlyTo(lat, lon);
        }

        public void SetPin(GameObject pin)
        {
            pinning = true;
            PinHandle = pin;
        }

        private bool IsPointerOverPinUI()
        {
            if (EventSystem.current == null)
                return false;

            var ped = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            foreach (var rr in results)
            {
                // compara a layer do objeto detectado
                if (((1 << rr.gameObject.layer) & pinUiLayerMask) != 0)
                    return true;
            }

            return false;
        }


    }
}