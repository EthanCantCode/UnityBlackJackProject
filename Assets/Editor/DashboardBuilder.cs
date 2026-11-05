using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using TMPro;

// Adds "Tools > Build Dashboard", "Tools > Build Bet Controls", "Tools > Build Hand Values",
// "Tools > Build Hit Stand Buttons", "Tools > Build Result Banner" and "Tools > Apply Pixel Font" to the Unity menu.
// They place UI on top of the table image and plug it into the scripts on the GameManager.
// All positions are fractions of the table image (x: 0 = left, 1 = right; y: 0 = bottom, 1 = top),
// so the UI stays on its box at any screen size.
public static class DashboardBuilder
{
    // Left and right edges of the dashboard boxes
    const float minX = 0.04f;
    const float maxX = 0.193f;

    // moneyScore field, then the box's bottom and top edges
    static readonly (string field, float minY, float maxY)[] boxes =
    {
        ("walletText",    0.754f, 0.924f), // orange
        ("payoutText",    0.646f, 0.741f), // orange
        ("winAmtText",    0.535f, 0.630f), // orange
        ("betMinText",    0.442f, 0.519f), // red
        ("multText",      0.350f, 0.428f), // blue
        ("roundText",     0.217f, 0.300f), // grey
        ("highscoreText", 0.051f, 0.194f), // yellow
    };

    [MenuItem("Tools/Build Dashboard")]
    static void Build()
    {
        if (!FindTargets(out GameObject table, out GameObject manager, out moneyScore money))
        {
            return;
        }

        RectTransform holder = MakeHolder(table, "DashboardBoxes");
        TMP_FontAsset font = LoadOrCreatePixelFont();
        SerializedObject so = new SerializedObject(money);

        foreach (var box in boxes)
        {
            TMP_Text text = MakeText(holder, box.field, minX, box.minY, maxX, box.maxY, 30, font);
            text.text = box.field;
            so.FindProperty(box.field).objectReferenceValue = text;
        }

        so.ApplyModifiedProperties();
        Finish(holder, manager, "Dashboard built");
    }

    [MenuItem("Tools/Build Bet Controls")]
    static void BuildBetControls()
    {
        if (!FindTargets(out GameObject table, out GameObject manager, out moneyScore money))
        {
            return;
        }

        RectTransform holder = MakeHolder(table, "BetControls");
        TMP_FontAsset font = LoadOrCreatePixelFont();
        SerializedObject so = new SerializedObject(money);

        // Invisible buttons on top of the green up arrow and red down arrow in the table image
        Button up = MakeButton(holder, "BetUpButton", 0.795f, 0.24f, 0.82f, 0.32f, Color.clear);
        UnityEventTools.AddPersistentListener(up.onClick, money.BetUp);

        Button down = MakeButton(holder, "BetDownButton", 0.964f, 0.24f, 0.99f, 0.32f, Color.clear);
        UnityEventTools.AddPersistentListener(down.onClick, money.BetDown);

        // Bet amount in the light box between the arrows, step size above it
        so.FindProperty("betText").objectReferenceValue = MakeText(holder, "betText", 0.822f, 0.236f, 0.964f, 0.326f, 36, font);
        so.FindProperty("stepText").objectReferenceValue = MakeText(holder, "stepText", 0.80f, 0.333f, 0.984f, 0.389f, 28, font);

        // A row of step buttons (10 50 100 1K) under the bet amount
        SerializedProperty stepImages = so.FindProperty("stepButtons");
        stepImages.arraySize = money.betSteps.Length;

        float rowLeft = 0.80f;
        float rowRight = 0.984f;
        float gap = 0.004f;
        float width = (rowRight - rowLeft - gap * (money.betSteps.Length - 1)) / money.betSteps.Length;

        // Dark grey buttons so the white numbers stand out; the picked step still turns gold
        Color stepColor = new Color(0.3f, 0.3f, 0.32f);
        so.FindProperty("stepNormalColor").colorValue = stepColor;

        // Biggest text size that fits the longest label ("100") in a button. Press Start 2P is monospaced
        // (every character is exactly one font size wide), so it can be worked out exactly.
        // Rounded down to a multiple of 8, the font's pixel grid, so the pixels stay even
        int longest = 0;
        foreach (int amount in money.betSteps)
        {
            longest = Mathf.Max(longest, ShortAmount(amount).Length);
        }
        float buttonWidth = table.GetComponent<RectTransform>().rect.width * width;
        float labelWidth = buttonWidth - 2 * ButtonPadding(buttonWidth);
        float stepFontSize = Mathf.Max(8, Mathf.Floor(labelWidth / longest / 8) * 8);

        for (int i = 0; i < money.betSteps.Length; i++)
        {
            float left = rowLeft + i * (width + gap);
            Button step = MakeButton(holder, "Step" + money.betSteps[i], left, 0.162f, left + width, 0.227f, stepColor);
            UnityEventTools.AddIntPersistentListener(step.onClick, money.SetStep, i);

            // Short labels with no "$", all four at the same size
            TMP_Text label = MakeText((RectTransform)step.transform, "Label", 0, 0, 1, 1, stepFontSize, font);
            label.text = ShortAmount(money.betSteps[i]);

            stepImages.GetArrayElementAtIndex(i).objectReferenceValue = step.image;
        }

        so.ApplyModifiedProperties();
        Finish(holder, manager, "Bet controls built");
    }

