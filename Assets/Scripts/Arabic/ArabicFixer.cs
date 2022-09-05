using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ArabicFixer : MonoBehaviour
{
    public string fixedText;
    public bool ShowTashkeel;
    public bool UseHinduNumbers;

    Text textComponent;

    private string OldText; // For Refresh on TextChange
    private int OldFontSize; // For Refresh on Font Size Change
    private RectTransform rectTransform;  // For Refresh on resize
    private Vector2 OldDeltaSize; // For Refresh on resize
    private bool OldEnabled = false; // For Refresh on enabled change // when text ui is not active then arabic text will not trigered when the control get active
    private List<RectTransform> OldRectTransformParents = new List<RectTransform>(); // For Refresh on parent resizing
    private Vector2 OldScreenRect = new Vector2(Screen.width, Screen.height); // For Refresh on screen resizing

    bool isInitilized;
    public void Awake()
    {
        GetRectTransformParents(OldRectTransformParents);
        isInitilized = false;
        textComponent = GetComponent<Text>();
    }

    public void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        fixedText = textComponent.text;
        isInitilized = true;
    }


    private void GetRectTransformParents(List<RectTransform> rectTransforms)
    {
        rectTransforms.Clear();
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
        {
            GameObject goP = parent.gameObject;
            RectTransform rect = goP.GetComponent<RectTransform>();
            if (rect) rectTransforms.Add(rect);
        }
    }

    private bool CheckRectTransformParentsIfChanged()
    {
        bool hasChanged = false;
        for (int i = 0; i < OldRectTransformParents.Count; i++)
        {
            hasChanged |= OldRectTransformParents[i].hasChanged;
            OldRectTransformParents[i].hasChanged = false;
        }
        return hasChanged;
    }

    public void Update()
    {
        if (!isInitilized)
            return;


        // if No Need to Refresh
        if (OldText == fixedText &&
            OldFontSize == textComponent.fontSize &&
            OldDeltaSize == rectTransform.sizeDelta &&
            OldEnabled == textComponent.enabled &&
            (OldScreenRect.x == Screen.width && OldScreenRect.y == Screen.height &&
            !CheckRectTransformParentsIfChanged()))
            return;

        FixTextForUI();
        OldText = fixedText;
        OldFontSize = textComponent.fontSize;
        OldDeltaSize = rectTransform.sizeDelta;
        OldEnabled = textComponent.enabled;
        OldScreenRect.x = Screen.width;
        OldScreenRect.y = Screen.height;
    }

    public void FixTextForUI()
    {
        if (!string.IsNullOrEmpty(fixedText))
        {
            string rtlText = ArabicSupport.Fix(fixedText, ShowTashkeel, UseHinduNumbers);
            rtlText = rtlText.Replace("\r", ""); // the Arabix fixer Return \r\n for everyy \n .. need to be removed

            string finalText = "";
            string[] rtlParagraph = rtlText.Split('\n');

            textComponent.text = "";

            for (int lineIndex = 0; lineIndex < rtlParagraph.Length; lineIndex++)
            {
                string[] words = rtlParagraph[lineIndex].Split(' ');
                System.Array.Reverse(words);
                textComponent.text = string.Join(" ", words);
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < textComponent.cachedTextGenerator.lines.Count; i++)
                {
                    int startIndex = textComponent.cachedTextGenerator.lines[i].startCharIdx;
                    int endIndex = (i == textComponent.cachedTextGenerator.lines.Count - 1) ? textComponent.text.Length
                        : textComponent.cachedTextGenerator.lines[i + 1].startCharIdx;
                    int length = endIndex - startIndex;

                    string[] lineWords = textComponent.text.Substring(startIndex, length).Split(' ');
                    System.Array.Reverse(lineWords);

                    finalText = finalText + string.Join(" ", lineWords).Trim() + "\n";
                }
            }
            textComponent.text = finalText.TrimEnd('\n');
        }
    }
}