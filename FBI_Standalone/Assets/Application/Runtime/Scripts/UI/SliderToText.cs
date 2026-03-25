using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderToText : MonoBehaviour
{
    private Slider slider;

    [SerializeField] private TMP_Text tmpText;

    void Start()
    {
        slider = GetComponent<Slider>();

        slider.onValueChanged.AddListener(UpdateText);
        UpdateText(slider.value);
    }

    void UpdateText(float value)
    {
        tmpText.text = value.ToString("F2");
    }

    void OnDestroy()
    {
        if(slider!= null)
        {
            slider.onValueChanged.RemoveListener(UpdateText);
        } 
    }
}