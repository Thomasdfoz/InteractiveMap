using UnityEngine;

public interface IMapInput 
{
    public void FlyTo(double lat, double lon);
    public void SetPin(GameObject pin);
    public void ZoomMouse(float value);
    public void PanDown(Vector2 pos);
    public void PanMove(Vector2 pos);
    public void PanUp();
    public void AddPin(Vector2 pos, GameObject prefab);
}
