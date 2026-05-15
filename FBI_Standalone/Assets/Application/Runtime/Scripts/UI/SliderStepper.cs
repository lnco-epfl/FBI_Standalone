using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SliderStepper : MonoBehaviour
{
    [Header("References")]
    public Slider slider;
    public Button plusButton;
    public Button minusButton;
    [Tooltip("Optionnel : affiche la valeur du slider en temps réel")]
    public TMP_Text valueLabel;

    [Header("Step Settings")]
    public float step = 0.01f;

    [Header("Hold Settings")]
    [Tooltip("Délai avant que la répétition commence (en secondes)")]
    public float holdDelay = 0.5f;
    [Tooltip("Intervalle entre chaque répétition (en secondes)")]
    public float holdInterval = 0.08f;

    private float holdTimer = 0f;
    private float repeatTimer = 0f;
    private int holdDirection = 0; // +1 ou -1
    private bool isHolding = false;

    private void OnEnable()
    {
        AddHoldListeners(plusButton, +1);
        AddHoldListeners(minusButton, -1);

        slider.onValueChanged.AddListener(OnSliderValueChanged);
        UpdateLabel(slider.value);
    }

    private void OnDisable()
    {
        StopHold();
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void AddHoldListeners(Button button, int direction)
    {
        var trigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();

        AddTriggerEntry(trigger, EventTriggerType.PointerDown, (_) => StartHold(direction));
        AddTriggerEntry(trigger, EventTriggerType.PointerUp,   (_) => StopHold());
        AddTriggerEntry(trigger, EventTriggerType.PointerExit, (_) => StopHold());
    }

    private void AddTriggerEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void StartHold(int direction)
    {
        holdDirection = direction;
        isHolding = true;
        holdTimer = 0f;
        repeatTimer = 0f;

        Step(direction);
    }

    private void StopHold()
    {
        isHolding = false;
        holdDirection = 0;
    }

    private void Update()
    {
        if (!isHolding) return;

        holdTimer += Time.deltaTime;

        if (holdTimer >= holdDelay)
        {
            repeatTimer += Time.deltaTime;

            if (repeatTimer >= holdInterval)
            {
                repeatTimer = 0f;
                Step(holdDirection);
            }
        }
    }

    private void Step(int direction)
    {
        float value = slider.value + step * direction;

        // Snap to the nearest step to avoid floating-point drift
        value = Mathf.Round(value / step) * step;
        value = Mathf.Clamp(value, slider.minValue, slider.maxValue);

        slider.value = value;
    }

    private void OnSliderValueChanged(float value)
    {
        UpdateLabel(value);
    }

    private void UpdateLabel(float value)
    {
        if (valueLabel != null)
            valueLabel.text = value.ToString("F2");
    }
}
