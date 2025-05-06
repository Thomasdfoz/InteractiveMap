using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EGS.MapInput
{

public interface IMapInput
{
    public void FlyTo(double lat, double lon);
    public void SetPin(GameObject pin);
    public void ZoomMouse(float value);
    public void PanDown(Vector2 pos);
    public void PanMove(Vector2 pos);
    public void PanUp();
    public void AddPin(Vector2 pos, GameObject prefab);

    public void FlyTo(double lat, double lon, float zoom);

    public void FlyTo(double minLat, double maxLat, double minLon, double maxLon);
}

}