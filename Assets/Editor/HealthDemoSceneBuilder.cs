using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class HealthDemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/HealthDemo.unity";
    private const string TmpEssentialsResource = "TMP Settings";
    private const int MaxHealth = 100;
    private const int ChangeAmount = 10;

    private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

    [MenuItem("Tools/Health Demo/Build Demo Scene")]
    public static void Build()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
        {
            return;
        }

        ImportTmpEssentials();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Health health = BuildHealth();
        Canvas canvas = BuildCanvas();
        TMP_FontAsset font = FindDefaultFont();

        BuildLabel(canvas.transform, "Title", "Health Demo", 36, TextAlignmentOptions.Center, font, new Vector2(0f, 250f), new Vector2(600f, 60f));
        BuildTextIndicator(canvas, font, health);
        BuildLabel(canvas.transform, "Instant Caption", "Instant", 22, TextAlignmentOptions.Right, font, new Vector2(-310f, 70f), new Vector2(120f, 26f));
        BuildLabel(canvas.transform, "Smooth Caption", "Smooth", 22, TextAlignmentOptions.Right, font, new Vector2(-310f, 20f), new Vector2(120f, 26f));

        Slider instantBar = BuildSlider(canvas, "Instant Bar", new Vector2(0f, 70f), new Color(0.25f, 0.70f, 0.30f));
        Slider smoothBar = BuildSlider(canvas, "Smooth Bar", new Vector2(0f, 20f), new Color(0.30f, 0.60f, 0.90f));

        AttachBarView<HealthBar>(instantBar, health);
        AttachBarView<SmoothHealthBar>(smoothBar, health);
        BuildSimulator(canvas, font, health);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        EditorUtility.DisplayDialog("Health Demo", $"Scene built: {ScenePath}\n\nPress Play and use the buttons.", "OK");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
    }

    private static void ImportTmpEssentials()
    {
        if (Resources.Load<TMP_Settings>(TmpEssentialsResource) == null)
        {
            ImportTmpEssentialsPackage();
        }
    }

    private static void ImportTmpEssentialsPackage()
    {
        PackageInfo package = PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro");

        if (package == null)
        {
            Debug.LogError("TextMesh Pro package is missing. Import TMP Essential Resources manually: Window → TextMeshPro → Import TMP Essential Resources.");
            return;
        }

        string essentialsPath = Path.Combine(package.assetPath, "Package Resources", "TMP Essential Resources.unitypackage");

        if (File.Exists(essentialsPath) == false)
        {
            Debug.LogError($"TMP essentials not found at {essentialsPath}. Import them manually: Window → TextMeshPro → Import TMP Essential Resources.");
            return;
        }

        AssetDatabase.ImportPackage(essentialsPath, false);
    }

    private static Health BuildHealth()
    {
        GameObject player = new GameObject("Player");
        Health health = player.AddComponent<Health>();
        SetValue(health, "_maximum", MaxHealth);
        SetValue(health, "_invincibilityTime", 0f);
        return health;
    }

    private static Canvas BuildCanvas()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void BuildTextIndicator(Canvas canvas, TMP_FontAsset font, Health health)
    {
        TextMeshProUGUI label = BuildLabel(canvas.transform, "Health Text", $"{MaxHealth}/{MaxHealth}", 56, TextAlignmentOptions.Center, font, new Vector2(0f, 150f), new Vector2(400f, 70f));
        HealthText view = label.gameObject.AddComponent<HealthText>();
        SetReference(view, "_health", health);
        SetReference(view, "_text", label);
    }

    private static Slider BuildSlider(Canvas canvas, string name, Vector2 position, Color fillColor)
    {
        GameObject sliderGo = new GameObject(name, typeof(RectTransform), typeof(Slider));
        RectTransform sliderRect = (RectTransform)sliderGo.transform;
        sliderRect.SetParent(canvas.transform, false);
        Place(sliderRect, position, new Vector2(480f, 24f));

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Image background = BuildImage(sliderRect, "Background", uiSprite, new Color(0.18f, 0.18f, 0.18f));
        Stretch(background.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));

        RectTransform fillArea = CreateRect(sliderRect, "Fill Area");
        Stretch(fillArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));
        fillArea.offsetMin = new Vector2(10f, 0f);
        fillArea.offsetMax = new Vector2(-10f, 0f);

        Image fill = BuildImage(fillArea, "Fill", uiSprite, fillColor);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one);

        RectTransform handleArea = CreateRect(sliderRect, "Handle Slide Area");
        Stretch(handleArea, Vector2.zero, Vector2.one);
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);

        Image handle = BuildImage(handleArea, "Handle", uiSprite, Color.white);
        handle.rectTransform.anchorMin = new Vector2(0f, 0f);
        handle.rectTransform.anchorMax = new Vector2(0f, 1f);
        handle.rectTransform.sizeDelta = new Vector2(20f, -4f);

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = true;
        return slider;
    }

    private static void BuildSimulator(Canvas canvas, TMP_FontAsset font, Health health)
    {
        GameObject simulatorGo = new GameObject("Simulator");
        HealthSimulator simulator = simulatorGo.AddComponent<HealthSimulator>();
        SetReference(simulator, "_health", health);
        SetValue(simulator, "_damageAmount", ChangeAmount);
        SetValue(simulator, "_healAmount", ChangeAmount);

        Button damageButton = BuildButton(canvas.transform, "Damage Button", "Damage -10", new Vector2(-130f, -140f), new Color(0.80f, 0.30f, 0.25f), font);
        Button healButton = BuildButton(canvas.transform, "Heal Button", "Heal +10", new Vector2(130f, -140f), new Color(0.35f, 0.70f, 0.35f), font);

        UnityEventTools.AddPersistentListener(damageButton.onClick, simulator.TakeDamage);
        UnityEventTools.AddPersistentListener(healButton.onClick, simulator.Heal);
    }

    private static Button BuildButton(Transform parent, string name, string caption, Vector2 position, Color color, TMP_FontAsset font)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)buttonGo.transform;
        buttonRect.SetParent(parent, false);
        Place(buttonRect, position, new Vector2(200f, 56f));

        Image image = buttonGo.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = color;

        CreateLabel(buttonRect, "Caption", caption, 26, TextAlignmentOptions.Center, font);

        return buttonGo.GetComponent<Button>();
    }

    private static T AttachBarView<T>(Slider slider, Health health) where T : HealthBar
    {
        T view = slider.gameObject.AddComponent<T>();
        SetReference(view, "_health", health);
        SetReference(view, "_slider", slider);
        return view;
    }

    private static TextMeshProUGUI BuildLabel(Transform parent, string name, string caption, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        TextMeshProUGUI label = CreateLabel(parent, name, caption, fontSize, alignment, font);
        Place((RectTransform)label.transform, position, size);
        return label;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string caption, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        GameObject labelGo = new GameObject(name, typeof(RectTransform));
        RectTransform labelRect = (RectTransform)labelGo.transform;
        labelRect.SetParent(parent, false);
        Stretch(labelRect, Vector2.zero, Vector2.one);

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        AssignFont(label, font);
        label.text = caption;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    private static Image BuildImage(Transform parent, string name, Sprite sprite, Color color)
    {
        Image image = CreateRect(parent, name).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject rectGo = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)rectGo.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static TMP_FontAsset FindDefaultFont()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

            if (font == null)
            {
                continue;
            }

            if (font.name.Contains("LiberationSans"))
            {
                return font;
            }
        }

        return null;
    }

    private static void AssignFont(TMP_Text label, TMP_FontAsset font)
    {
        if (font == null)
        {
            return;
        }

        label.font = font;
    }

    private static void SetReference(Object owner, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(owner);
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogError($"{owner.GetType().Name} has no serialized property {propertyName}.");
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetValue(Object owner, string propertyName, int value)
    {
        SerializedObject serialized = new SerializedObject(owner);
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogError($"{owner.GetType().Name} has no serialized property {propertyName}.");
            return;
        }

        property.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetValue(Object owner, string propertyName, float value)
    {
        SerializedObject serialized = new SerializedObject(owner);
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogError($"{owner.GetType().Name} has no serialized property {propertyName}.");
            return;
        }

        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = CenterAnchor;
        rect.anchorMax = CenterAnchor;
        rect.pivot = CenterAnchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
