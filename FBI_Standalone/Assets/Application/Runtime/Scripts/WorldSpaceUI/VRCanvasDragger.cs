using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class VRCanvasDragger : MonoBehaviour
{
    [SerializeField] private float positionSmoothing = 20f;
    [SerializeField] private float rotationSmoothing = 20f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private IXRSelectInteractor  currentInteractor;

    private Vector3 grabOffset;
    private float grabYawOffset;
    private bool  isDragging;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;
        isDragging = true;

        Transform hand = currentInteractor.GetAttachTransform(interactable);

        grabOffset = transform.position - hand.position;

        float handYaw = hand.eulerAngles.y;
        float canvasYaw = transform.eulerAngles.y;
        grabYawOffset = canvasYaw - handYaw;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isDragging = false;
        currentInteractor = null;
    }

    private void Update()
    {
        if (!isDragging || currentInteractor == null)
        {
            return;
        }

        Transform hand = currentInteractor.GetAttachTransform(interactable);

        Vector3 targetPosition = hand.position + grabOffset;

        float targetYaw = hand.eulerAngles.y + grabYawOffset;
        Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionSmoothing);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothing);

    }
}
