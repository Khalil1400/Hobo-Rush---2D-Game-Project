using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HoboRushUiTypographySync
{
    private const string FontPath = "Assets/HoboRush/Fonts/PressStart2P-Regular.ttf";
    private const string RedButtonPath = "Assets/HoboRush/Art/KenneyPixelUI/9-Slice_Colored_red.png";
    private const string RedButtonPressedPath = "Assets/HoboRush/Art/KenneyPixelUI/9-Slice_Colored_red_pressed.png";
    private static readonly Color Ink = new Color(0.02f, 0.03f, 0.04f, 1f);

    [MenuItem("Tools/Hobo Rush/Apply UI Typography")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    private static void Apply()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        Sprite redButton = AssetDatabase.LoadAssetAtPath<Sprite>(RedButtonPath);
        Sprite redButtonPressed = AssetDatabase.LoadAssetAtPath<Sprite>(RedButtonPressedPath);
        if (font == null || redButton == null || redButtonPressed == null)
        {
            return;
        }

        int updates = 0;
        Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null || !text.gameObject.scene.IsValid())
            {
                continue;
            }

            text.font = font;
            if (ShouldUseInk(text))
            {
                text.color = Ink;
            }

            ApplyGameOverFontSize(text);
            ApplyHudFontSize(text);
            if (text.name == "Final Time")
            {
                text.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(text);
            EditorSceneManager.MarkSceneDirty(text.gameObject.scene);
            updates++;
        }

        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button.name != "Exit Button" || !button.gameObject.scene.IsValid())
            {
                continue;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = redButton;
                EditorUtility.SetDirty(image);
            }

            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = redButton;
            spriteState.selectedSprite = redButton;
            spriteState.pressedSprite = redButtonPressed;
            button.spriteState = spriteState;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.font = font;
                label.color = Ink;
                EditorUtility.SetDirty(label);
            }

            EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
            updates++;
        }

        Image[] images = Resources.FindObjectsOfTypeAll<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || !image.gameObject.scene.IsValid())
            {
                continue;
            }

            if (image.name == "Score HUD Panel" || image.name == "Score HUD Inlay" || image.name == "Game Over Stats Inlay")
            {
                image.enabled = false;
                image.raycastTarget = false;
                EditorUtility.SetDirty(image);
                EditorSceneManager.MarkSceneDirty(image.gameObject.scene);
                updates++;
            }
        }

        RectTransform[] rects = Resources.FindObjectsOfTypeAll<RectTransform>();
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null || rect.name != "Score HUD Panel" || !rect.gameObject.scene.IsValid())
            {
                continue;
            }

            SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(360f, 136f));
            EditorUtility.SetDirty(rect);
            EditorSceneManager.MarkSceneDirty(rect.gameObject.scene);
            updates++;
        }

        if (updates > 0)
        {
            Debug.Log("Applied Hobo Rush UI typography to loaded scene objects: " + updates);
        }
    }

    private static bool ShouldUseInk(Text text)
    {
        return text.name == "Final Score" ||
            text.name == "Score Text" ||
            text.name == "Best Text" ||
            text.text == "RUN ENDED" ||
            text.text == "SCORE" ||
            text.text == "EXIT";
    }

    private static void ApplyGameOverFontSize(Text text)
    {
        if (text.text == "RUN ENDED")
        {
            text.fontSize = 50;
        }
        else if (text.name == "Final Score")
        {
            text.fontSize = 30;
            text.text = "SCORE 0\nBEST: 0";
        }
    }

    private static void ApplyHudFontSize(Text text)
    {
        if (text.name == "Score Label")
        {
            text.alignment = TextAnchor.UpperLeft;
            text.fontSize = 18;
            PrepareHudText(text);
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(150f, 24f));
        }
        else if (text.name == "Score Text")
        {
            text.alignment = TextAnchor.UpperLeft;
            text.fontSize = 44;
            text.text = "0";
            PrepareHudText(text);
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -42f), new Vector2(270f, 50f));
        }
        else if (text.name == "Best Text")
        {
            text.alignment = TextAnchor.UpperLeft;
            text.fontSize = 16;
            PrepareHudText(text);
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -96f), new Vector2(320f, 30f));
        }
    }

    private static void PrepareHudText(Text text)
    {
        text.color = Ink;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = false;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
