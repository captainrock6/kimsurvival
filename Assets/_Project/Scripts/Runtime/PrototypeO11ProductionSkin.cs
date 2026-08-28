using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KimSurvival
{
    /// <summary>
    /// Editable runtime tokens derived from the adopted O9/O10 V2 benchmark.
    /// The benchmark bitmap is never loaded by the game; this class carries only
    /// its approved ink, amber, teal and compact-layout grammar.
    /// </summary>
    public static class PrototypeO11ProductionSkin
    {
        public const string AdoptedStyleJobId = "job_20260828122852_c9ccf2aa";
        public const float MinimumFocusPixels = 44f;

        public static readonly Color Ink = new Color(0.035f, 0.075f, 0.078f, 0.98f);
        public static readonly Color InkSoft = new Color(0.055f, 0.125f, 0.125f, 0.94f);
        public static readonly Color Paper = new Color(0.91f, 0.84f, 0.66f, 1f);
        public static readonly Color Amber = new Color(0.96f, 0.61f, 0.18f, 1f);
        public static readonly Color Teal = new Color(0.11f, 0.52f, 0.54f, 1f);
        public static readonly Color Disabled = new Color(0.31f, 0.34f, 0.32f, 0.72f);

        public static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void ApplyPanel(GameObject root, float alpha = 0.96f)
        {
            if (root == null)
            {
                return;
            }

            Image surface = root.GetComponent<Image>();
            if (surface != null)
            {
                surface.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            }

            Outline outline = root.GetComponent<Outline>();
            if (outline == null)
            {
                outline = root.AddComponent<Outline>();
            }
            outline.effectColor = new Color(Amber.r, Amber.g, Amber.b, 0.88f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
        }

        public static void ApplyButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image surface = button.GetComponent<Image>();
            if (surface != null && surface.sprite == null)
            {
                surface.color = InkSoft;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 0.97f, 0.93f, 1f);
            colors.selectedColor = Teal;
            colors.pressedColor = Amber;
            colors.disabledColor = Disabled;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
            {
                outline = button.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = button.interactable
                ? new Color(Teal.r, Teal.g, Teal.b, 0.90f)
                : new Color(Paper.r, Paper.g, Paper.b, 0.34f);
            outline.effectDistance = new Vector2(1f, -1f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 22f;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
    }
}
