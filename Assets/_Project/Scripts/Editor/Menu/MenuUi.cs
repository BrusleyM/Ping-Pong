using PingPong.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    internal static class MenuUi
    {
        internal static UiTheme Theme;

        internal static RectTransform RectOf(GameObject go)
        {
            var rect = go.transform as RectTransform;
            return rect != null ? rect : go.AddComponent<RectTransform>();
        }

        internal static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = GetOrCreate(name, parent);
            var rect = RectOf(go);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        internal static RectTransform Stretch(Transform parent, string name)
        {
            var rect = Rect(parent, name, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal static Image Picture(Transform parent, string name, string sprite, Vector2 position, Vector2 size, Color? tint = null)
        {
            var rect = Rect(parent, name, position, size);
            var image = GetOrAdd<Image>(rect.gameObject);
            image.sprite = sprite != null ? MenuAssetsBuilder.SpriteAt(sprite) : null;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = tint ?? Color.white;
            image.raycastTarget = false;
            return image;
        }

        internal static TextMeshProUGUI Label(Transform parent, string name, string text, float size, bool title, Color colour, Vector2 position, Vector2 boxSize, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rect = Rect(parent, name, position, boxSize);
            var tmp = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            tmp.font = title ? MenuAssetsBuilder.Title : MenuAssetsBuilder.Body;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = colour;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        internal static PopButton Button(Transform parent, string name, string label, string sprite, Vector2 position, Vector2 size, float labelSize, Color labelColour, bool title = true, bool soonTag = false)
        {
            var rect = Rect(parent, name, position, size);
            var image = GetOrAdd<Image>(rect.gameObject);
            image.sprite = MenuAssetsBuilder.SpriteAt(sprite);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var button = GetOrAdd<Button>(rect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = Label(rect, "Label", label, labelSize, title, labelColour, new Vector2(0f, 5f), size);
            Fill(text.rectTransform, 8f);
            var pop = GetOrAdd<PopButton>(rect.gameObject);
            Set(pop, "_theme", Theme);
            if (soonTag)
            {
                var tag = Picture(rect, "SoonTag", "ui_tag", new Vector2(size.x * 0.5f - 40f, size.y * 0.5f), new Vector2(150f, 52f));
                var tagText = Label(tag.transform, "Label", "SOON", 28f, true, Theme.Ink, Vector2.zero, new Vector2(150f, 52f));
                Fill(tagText.rectTransform, 0f);
                tag.gameObject.SetActive(false);
                Set(pop, "_lockedTag", tag.gameObject);
            }

            return pop;
        }

        internal static PanelPop Panel(Transform parent, string name)
        {
            var rect = Stretch(parent, name);
            GetOrAdd<CanvasGroup>(rect.gameObject);
            var pop = GetOrAdd<PanelPop>(rect.gameObject);
            Set(pop, "_theme", Theme);
            return pop;
        }

        internal static void Fill(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        internal static void Heading(Transform parent, string eyebrow, string title, string sub)
        {
            Label(parent, "Eyebrow", eyebrow, 30f, false, Theme.Accent, new Vector2(0f, 330f), new Vector2(900f, 44f));
            Label(parent, "Title", title, 86f, true, Color.white, new Vector2(0f, 255f), new Vector2(1100f, 110f));
            if (!string.IsNullOrEmpty(sub))
            {
                Label(parent, "Sub", sub, 38f, false, new Color(1f, 1f, 1f, 0.8f), new Vector2(0f, 180f), new Vector2(1100f, 56f));
            }
        }

        internal static PopButton BackButton(Transform parent, string label = "< Back")
        {
            return Button(parent, "Back", label, "ui_button_tertiary", new Vector2(-590f, 330f), new Vector2(230f, 84f), 36f, Color.white);
        }

        internal static PopButton ModeCard(Transform parent, string name, string title, string note, string glyph, Vector2 position, Color tint, bool soon)
        {
            var card = Button(parent, name, string.Empty, "ui_card_mode", position, new Vector2(360f, 420f), 10f, Color.white, true, soon);
            card.GetComponent<Image>().color = tint;
            var icon = Picture(card.transform, "Icon", "ui_round", new Vector2(0f, 90f), new Vector2(170f, 170f));
            Label(icon.transform, "Glyph", glyph, 72f, true, Theme.Ink, Vector2.zero, new Vector2(170f, 170f));
            Label(card.transform, "Name", title, 54f, true, Color.white, new Vector2(0f, -70f), new Vector2(340f, 70f));
            Label(card.transform, "Note", note, 30f, false, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, -135f), new Vector2(320f, 70f));
            card.transform.Find("Label").gameObject.SetActive(false);
            return card;
        }

        internal static Slider SliderRow(Transform parent, string name, Vector2 position, float width)
        {
            var rect = Rect(parent, name, position, new Vector2(width, 44f));
            var slider = GetOrAdd<Slider>(rect.gameObject);
            var background = Picture(rect, "Background", "ui_track", Vector2.zero, new Vector2(width, 40f));
            Fill(background.rectTransform, 0f);
            var fillArea = Stretch(rect, "Fill Area");
            fillArea.offsetMin = new Vector2(8f, 8f);
            fillArea.offsetMax = new Vector2(-8f, -8f);
            var fill = Picture(fillArea, "Fill", "ui_round", Vector2.zero, new Vector2(20f, 24f), Theme.Success);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = new Vector2(20f, 0f);
            var handleArea = Stretch(rect, "Handle Slide Area");
            handleArea.offsetMin = new Vector2(22f, 0f);
            handleArea.offsetMax = new Vector2(-22f, 0f);
            var handle = Picture(handleArea, "Handle", "ui_round", Vector2.zero, new Vector2(54f, 54f), Theme.Accent);
            handle.raycastTarget = true;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }
    }
}
