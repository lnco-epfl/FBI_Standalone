using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DragToScroll : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    public TMP_InputField targetInput;

    [Header("Settings")]
    public float sensitivity = 0.1f;
    public float minValue = float.MinValue;
    public float maxValue = float.MaxValue;
    public int decimalPlaces = 2;
    public bool wholeNumbersOnly = false;

    [Header("Visual Feedback")]
    public bool changeCursorOnHover = true;
    public Color dragColor = new Color(0.4f, 0.8f, 1f);
    public Color defaultColor = Color.white;

    private float currentValue;
    private float dragStartX;
    private float valueAtDragStart;
    private bool isDragging;
    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();

        if (targetInput != null)
            SyncValueFromInput();
    }


    private void SyncValueFromInput()
    {
        if (float.TryParse(targetInput.text, out float parsed))
            currentValue = parsed;
    }

    private void PushValueToInput()
    {
        float clamped = Mathf.Clamp(currentValue, minValue, maxValue);
        currentValue = clamped;

        string formatted = wholeNumbersOnly
            ? Mathf.RoundToInt(clamped).ToString()
            : clamped.ToString($"F{decimalPlaces}");

        if (targetInput != null)
            targetInput.text = formatted;
    }


    public void OnPointerEnter(PointerEventData e)
    {
        if (label) label.color = dragColor;
        if (changeCursorOnHover)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!isDragging && label)
            label.color = defaultColor;
    }

    public void OnPointerDown(PointerEventData e)
    {
        isDragging = true;
        dragStartX = e.position.x;
        valueAtDragStart = currentValue;

        SyncValueFromInput();
        valueAtDragStart = currentValue;
    }

    public void OnDrag(PointerEventData e)
    {
        float delta = (e.position.x - dragStartX) * sensitivity;
        currentValue = valueAtDragStart + delta;
        PushValueToInput();
    }

    public void OnPointerUp(PointerEventData e)
    {
        isDragging = false;
        if (label) label.color = defaultColor;
    }
}