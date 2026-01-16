using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    [SerializeField] private Vector3 upAxis = Vector3.up;
    [SerializeField] private Vector3 forwardAxis = Vector3.forward;
    [SerializeField] private bool alignUpOnly = false;
    [SerializeField] private float smoothSpeed = 0f;

    private void LateUpdate()
    {
        if (alignUpOnly)
        {
            AlignUpOnly();
        }
        else
        {
            AlignUpAndForward();
        }
    }

    private void AlignUpOnly()
    {

        Vector3 worldUp = Vector3.up;

        Quaternion targetRotation = Quaternion.FromToRotation(transform.TransformDirection(upAxis), worldUp);

        if (smoothSpeed > 0)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation * transform.rotation,
                Time.deltaTime * smoothSpeed
            );
        }
        else
        {
            transform.rotation = targetRotation * transform.rotation;
        }
    }

    private void AlignUpAndForward()
    {
        Vector3 worldUp = Vector3.up;
        Vector3 worldForward = Vector3.ProjectOnPlane(transform.TransformDirection(forwardAxis), worldUp).normalized;

        if (worldForward.sqrMagnitude < 0.001f)
        {
            worldForward = Vector3.ProjectOnPlane(transform.TransformDirection(Vector3.right), worldUp).normalized;

            if (worldForward.sqrMagnitude < 0.001f)
                worldForward = Vector3.forward;
        }

        Quaternion targetRotation = Quaternion.LookRotation(worldForward, worldUp);

        if (forwardAxis != Vector3.forward || upAxis != Vector3.up)
        {
            Quaternion adjustment = Quaternion.Inverse(
                Quaternion.LookRotation(forwardAxis, upAxis)
            );
            targetRotation *= adjustment;
        }

        if (smoothSpeed > 0)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * smoothSpeed
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }
}