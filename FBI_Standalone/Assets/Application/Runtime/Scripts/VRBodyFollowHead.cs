using UnityEngine;

public class VRBodyFollowHead : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform; 

    [Header("Settings")]
    public Vector3 bodyOffset = new Vector3(0, -0.5f, 0); 

    private Quaternion fixedRotation;

    void Start()
    {
        fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (headTransform != null)
        {
            transform.position = headTransform.position + bodyOffset;

            transform.rotation = fixedRotation;
        }
    }
}