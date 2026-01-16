using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))]
public class TextAutoSizer : MonoBehaviour
{
    private TMP_Text tmpText;
    private Image background;

    private float paddingHorizontal = 2.5f;
    private float paddingVertical = 2.5f;
    private float minWidth = 0f;
    private float maxWidth = 125f;
    private float minHeight = 0f;
    private float maxHeight = 15f;

    private bool resizeWidth = true;
    private bool resizeHeight = true;
    private bool updateOnEnable = true;
    private bool continuousUpdate = false;

    private RectTransform parentRectTransform;
    private string lastTextValue = "";

    private void OnEnable()
    {

        tmpText = GetComponentInChildren<TMP_Text>();

        background = GetComponentInChildren<Image>();

        parentRectTransform = GetComponent<RectTransform>();

        if (updateOnEnable)
        {
            UpdateRectSize();
        }

        SetText(string.Empty);
    }

    private void Update()
    {
        if (continuousUpdate || lastTextValue != tmpText.text)
        {
            UpdateRectSize();
            lastTextValue = tmpText.text;
        }
    }

    /// <summary>
    /// Call this method when you want to force an update of the rect size
    /// </summary>
    public void UpdateRectSize()
    {
        if (tmpText == null || parentRectTransform == null)
            return;


        Vector2 preferredValues = tmpText.GetPreferredValues();

        Vector2 newSize = parentRectTransform.sizeDelta;

        if (resizeWidth)
        {
            newSize.x = Mathf.Clamp(preferredValues.x + paddingHorizontal * 2f, minWidth, maxWidth);
        }

        if (resizeHeight)
        {
            newSize.y = Mathf.Clamp(preferredValues.y + paddingVertical * 2f, minHeight, maxHeight);
        }

        parentRectTransform.sizeDelta = newSize;

        if (preferredValues.magnitude > 0.0f)
        {
            background.enabled = true;
        }
    }

    /// <summary>
    /// Updates the text and automatically resizes the container
    /// </summary>
    public void SetText(string newText)
    {
        if (tmpText != null)
        {
            if (string.IsNullOrEmpty(newText))
            {
                background.enabled = false;
            }

            tmpText.text = newText;
            UpdateRectSize();
        }
    }
}