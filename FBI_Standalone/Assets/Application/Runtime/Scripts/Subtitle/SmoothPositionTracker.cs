using UnityEngine;

public class SmoothPositionTracker : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [Header("Position Tracking")]
    [SerializeField] private bool trackPosition = true;
    [SerializeField] private float positionSmoothSpeed = 5f;
    [SerializeField] private bool trackX = true;
    [SerializeField] private bool trackY = true;
    [SerializeField] private bool trackZ = true;
    [SerializeField] private Vector3 positionOffset;

    [Header("Rotation Tracking")]
    [SerializeField] private bool trackRotation = true;
    [SerializeField] private float rotationSmoothSpeed = 5f;
    [SerializeField] private bool trackPitch = true;   // X rotation
    [SerializeField] private bool trackYaw = true;     // Y rotation
    [SerializeField] private bool trackRoll = true;    // Z rotation
    [SerializeField] private Vector3 rotationOffset;

    [Header("Update Settings")]
    [SerializeField] private bool useFixedUpdate = false;

    private Vector3 positionVelocity;

    private void Start()
    {
        if (target == null)
        {
            EventFileManager.Warning("No target assigned to SmoothTransformTracker on " + gameObject.name);
        }
    }

    private void Update()
    {
        if (!useFixedUpdate)
        {
            TrackTarget();
        }
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            TrackTarget();
        }
    }

    private void TrackTarget()
    {
        if (target == null)
            return;

        float deltaTime = useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;

        // Position tracking
        if (trackPosition)
        {
            Vector3 targetPosition = target.position + positionOffset;
            Vector3 currentPosition = transform.position;

            // Only track selected axes
            if (!trackX) targetPosition.x = currentPosition.x;
            if (!trackY) targetPosition.y = currentPosition.y;
            if (!trackZ) targetPosition.z = currentPosition.z;

            // Use SmoothDamp for smoother movement
            Vector3 smoothedPosition = Vector3.SmoothDamp(
                currentPosition,
                targetPosition,
                ref positionVelocity,
                1f / positionSmoothSpeed,
                Mathf.Infinity,
                deltaTime
            );

            transform.position = smoothedPosition;
        }

        // Rotation tracking
        if (trackRotation)
        {
            // Get target rotation with offset
            Quaternion targetRotation = target.rotation * Quaternion.Euler(rotationOffset);

            // If not tracking all rotation axes, we need to handle them individually
            if (!trackPitch || !trackYaw || !trackRoll)
            {
                // Convert to euler angles for easier axis manipulation
                Vector3 targetEuler = targetRotation.eulerAngles;
                Vector3 currentEuler = transform.rotation.eulerAngles;

                // Only track selected rotation axes
                if (!trackPitch) targetEuler.x = currentEuler.x;
                if (!trackYaw) targetEuler.y = currentEuler.y;
                if (!trackRoll) targetEuler.z = currentEuler.z;

                // Convert back to quaternion
                targetRotation = Quaternion.Euler(targetEuler);
            }

            // Smoothly interpolate rotation
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSmoothSpeed * deltaTime
            );
        }
    }


}