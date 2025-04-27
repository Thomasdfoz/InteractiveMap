using UnityEngine;

[CreateAssetMenu(fileName = "PinConfig", menuName = "Maps Objects/Create Pin")]
public class PinConfig : ScriptableObject
{
    public string Name;
    public Sprite Icon;
    public GameObject Prefab;

}
