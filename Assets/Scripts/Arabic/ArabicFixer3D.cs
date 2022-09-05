using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class ArabicFixer3D : MonoBehaviour
{
    public string fixedText;
    //public InputField RefrenceInput;
    public bool ShowTashkeel;
    public bool UseHinduNumbers;

    TextMeshPro tmpTextComponent;

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
        tmpTextComponent = GetComponent<TextMeshPro>();
    }

    public void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        fixedText = tmpTextComponent.text;
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
            OldFontSize == tmpTextComponent.fontSize &&
            OldDeltaSize == rectTransform.sizeDelta &&
            OldEnabled == tmpTextComponent.gameObject.activeInHierarchy &&
            (OldScreenRect.x == Screen.width && OldScreenRect.y == Screen.height &&
            !CheckRectTransformParentsIfChanged()))
            return;

        //tmpTextComponent.text = fixedText;
        //fixedText = tmpTextComponent.text;
        FixTextForUI();
        OldText = fixedText;
        OldFontSize = (int)tmpTextComponent.fontSize;
        OldDeltaSize = rectTransform.sizeDelta;
        OldEnabled = tmpTextComponent.gameObject.activeInHierarchy;
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

            tmpTextComponent.text = "";
            for (int lineIndex = 0; lineIndex < rtlParagraph.Length; lineIndex++)
            {
                string[] words = rtlParagraph[lineIndex].Split(' ');
                System.Array.Reverse(words);
                tmpTextComponent.text = string.Join(" ", words);
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < tmpTextComponent.textInfo.lineCount; i++) 
                {
                    int startIndex = tmpTextComponent.textInfo.lineInfo[i].firstCharacterIndex;
                    int endIndex = (i == tmpTextComponent.textInfo.lineCount - 1) ? tmpTextComponent.text.Length
                        : tmpTextComponent.textInfo.lineInfo[i + 1].firstCharacterIndex;
                    int length = endIndex - startIndex;
                    string[] lineWords = tmpTextComponent.text.Substring(startIndex, length).Split(' ');
                    System.Array.Reverse(lineWords);
                    finalText = finalText + string.Join(" ", lineWords).Trim() + "\n";
                }
            }
            tmpTextComponent.text = finalText.TrimEnd('\n');
        }
    }
}


