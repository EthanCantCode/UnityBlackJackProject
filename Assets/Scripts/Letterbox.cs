using UnityEngine;

// Keeps the game at 16:9 on any screen by adding black bars (letterboxing),
// so the UI and the 3D objects always stay lined up
public class Letterbox : MonoBehaviour
{
    // The shape the game was designed for (1920x1080)
    public float targetAspect = 16f / 9f;

    private Camera cam;
    private int lastWidth;
    private int lastHeight;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateRect();
    }

    void Update()
    {
        // Only recalculate when the window size changes (like entering fullscreen)
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            UpdateRect();
        }
    }

    void UpdateRect()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float screenAspect = (float)Screen.width / Screen.height;
        float scale = screenAspect / targetAspect;

        if (scale < 1f)
        {
            // Screen is taller than 16:9: bars on the top and bottom
            cam.rect = new Rect(0f, (1f - scale) / 2f, 1f, scale);
        }
        else
        {
            // Screen is wider than 16:9: bars on the left and right
            float width = 1f / scale;
            cam.rect = new Rect((1f - width) / 2f, 0f, width, 1f);
        }
    }
}