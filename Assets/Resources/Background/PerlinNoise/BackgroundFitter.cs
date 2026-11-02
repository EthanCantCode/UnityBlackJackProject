using UnityEngine;

// Attach to a Quad that is a child of your camera.
// It parks the quad near the far clip plane and scales it to fill the view.
[ExecuteAlways]
public class BackgroundFitter : MonoBehaviour
{
    public Camera cam;
    [Range(0.5f, 0.99f)] public float depthPercent = 0.95f;

    void LateUpdate()
    {
        if (cam == null) cam = GetComponentInParent<Camera>();
        if (cam == null) return;

        float dist = cam.farClipPlane * depthPercent;
        transform.localPosition = new Vector3(0, 0, dist);
        transform.localRotation = Quaternion.identity;

        float height = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

        transform.localScale = new Vector3(height * cam.aspect, height, 1f);
    }
}
