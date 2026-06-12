using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRSlider : Slider
{
    
    public float sensitivity = 0.3f;

    public override void OnDrag(PointerEventData eventData)
    {
        eventData.delta *= sensitivity;
        base.OnDrag(eventData);
    }
}