/* // For all kinds

   enum whichType { None, Text, TMPText, InputField, TMPInputField }

    public string fixedText;
    //public InputField RefrenceInput;
    public bool ShowTashkeel;
    public bool UseHinduNumbers;

    //all types
    whichType sourceComponent;
    Text textComponent;
    TextMeshProUGUI tmpTextComponent;
    InputField inputFieldComponent;
    TMP_InputField tmpInputFieldComponent;


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
        sourceComponent = whichType.None;
        isInitilized = false;
    }

    public void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(PrepareComponenet());
    }

    IEnumerator PrepareComponenet()
    {
        if (GameManager.instance.player.language != 0) //if not arabic remove script
            Destroy(GetComponent<ArabicFixer>());

        yield return null;
        if (GetComponent<Text>())
        {
            textComponent = GetComponent<Text>();
            fixedText = textComponent.text;
            sourceComponent = whichType.Text;
        }
        else if (GetComponent<TextMeshProUGUI>())
        {
            tmpTextComponent = GetComponent<TextMeshProUGUI>();
            fixedText = tmpTextComponent.text;
            sourceComponent = whichType.TMPText;            
        }
        else if (GetComponent<InputField>())
        {
            inputFieldComponent = GetComponentInChildren<InputField>(); //get the 2nd Text not 1st because 1st is placeholder
            fixedText = inputFieldComponent.text;
            sourceComponent = whichType.InputField;
        }
        else
        {
            sourceComponent = whichType.None;
            //Text = tmpTextComponent.text;
        }
        //txt = gameObject.GetComponent<UnityEngine.UI.Text>();

        switch (sourceComponent)
        {
            case whichType.None:
                break;
            case whichType.Text:
                break;
            case whichType.TMPText:
                tmpTextComponent.text = "";
                break;
            case whichType.InputField:
                break;
            case whichType.TMPInputField:
                break;
            default:
                break;
        }

        yield return LocalizationSettings.InitializationOperation;

        switch (sourceComponent)
        {
            case whichType.None:
                break;
            case whichType.Text:
                break;
            case whichType.TMPText:
                while (tmpTextComponent.text == "")
                {
                    yield return null;

                }
                fixedText = tmpTextComponent.text;
                isInitilized = true;
                break;
            case whichType.InputField:
                break;
            case whichType.TMPInputField:
                break;
            default:
                break;
        }

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
        //if (!txt)
        //    return;
        if (sourceComponent == whichType.None || !isInitilized)
            return;

        switch (sourceComponent)
        {
            case whichType.None:
                return;
            case whichType.Text:
                // if No Need to Refresh
                if (OldText == fixedText &&
                    OldFontSize == textComponent.fontSize &&
                    OldDeltaSize == rectTransform.sizeDelta &&
                    OldEnabled == textComponent.enabled &&
                    (OldScreenRect.x == Screen.width && OldScreenRect.y == Screen.height &&
                    !CheckRectTransformParentsIfChanged()))
                    return;

                //Text = txt.text;
                //fixedText = textComponent.text;
                FixTextForUI();
                OldText = fixedText;
                OldFontSize = textComponent.fontSize;
                OldDeltaSize = rectTransform.sizeDelta;
                OldEnabled = textComponent.enabled;
                OldScreenRect.x = Screen.width;
                OldScreenRect.y = Screen.height;
                break;
            case whichType.TMPText:
                // if No Need to Refresh
                if (OldText == fixedText && 
                    OldFontSize == tmpTextComponent.fontSize &&
                    OldDeltaSize == rectTransform.sizeDelta &&
                    OldEnabled == tmpTextComponent.enabled &&
                    (OldScreenRect.x == Screen.width && OldScreenRect.y == Screen.height &&
                    !CheckRectTransformParentsIfChanged()))
                    return;

                //tmpTextComponent.text = fixedText;
                //fixedText = tmpTextComponent.text;
                FixTextForUI();
                OldText = fixedText;
                OldFontSize = (int)tmpTextComponent.fontSize;
                OldDeltaSize = rectTransform.sizeDelta;
                OldEnabled = tmpTextComponent.enabled;
                OldScreenRect.x = Screen.width;
                OldScreenRect.y = Screen.height;
                break;
            case whichType.InputField:
                // if No Need to Refresh
                if (OldText == fixedText && 
                    OldFontSize == inputFieldComponent.textComponent.fontSize &&
                    OldDeltaSize == rectTransform.sizeDelta &&
                    OldEnabled == inputFieldComponent.enabled &&
                    (OldScreenRect.x == Screen.width && OldScreenRect.y == Screen.height &&
                    !CheckRectTransformParentsIfChanged()))
                {
                    return;
                }


                //Text = txt.text;
                FixTextForUI();
                OldText = fixedText;
                OldFontSize = inputFieldComponent.textComponent.fontSize;
                OldDeltaSize = rectTransform.sizeDelta;
                OldEnabled = inputFieldComponent.enabled;
                OldScreenRect.x = Screen.width;
                OldScreenRect.y = Screen.height;
                break;
            default:
                break;
        }

        


    }

    public void FixTextForUI()
    {
        if (!string.IsNullOrEmpty(fixedText))
        {
            string rtlText = ArabicSupport.Fix(fixedText, ShowTashkeel, UseHinduNumbers);
            rtlText = rtlText.Replace("\r", ""); // the Arabix fixer Return \r\n for everyy \n .. need to be removed

            string finalText = "";
            string[] rtlParagraph = rtlText.Split('\n');


            //txt.text = "";

            switch (sourceComponent)
            {
                case whichType.None:
                    return ;
                case whichType.Text:
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
                    break;
                case whichType.TMPText:
                    tmpTextComponent.text = "";

                    for (int lineIndex = 0; lineIndex < rtlParagraph.Length; lineIndex++)
                    {
                        string[] words = rtlParagraph[lineIndex].Split(' ');
                        System.Array.Reverse(words);
                        tmpTextComponent.text = string.Join(" ", words);
                        Canvas.ForceUpdateCanvases();
                        for (int i = 0; i < tmpTextComponent.textInfo.lineCount; i++)
                        {
                            int startIndex = tmpTextComponent.textInfo.lineInfo[i].firstCharacterIndex;
                            int endIndex = (i == tmpTextComponent.textInfo.lineCount - 1) ? tmpTextComponent.text.Length
                                : tmpTextComponent.textInfo.lineInfo[i+1].firstCharacterIndex;
                            int length = endIndex - startIndex;
                            string[] lineWords = tmpTextComponent.text.Substring(startIndex, length).Split(' ');
                            System.Array.Reverse(lineWords);
                            finalText = finalText + string.Join(" ", lineWords).Trim() + "\n";
                        }
                    }

                    break;
                case whichType.InputField:
                    inputFieldComponent.textComponent.text = "";

                    for (int lineIndex = 0; lineIndex < rtlParagraph.Length; lineIndex++)
                    {
                        string[] words = rtlParagraph[lineIndex].Split(' ');
                        System.Array.Reverse(words);
                        inputFieldComponent.textComponent.text = string.Join(" ", words);
                        Canvas.ForceUpdateCanvases();
                        for (int i = 0; i < inputFieldComponent.textComponent.cachedTextGenerator.lines.Count; i++)
                        {
                            int startIndex = inputFieldComponent.textComponent.cachedTextGenerator.lines[i].startCharIdx;
                            int endIndex = (i == inputFieldComponent.textComponent.cachedTextGenerator.lines.Count - 1) ? inputFieldComponent.textComponent.text.Length
                                : inputFieldComponent.textComponent.cachedTextGenerator.lines[i + 1].startCharIdx;
                            int length = endIndex - startIndex;

                            string[] lineWords = inputFieldComponent.textComponent.text.Substring(startIndex, length).Split(' ');
                            System.Array.Reverse(lineWords);

                            finalText = finalText + string.Join(" ", lineWords).Trim() + "\n";
                        }
                    }
                    break;
            }
            switch (sourceComponent)
            {
                case whichType.None:
                    break;
                case whichType.Text:
                    textComponent.text = finalText.TrimEnd('\n');
                    break;
                case whichType.TMPText:
                    tmpTextComponent.text = finalText.TrimEnd('\n');
                    break;
                case whichType.InputField:
                    Debug.Log("String is:" + textComponent.text);
                    inputFieldComponent.textComponent.text = finalText.TrimEnd('\n');
                    break;
            }
        }
        //else
        //    return null;
    }

*/