    // 1000 -> "1K", 5000 -> "5K"; anything else stays as it is (10, 50, 100)
    static string ShortAmount(int amount)
    {
        if (amount >= 1000 && amount % 1000 == 0)
        {
            return amount / 1000 + "K";
        }
        return amount.ToString();
    }

    [MenuItem("Tools/Build Hand Values")]
    static void BuildHandValues()
    {
        if (!FindTargets(out GameObject table, out GameObject manager, out moneyScore money))
        {
            return;
        }

        GameObject spawner = GameObject.Find("CardSpawner");
        randomFace cards = spawner != null ? spawner.GetComponent<randomFace>() : null;
        if (cards == null)
        {
            Debug.LogError("Need a 'CardSpawner' object with randomFace on it");
            return;
        }

        // Add handDisplay to the GameManager if it isn't there yet
        handDisplay display = manager.GetComponent<handDisplay>();
        if (display == null)
        {
            display = Undo.AddComponent<handDisplay>(manager);
        }

        RectTransform holder = MakeHolder(table, "HandValues");
        TMP_FontAsset font = LoadOrCreatePixelFont();

        // Dealer's value just under the brown dealer area, player's just above the blue player area
        TMP_Text dealer = MakeText(holder, "dealerValueText", 0.246f, 0.69f, 0.77f, 0.75f, 36, font);
        TMP_Text player = MakeText(holder, "playerValueText", 0.246f, 0.40f, 0.77f, 0.458f, 36, font);

        SerializedObject so = new SerializedObject(display);
        so.FindProperty("cards").objectReferenceValue = cards;
        so.FindProperty("dealerValueText").objectReferenceValue = dealer;
        so.FindProperty("playerValueText").objectReferenceValue = player;
        so.ApplyModifiedProperties();

        Finish(holder, manager, "Hand values built");
    }

    [MenuItem("Tools/Build Hit Stand Buttons")]
    static void BuildHitStandButtons()
    {
        if (!FindTargets(out GameObject table, out GameObject manager, out moneyScore money))
        {
            return;
        }

        GameObject spawner = GameObject.Find("CardSpawner");
        randomFace cards = spawner != null ? spawner.GetComponent<randomFace>() : null;
        if (cards == null)
        {
            Debug.LogError("Need a 'CardSpawner' object with randomFace on it");
            return;
        }

        // Add roundControl (Deal, Hit and Stand) to the GameManager if it isn't there yet
        roundControl round = manager.GetComponent<roundControl>();
        if (round == null)
        {
            round = Undo.AddComponent<roundControl>(manager);
        }

        RectTransform holder = MakeHolder(table, "HitStandButtons");
        TMP_FontAsset font = LoadOrCreatePixelFont();

        // Deal, Hit and Stand side by side at the bottom middle of the table
        Button deal = MakeButton(holder, "DealButton", 0.345f, 0.03f, 0.435f, 0.11f, new Color(0.2f, 0.4f, 0.8f));
        UnityEventTools.AddPersistentListener(deal.onClick, round.Deal);

        Button hit = MakeButton(holder, "HitButton", 0.455f, 0.03f, 0.545f, 0.11f, new Color(0.2f, 0.65f, 0.3f));
        UnityEventTools.AddPersistentListener(hit.onClick, round.Hit);

        Button stand = MakeButton(holder, "StandButton", 0.565f, 0.03f, 0.655f, 0.11f, new Color(0.8f, 0.25f, 0.2f));
        UnityEventTools.AddPersistentListener(stand.onClick, round.Stand);

        foreach (var (button, text) in new[] { (deal, "DEAL"), (hit, "HIT"), (stand, "STAND") })
        {
            TMP_Text label = MakeText((RectTransform)button.transform, "Label", 0, 0, 1, 1, 36, font);
            label.text = text;
        }

        SerializedObject so = new SerializedObject(round);
        so.FindProperty("cards").objectReferenceValue = cards;
        so.FindProperty("money").objectReferenceValue = money;
        so.FindProperty("dealButton").objectReferenceValue = deal;
        so.FindProperty("hitButton").objectReferenceValue = hit;
        so.FindProperty("standButton").objectReferenceValue = stand;

        // The left hand sweeps the table between rounds (its NewRound clip)
        GameObject leftHand = GameObject.Find("leftHand");
        if (leftHand != null)
        {
            so.FindProperty("leftHand").objectReferenceValue = leftHand.GetComponent<Animation>();
        }
        else
        {
            Debug.LogWarning("No 'leftHand' object found - the table will clear without the sweep");
        }
        so.ApplyModifiedProperties();

        Finish(holder, manager, "Deal, Hit and Stand buttons built");
    }

