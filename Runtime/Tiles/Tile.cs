using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public Image _image;

    public void SetTile(Sprite sprite)
    {       

        if (_image != null && sprite != null)
        {
            _image.sprite = sprite;
           
        }
    }
}
