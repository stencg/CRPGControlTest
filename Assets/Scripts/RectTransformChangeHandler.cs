using UnityEngine;
namespace Assets.Scripts
{
    public class CanvasSizeListener : MonoBehaviour
    {
        public void OnRectTransformDimensionsChange()
        {
            //Debug.Log("Canvas dimensions changed: " + GetComponent<RectTransform>().rect.size);
            if (MyCameraController.Instance) MyCameraController.Instance.OnScreenChange(GetComponent<RectTransform>().rect.size);
        }
    }
}