    [MenuItem("Tools/Build Result Banner")]
    static void BuildResultBanner()
    {
        if (!FindTargets(out GameObject table, out GameObject manager, out moneyScore money))
        {
            return;
        }

        roundControl round = manager.GetComponent<roundControl>();
        if (round == null)
        {
            Debug.LogError("Run 'Tools > Build Hit Stand Buttons' first - it adds roundControl to the GameManager");
            return;
        }

        RectTransform holder = MakeHolder(table, "ResultBanner");

        // Rounded dark box in the center of the table that grows to fit whatever text is in it
        GameObject box = new GameObject("ResultBox", typeof(RectTransform), typeof(Image), typeof(CanvasGroup),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.SetParent(holder, false);
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;

        Image background = box.GetComponent<Image>();
        background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        background.type = Image.Type.Sliced;
        background.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

        CanvasGroup group = box.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false; // don't block clicks on the table

        HorizontalLayoutGroup layout = box.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(60, 60, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = box.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Big text inside the box; roundControl adds the pop/fade
        TMP_Text text = MakeText(boxRect, "ResultText", 0, 0, 1, 1, resultFontSize, LoadOrCreatePixelFont());
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.text = "WIN +$100"; // placeholder so it can be seen in the editor; hidden when the game starts

        SerializedObject so = new SerializedObject(round);
        so.FindProperty("resultBox").objectReferenceValue = group;
        so.FindProperty("resultText").objectReferenceValue = text;
        so.ApplyModifiedProperties();

        Finish(holder, manager, "Result banner built");
    }

    const string pixelFontFile = "Assets/Fonts/PressStart2P-Regular.ttf";
    const string pixelFontAsset = "Assets/Fonts/PressStart2P Outlined.asset";

    // Press Start 2P is very wide, so the result text is smaller than a normal font would be
    const float resultFontSize = 64;

    // Extra space between lines, in hundredths of the font size. Press Start 2P letters are 8 pixels tall,
    // so 25 (a quarter) is a gap of 2 of the font's own pixels, and it stays 2 font pixels at any text size
    const float lineGap = 25;

    // Space kept between a button's label and its left/right edges (top and bottom get half)
    const float buttonPadding = 12;

    // Small buttons (like the bet steps) get less padding, so their text isn't squeezed down to nothing
    static float ButtonPadding(float buttonWidth)
    {
        return Mathf.Min(buttonPadding, buttonWidth * 0.06f);
    }

    // Puts the pixel font on every piece of UI text on the table (dashboard, bet controls,
    // hand values, button labels and the result banner): white letters with a black outline.
    // The result text keeps its win/lose color. Safe to run again after a rebuild.
    [MenuItem("Tools/Apply Pixel Font")]
    static void ApplyPixelFont()
    {
        TMP_FontAsset font = LoadOrCreatePixelFont();
        if (font == null || !FindTargets(out GameObject table, out GameObject manager, out moneyScore money))
        {
            return;
        }

        roundControl round = manager.GetComponent<roundControl>();
        TMP_Text resultText = round != null ? round.resultText : null;

        TMP_Text[] texts = table.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            Undo.RecordObject(text, "Apply Pixel Font");
            text.font = font;
            text.fontSharedMaterial = font.material;
            if (text == resultText)
            {
                text.fontSize = resultFontSize;
            }
            StyleText(text);
        }

        EditorSceneManager.MarkSceneDirty(manager.scene);
        Debug.Log("Pixel font applied to " + texts.Length + " texts - press Ctrl+S to save the scene");
    }

    // Turns the .ttf into a TextMeshPro font asset the first time
    static TMP_FontAsset LoadOrCreatePixelFont()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(pixelFontAsset);
        if (existing != null)
        {
            return existing;
        }

        Font source = AssetDatabase.LoadAssetAtPath<Font>(pixelFontFile);
        if (source == null)
        {
            Debug.LogError("Can't find " + pixelFontFile);
            return null;
        }

        // SDF mode is the only kind TextMeshPro can outline. Press Start 2P is drawn on an 8 pixel grid,
        // so sampling at 64 keeps every font pixel 8x8 in the atlas and the letters stay blocky
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 1024, 1024);
        font.name = System.IO.Path.GetFileNameWithoutExtension(pixelFontAsset);

