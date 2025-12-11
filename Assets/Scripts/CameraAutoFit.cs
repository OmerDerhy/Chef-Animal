using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoFit : MonoBehaviour
{
    public float referenceAspectWidth = 16f;
    public float referenceAspectHeight = 9f;

    void Start()
    {
        AdjustCamera();
    }

    void AdjustCamera()
    {
        Camera cam = GetComponent<Camera>();

        float targetAspect = referenceAspectWidth / referenceAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scale = windowAspect / targetAspect;

        // We want to always fit height, so if the device is wider (like iPad)
        // we simply show more horizontally — no black bars.
        if (scale >= 1.0f)
        {
            // Wider screen: expand horizontally, no letterbox
            cam.orthographicSize = referenceAspectHeight / 2f;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
        }
        else
        {
            // Taller screen: zoom out a bit to fit height
            cam.orthographicSize = (referenceAspectHeight / 2.2f) / scale;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}
