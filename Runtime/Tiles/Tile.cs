using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public RawImage _image;

    public void SetTile(Texture2D text)
    {       

        if (_image != null && text != null)
        {
            _image.texture = text;
           
        }
    }
}