        // Bake in every normal keyboard character, then lock it so nothing changes at runtime
        string characters = "";
        for (char c = ' '; c <= '~'; c++)
        {
            characters += c;
        }
        font.TryAddCharacters(characters);
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(font, pixelFontAsset);

        // The atlas and material have to be saved inside the font asset, or they're lost on reload
        Texture2D atlas = font.atlasTexture;
        atlas.name = font.name + " Atlas";
        AssetDatabase.AddObjectToAsset(atlas, font);

        // Black outline; the dilate grows the letters back so the outline doesn't eat into them
        Material material = font.material;
        material.name = font.name + " Material";
        material.EnableKeyword("OUTLINE_ON");
        material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
        AssetDatabase.AddObjectToAsset(material, font);

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return font;
    }

    static bool FindTargets(out GameObject table, out GameObject manager, out moneyScore money)
    {
        table = null;
        manager = null;
        money = null;

        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play mode first - anything built while playing gets deleted");
            return false;
        }

        table = GameObject.Find("Background/Image");
        manager = GameObject.Find("GameManager");
        if (manager != null)
        {
            money = manager.GetComponent<moneyScore>();
        }

        if (table == null || money == null)
        {
            Debug.LogError("Need a 'Background/Image' object and a 'GameManager' with moneyScore on it");
            return false;
        }

        // Scale the whole canvas with the screen (designed at the table image's 1920x1080),
        // so the UI grows and shrinks with the table instead of staying a fixed pixel size
        CanvasScaler scaler = table.GetComponentInParent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Build UI");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        return true;
    }

    // Empty object covering the whole table image. Replaces an older one with the same name so builds can be rerun
    static RectTransform MakeHolder(GameObject table, string name)
    {
        Transform old = table.transform.Find(name);
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        GameObject holder = new GameObject(name, typeof(RectTransform));
        RectTransform rect = holder.GetComponent<RectTransform>();
        rect.SetParent(table.transform, false);
        Place(rect, 0, 0, 1, 1);
        return rect;
    }

    static void Place(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static TMP_Text MakeText(RectTransform parent, string name, float left, float bottom, float right, float top, float size, TMP_FontAsset font)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Place(rect, left, bottom, right, top);

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.raycastTarget = false; // let clicks go through to the buttons
        StyleText(text);
        return text;
    }

    // The pixel font look, used by every build tool and by Apply Pixel Font so they always match.
    // White letters (the font itself draws the black outline), a gap under labels, and text that shrinks to fit its box
    static void StyleText(TMP_Text text)
    {
        text.fontStyle &= ~FontStyles.Bold; // the pixel font is thick enough on its own
        text.color = Color.white;

        // Gap between a label and its number (like "Wallet" and "$1000" below it)
        text.lineSpacing = lineGap;

        // Button labels (DEAL, HIT, STAND, bet steps): keep them on one line with some padding,
        // so they shrink to fit inside the button. The outline draws a little past the letters,
        // so without padding a label that "fits" still pokes out the edges
        if (text.GetComponentInParent<Button>() != null)
        {
            float pad = ButtonPadding(text.rectTransform.rect.width);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(pad, pad / 2, pad, pad / 2);
        }

        if (text.GetComponentInParent<LayoutGroup>() != null)
        {
            // The result box sizes itself to the text, so the text keeps one fixed size
            text.enableAutoSizing = false;
        }
        else if (!text.enableAutoSizing)
        {
            // The pixel font is wide, so let text shrink to fit its box instead of spilling out.
            // Its current size becomes the largest it can be
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = 8;
            text.enableAutoSizing = true;
        }
    }

    static Button MakeButton(RectTransform parent, string name, float left, float bottom, float right, float top, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Place(rect, left, bottom, right, top);

        // A clear image is still clickable, which is how the arrows drawn on the table become buttons
        Image image = obj.GetComponent<Image>();
        image.color = color;

        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    static void Finish(RectTransform holder, GameObject manager, string message)
    {
        Undo.RegisterCreatedObjectUndo(holder.gameObject, message);
        EditorSceneManager.MarkSceneDirty(manager.scene);
        Selection.activeGameObject = holder.gameObject;
        Debug.Log(message + " - press Ctrl+S to save the scene");
    }
}
