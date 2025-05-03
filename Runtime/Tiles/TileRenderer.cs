using UnityEngine;
using UnityEngine.UI;

namespace EGS.Tile
{
    public class TileRenderer : MonoBehaviour
    {
        public RawImage _image;
    
        public void SetTile(Texture2D text)
        {       
    
            if (_image != null && text != null)
            {
                _image.texture = text;
               
            }
            else
            {
               gameObject.SetActive(false);
            }
        }
    }
}