using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HealthDemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/HealthDemo.unity";
    private const string ButtonNormalSpritePath = "Assets/My Assets/Fantasy Wooden GUI/TextBTN_Big.png";
    private const string ButtonPressedSpritePath = "Assets/My Assets/Fantasy Wooden GUI/TextBTN_Big_Pressed.png";
    private const string HandCursorPath = "Assets/My Assets/UI/hand_cursor.png";
    private const int MaximumHealth = 100;
    private const int SimulatedDamage = 10;
    private const int SimulatedHeal = 10;
    private const float SmoothFillSpeed = 0.2f;
    private const float ButtonCaptionLift = 16f;
    private const string DeathMessageText = "Юнит умер, лечение не поможет";

    private static readonly Color WoodenTint = new Color(0.7547f, 0.6372f, 0.6372f);
    private static readonly Color SceneBackgroundColor = new Color(0.08f, 0.08f, 0.10f);
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color DeathMessageColor = new Color(0.90f, 0.45f, 0.40f);

    [MenuItem("Tools/Health Demo/Build Demo Scene")]
    public static void BuildFromMenu()
    {
        Build();
    }

    [MenuItem("Tools/Health Demo/Validate Demo Scene")]
    public static void ValidateFromMenu()
    {
        ValidateSavedScene();
    }

    public static void ValidateSavedScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        HealthDemoSceneValidator.Validate(ScenePath);
    }

    public static void Build()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();

        GameObject root = new GameObject("HealthDemo");
        root.SetActive(false);

        Health health = root.AddComponent<Health>();
        SerializedPropertyUtility.SetInteger(health, "_maximum", MaximumHealth);
        SerializedPropertyUtility.SetFloat(health, "_invincibilityTime", 0f);

        HealthSimulator simulator = root.AddComponent<HealthSimulator>();
        SerializedPropertyUtility.SetObjectReference(simulator, "_health", health);
        SerializedPropertyUtility.SetInteger(simulator, "_damageAmount", SimulatedDamage);
        SerializedPropertyUtility.SetInteger(simulator, "_healAmount", SimulatedHeal);

        Canvas canvas = CreateCanvas(root.transform);
        DefaultControls.Resources resources = CreateResources();

        RectTransform panel = CreatePanel(canvas.transform);

        CreateHealthText(panel, health);
        CreateBar(panel, "InstantHealthBar", "Бар здоровья", new Vector2(0f, -240f),
            new Color(0.30f, 0.80f, 0.35f), resources, health, false);
        CreateBar(panel, "SmoothHealthBar", "Плавный бар здоровья", new Vector2(0f, -380f),
            new Color(0.95f, 0.62f, 0.20f), resources, health, true);
        CreateButton(panel, "DamageButton", "Урон -10", new Vector2(-230f, -560f), simulator.TakeDamage);
        CreateButton(panel, "HealButton", "Лечение +10", new Vector2(230f, -560f), simulator.Heal);
        CreateDeathMessage(panel, health);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        root.SetActive(true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AddSceneToBuildSettings();

        Debug.Log($"[HealthDemoSceneBuilder] Scene saved to {ScenePath}.");
    }

    private static void AddSceneToBuildSettings()
    {
        if (ContainsScene(EditorBuildSettings.scenes))
        {
            return;
        }

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static bool ContainsScene(EditorBuildSettingsScene[] scenes)
    {
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == ScenePath)
            {
                return true;
            }
        }

        return false;
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private static RectTransform CreatePanel(Transform parent)
    {
        RectTransform panel = CreateElement("Panel", parent);
        AnchorTop(panel, new Vector2(0f, -110f), new Vector2(1240f, 860f));

        Image image = panel.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = PanelColor;
        return panel;
    }

    private static void CreateHealthText(Transform parent, Health health)
    {
        RectTransform rect = CreateElement("HealthText", parent);
        AnchorTop(rect, new Vector2(0f, -80f), new Vector2(500f, 70f));

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = $"{health.Maximum}/{health.Maximum}";
        label.fontSize = 54f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;

        HealthText view = rect.gameObject.AddComponent<HealthText>();
        SerializedPropertyUtility.SetObjectReference(view, "_health", health);
        SerializedPropertyUtility.SetObjectReference(view, "_text", label);
    }

    private static void CreateBar(Transform parent, string name, string caption, Vector2 position,
        Color fillColor, DefaultControls.Resources resources, Health health, bool smooth)
    {
        CreateBarCaption(parent, caption, new Vector2(position.x, position.y + 40f));

        GameObject sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.name = name;
        sliderObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)sliderObject.transform;
        AnchorTop(rect, position, new Vector2(1000f, 50f));

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.maxValue = MaximumHealth;
        slider.value = MaximumHealth;

        Image background = sliderObject.transform.Find("Background").GetComponent<Image>();
        background.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        Image fill = sliderObject.transform.Find("Fill Area/Fill").GetComponent<Image>();
        fill.color = fillColor;

        HealthBar view;
        if (smooth)
        {
            view = sliderObject.AddComponent<SmoothHealthBar>();
            SerializedPropertyUtility.SetFloat(view, "_fillSpeed", SmoothFillSpeed);
        }
        else
        {
            view = sliderObject.AddComponent<HealthBar>();
        }

        SerializedPropertyUtility.SetObjectReference(view, "_health", health);
        SerializedPropertyUtility.SetObjectReference(view, "_slider", slider);
    }

    private static void CreateCaption(Transform parent, string name, string text, float fontSize, Color color, float bottomOffset)
    {
        RectTransform rect = CreateElement(name, parent);
        Stretch(rect);
        rect.offsetMin = new Vector2(0f, bottomOffset);

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void CreateBarCaption(Transform parent, string text, Vector2 position)
    {
        RectTransform rect = CreateElement($"{text}Caption", parent);
        AnchorTop(rect, position, new Vector2(400f, 30f));
        CreateCaption(rect, "Label", text, 26f, new Color(0.82f, 0.82f, 0.82f), 0f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SceneBackgroundColor;
    }

    private static void CreateButton(Transform parent, string name, string caption, Vector2 position, UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(HoverCursor));
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.SetParent(parent, false);
        AnchorTop(rect, position, new Vector2(336f, 112f));

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonNormalSpritePath);
        image.color = WoodenTint;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.spriteState = new SpriteState
        {
            pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPressedSpritePath)
        };

        HoverCursor hoverCursor = buttonObject.GetComponent<HoverCursor>();
        SerializedPropertyUtility.SetObjectReference(hoverCursor, "_handCursor", AssetDatabase.LoadAssetAtPath<Texture2D>(HandCursorPath));

        CreateCaption(rect, "Caption", caption, 32f, Color.white, ButtonCaptionLift);
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
    }

    private static void CreateDeathMessage(Transform parent, Health health)
    {
        RectTransform rect = CreateElement("DeathMessage", parent);
        AnchorTop(rect, new Vector2(0f, -700f), new Vector2(1000f, 50f));

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = DeathMessageText;
        label.fontSize = 30f;
        label.color = DeathMessageColor;
        label.alignment = TextAlignmentOptions.Center;
        label.enabled = false;

        DeathMessage view = rect.gameObject.AddComponent<DeathMessage>();
        SerializedPropertyUtility.SetObjectReference(view, "_health", health);
        SerializedPropertyUtility.SetObjectReference(view, "_text", label);
    }

    private static RectTransform CreateElement(string name, Transform parent)
    {
        GameObject element = new GameObject(name, typeof(RectTransform));
        element.transform.SetParent(parent, false);

        return (RectTransform)element.transform;
    }

    private static void AnchorTop(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static DefaultControls.Resources CreateResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }
}
