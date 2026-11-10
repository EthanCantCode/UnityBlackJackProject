using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Game over screen: dims the table and pops in a panel with how the game went and a PLAY AGAIN button.
// It's built in code when the game ends (nothing to set up in the scene), and copies the font and box
// look from the result banner so it matches the rest of the table UI.
//   roundControl calls gameOverScreen.Show(...) once the table has cleared after the last round
public class gameOverScreen : MonoBehaviour
{
    public float fadeTime = 0.35f;        // dim layer fading in
    public float popTime = 0.25f;         // panel popping in, from big to normal size
    public float buttonPulseSize = 0.05f;
    public float buttonPulseSpeed = 4f;

    static readonly Color red = new Color(1f, 0.35f, 0.3f);
    static readonly Color gold = new Color(1f, 0.85f, 0.2f);
    static readonly Color green = new Color(0.2f, 0.65f, 0.3f);
    static readonly Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);

    CanvasGroup overlay;
    RectTransform panel;
    RectTransform button;
    float shownAt;

    // canvas       where the screen goes (it covers the whole canvas)
    // textStyle    text to copy the font and outline from (the result banner text)
    // boxStyle     image to copy the rounded box sprite from (the result banner box)
    public static gameOverScreen Show(Canvas canvas, TMP_Text textStyle, Image boxStyle,
        int rounds, int bestWallet, int highScore, bool newHighScore, UnityAction onPlayAgain)
    {
        // Dim layer over everything; it also catches clicks so the table underneath can't be used
        GameObject root = new GameObject("GameOverScreen", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        gameOverScreen screen = root.AddComponent<gameOverScreen>();
        screen.overlay = root.GetComponent<CanvasGroup>();
        screen.overlay.alpha = 0f;
        screen.shownAt = Time.unscaledTime;

        // Panel in the middle that sizes itself to what's in it, top to bottom
        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        screen.panel = (RectTransform)panelObj.transform;
        screen.panel.SetParent(rootRect, false);
        screen.panel.anchorMin = screen.panel.anchorMax = screen.panel.pivot = new Vector2(0.5f, 0.5f);
        screen.panel.anchoredPosition = Vector2.zero;
        StyleBox(panelObj.GetComponent<Image>(), boxStyle, panelColor);

        VerticalLayoutGroup layout = panelObj.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(80, 80, 56, 56);
        layout.spacing = 22;
        FitToContent(layout, panelObj.GetComponent<ContentSizeFitter>());

        // Sizes are based on the result banner's text, so the screen scales with the rest of the UI
        float big = textStyle != null ? textStyle.fontSize : 64f;
        float small = big * 0.4f;

        AddText(screen.panel, textStyle, "GAME OVER", big, red);
        AddSpace(screen.panel, big * 0.3f);
        AddText(screen.panel, textStyle, "Rounds played  " + rounds, small, Color.white);
        AddText(screen.panel, textStyle, "Best wallet  $" + bestWallet, small, Color.white);
        AddText(screen.panel, textStyle, (newHighScore ? "NEW HIGH SCORE  $" : "High score  $") + highScore, small,
            newHighScore ? gold : new Color(0.75f, 0.75f, 0.8f));
        AddSpace(screen.panel, big * 0.4f);

        // PLAY AGAIN button, the same green as HIT
        GameObject buttonObj = new GameObject("PlayAgainButton", typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(HorizontalLayoutGroup));
        screen.button = (RectTransform)buttonObj.transform;
        screen.button.SetParent(screen.panel, false);
        Image buttonImage = buttonObj.GetComponent<Image>();
        StyleBox(buttonImage, boxStyle, green);

        HorizontalLayoutGroup buttonLayout = buttonObj.GetComponent<HorizontalLayoutGroup>();
        buttonLayout.padding = new RectOffset(48, 48, 26, 26);
        FitToContent(buttonLayout, null);

        Button playAgain = buttonObj.GetComponent<Button>();
        playAgain.targetGraphic = buttonImage;
        playAgain.onClick.AddListener(onPlayAgain);

        AddText(screen.button, textStyle, "PLAY AGAIN", big * 0.5f, Color.white);

        return screen;
    }

    void Update()
    {
        float t = Time.unscaledTime - shownAt;

        // Fade the dim layer (and the panel with it) in, and pop the panel from big to normal size
        overlay.alpha = Mathf.Clamp01(t / fadeTime);
        float k = Mathf.Clamp01(t / popTime);
        float ease = 1f - (1f - k) * (1f - k);
        panel.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, ease);

        // Pulse PLAY AGAIN once the panel has landed, like DEAL does when it's ready
        if (k >= 1f)
        {
            button.localScale = Vector3.one * (1f + Mathf.Sin((t - popTime) * buttonPulseSpeed) * buttonPulseSize);
        }
    }

    static void AddText(Transform parent, TMP_Text style, string text, float size, Color color)
    {
        TMP_Text box;
        if (style != null)
        {
            // Copy the banner text so the pixel font and its outline come with it
            box = Instantiate(style, parent, false);
            box.gameObject.SetActive(true);
        }
        else
        {
            box = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            box.transform.SetParent(parent, false);
        }

        box.name = text;
        box.text = text;
        box.fontSize = size;
        box.color = color;
        box.enableAutoSizing = false;
        box.textWrappingMode = TextWrappingModes.NoWrap;
        box.alignment = TextAlignmentOptions.Center;
        box.raycastTarget = false; // let clicks through to the button
        box.transform.localScale = Vector3.one;
    }

    static void AddSpace(Transform parent, float height)
    {
        GameObject space = new GameObject("Space", typeof(RectTransform), typeof(LayoutElement));
        space.transform.SetParent(parent, false);
        space.GetComponent<LayoutElement>().preferredHeight = height;
    }

    // Same rounded box as the result banner, in the given color
    static void StyleBox(Image image, Image style, Color color)
    {
        if (style != null)
        {
            image.sprite = style.sprite;
            image.type = style.type;
            image.pixelsPerUnitMultiplier = style.pixelsPerUnitMultiplier;
        }
        image.color = color;
    }

    // Children keep their own size and sit in the middle; the fitter (if any) shrinks the box around them
    static void FitToContent(HorizontalOrVerticalLayoutGroup layout, ContentSizeFitter fitter)
    {
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
