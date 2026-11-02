using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Adds "Tools > Build Dashboard", "Tools > Build Bet Controls" and "Tools > Build Hand Values" to the Unity menu.
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
        TMP_FontAsset font = FindFont();
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
        TMP_FontAsset font = FindFont();
        SerializedObject so = new SerializedObject(money);

        // Invisible buttons on top of the green up arrow and red down arrow in the table image
        Button up = MakeButton(holder, "BetUpButton", 0.795f, 0.24f, 0.82f, 0.32f, Color.clear);
        UnityEventTools.AddPersistentListener(up.onClick, money.BetUp);

        Button down = MakeButton(holder, "BetDownButton", 0.964f, 0.24f, 0.99f, 0.32f, Color.clear);
        UnityEventTools.AddPersistentListener(down.onClick, money.BetDown);

        // Bet amount in the light box between the arrows, step size above it
        so.FindProperty("betText").objectReferenceValue = MakeText(holder, "betText", 0.822f, 0.236f, 0.964f, 0.326f, 36, font);
        so.FindProperty("stepText").objectReferenceValue = MakeText(holder, "stepText", 0.80f, 0.333f, 0.984f, 0.389f, 28, font);

        // A row of step buttons ($10 $50 $100 $1000) under the bet amount
        SerializedProperty stepImages = so.FindProperty("stepButtons");
        stepImages.arraySize = money.betSteps.Length;

        float rowLeft = 0.80f;
        float rowRight = 0.984f;
        float gap = 0.004f;
        float width = (rowRight - rowLeft - gap * (money.betSteps.Length - 1)) / money.betSteps.Length;

        for (int i = 0; i < money.betSteps.Length; i++)
        {
            float left = rowLeft + i * (width + gap);
            Button step = MakeButton(holder, "Step" + money.betSteps[i], left, 0.162f, left + width, 0.227f, Color.white);
            UnityEventTools.AddIntPersistentListener(step.onClick, money.SetStep, i);

            TMP_Text label = MakeText((RectTransform)step.transform, "Label", 0, 0, 1, 1, 22, font);
            label.text = "$" + money.betSteps[i];

            stepImages.GetArrayElementAtIndex(i).objectReferenceValue = step.image;
        }

        so.ApplyModifiedProperties();
        Finish(holder, manager, "Bet controls built");
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
        TMP_FontAsset font = FindFont();

        // Dealer's value just under the brown dealer area, player's just above the blue player area
        TMP_Text dealer = MakeText(holder, "dealerValueText", 0.246f, 0.69f, 0.77f, 0.75f, 36, font);
        TMP_Text player = MakeText(holder, "playerValueText", 0.246f, 0.40f, 0.77f, 0.458f, 36, font);
        dealer.color = Color.white;
        player.color = Color.white;

        SerializedObject so = new SerializedObject(display);
        so.FindProperty("cards").objectReferenceValue = cards;
        so.FindProperty("dealerValueText").objectReferenceValue = dealer;
        so.FindProperty("playerValueText").objectReferenceValue = player;
        so.ApplyModifiedProperties();

        Finish(holder, manager, "Hand values built");
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
        text.color = Color.black;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.raycastTarget = false; // let clicks go through to the buttons
        return text;
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

    // First TextMeshPro font asset in Assets/Fonts, if there is one (otherwise TMP's default font is used)
    static TMP_FontAsset FindFont()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Fonts"))
        {
            return null;
        }

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Fonts" });
        if (guids.Length == 0)
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void Finish(RectTransform holder, GameObject manager, string message)
    {
        Undo.RegisterCreatedObjectUndo(holder.gameObject, message);
        EditorSceneManager.MarkSceneDirty(manager.scene);
        Selection.activeGameObject = holder.gameObject;
        Debug.Log(message + " - press Ctrl+S to save the scene");
    }
}
