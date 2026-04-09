using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputFieldStepper : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button plusButton;
    public Button minusButton;

    public float step = 0.01f;

    private void Enable()
    {
        plusButton.onClick.AddListener(Increase);
        minusButton.onClick.AddListener(Decrease);
    }

    private void Disable()
    {
        plusButton.onClick.RemoveListener(Increase);
        minusButton.onClick.RemoveListener(Decrease);
    }

    public void Increase()
    {
        float value = GetValue();
        value += step;
        SetValue(value);
    }

    public void Decrease()
    {
        float value = GetValue();
        value -= step;

        SetValue(value);
    }

    float GetValue()
    {
        float value;
        if (!float.TryParse(inputField.text, out value))
        {
            value = 0f;
        }
        return value;
    }

    void SetValue(float value)
    {
        inputField.text = value.ToString("F2"); 
    }
}