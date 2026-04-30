using UnityEngine;

public class MaintainWorldRotation : MonoBehaviour
{
    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.rotation;
    }

    void LateUpdate()
    {
        transform.rotation = targetRotation;
    }
}