using com.rfilkov.kinect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.rfilkov.components
{
    /// <summary>
    /// Displays userId, positional and rotational information for the specified user on screen. 
    /// </summary>
    public class DisplayUserInfo : MonoBehaviour
    {
        [Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
        public int playerIndex = 0;

        [Tooltip("UI Text to display debug information.")]
        public UnityEngine.UI.Text debugText;


        private void Update()
        {
            KinectManager kinectManager = KinectManager.Instance;
            if (kinectManager && kinectManager.IsInitialized())
            {
                ulong playerUserID = kinectManager.GetUserIdByIndex(playerIndex);

                //Debug.Log(transform.name + "  " + playerUserID);
                if (playerUserID != 0)
                {
                    if (kinectManager.GetJointTrackingState(playerUserID, (int)KinectInterop.JointType.SpineChest) != KinectInterop.TrackingState.NotTracked)
                    {
                        //Vector3 SpineChestPos = kinectManager.GetJointPosition(playerUserID, (int)KinectInterop.JointType.SpineChest);
                        Vector3 userPos = kinectManager.GetUserPosition(playerUserID);
                        Quaternion userRot = kinectManager.GetUserOrientation(playerUserID, true);

                        if (debugText)
                            debugText.text = $"Player {playerIndex}   UserID: {playerUserID}\nPos: {userPos:F2}, Rot: {userRot.eulerAngles:F0}";
                    }
                }
            }

        }

    }
}
