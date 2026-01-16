using UnityEngine;
using System.Collections;
using com.rfilkov.kinect;

namespace com.rfilkov.components
{
    /// <summary>
    /// FollowUserRotation makes the tranform it is attached to follow the given user orientation.
    /// </summary>
    public class FollowUserRotation : MonoBehaviour
    {
        [Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
        public int playerIndex = 0;

        void Update()
        {
            KinectManager kinectManager = KinectManager.Instance;

            if (kinectManager && kinectManager.IsInitialized())
            {
                if (kinectManager.IsUserDetected(playerIndex))
                {
                    ulong userId = kinectManager.GetUserIdByIndex(playerIndex);

                    if (kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.ShoulderLeft) &&
                       kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.ShoulderRight))
                    {
                        Vector3 posLeftShoulder = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.ShoulderLeft);
                        Vector3 posRightShoulder = kinectManager.GetJointPosition(userId, (int)KinectInterop.JointType.ShoulderRight);

                        posLeftShoulder.z = -posLeftShoulder.z;
                        posRightShoulder.z = -posRightShoulder.z;

                        Vector3 dirLeftRight = posRightShoulder - posLeftShoulder;
                        dirLeftRight -= Vector3.Project(dirLeftRight, Vector3.up);

                        Quaternion rotationShoulders = Quaternion.FromToRotation(Vector3.right, dirLeftRight);

                        transform.rotation = rotationShoulders;
                    }
                }
            }
        }

    }
}
