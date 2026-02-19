using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderToText : MonoBehaviour
{
    private Slider slider;

    [SerializeField] private TextMeshProUGUI tmpText;

    void Start()
    {
        slider = GetComponent<Slider>();
        tmpText = GetComponent<TextMeshProUGUI>();

        slider.onValueChanged.AddListener(UpdateText);
        UpdateText(slider.value);
    }

    void UpdateText(float value)
    {
        tmpText.text = value.ToString("F2");
    }

    void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(UpdateText);
    }
}