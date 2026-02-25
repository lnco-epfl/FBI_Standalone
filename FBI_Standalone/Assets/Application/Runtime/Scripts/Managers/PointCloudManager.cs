using com.rfilkov.kinect;
using Intel.RealSense;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using static com.rfilkov.kinect.KinectInterop;


public class PointCloudContainer
{
    public VisualEffect vfx;
    public PointCloudReplayBuffer replayBuffer;
    public RealtimeDelaySwitcher realtimeDelaySwitcher;
    public PointCloudContainer(VisualEffect vfx, PointCloudReplayBuffer replayBuffer, RealtimeDelaySwitcher realtimeDelaySwitcher)
    {
        this.vfx = vfx;
        this.replayBuffer = replayBuffer;
        this.realtimeDelaySwitcher = realtimeDelaySwitcher;
    }

}


public class PointCloudManager : MonoBehaviour
{
    private Dictionary<int, PointCloudContainer> pointCloudContainers = new Dictionary<int, PointCloudContainer>();

    [SerializeField] private List<Transform> pointClouds;


    private static PointCloudManager instance;
    public static PointCloudManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }


        foreach (var pointCloud in pointClouds)
        {
            var pointCloudVFX = pointCloud.GetComponentInChildren<VisualEffect>();
            var pointCloud1ReplayBuffer = pointCloudVFX.GetComponent<PointCloudReplayBuffer>();
            var realtimeDelaySwitcher = pointCloudVFX.GetComponent<RealtimeDelaySwitcher>();

            int cameraID = int.Parse(pointCloud.name.Split('_')[1]);

            pointCloudContainers[cameraID] = new PointCloudContainer(pointCloudVFX, pointCloud1ReplayBuffer, realtimeDelaySwitcher);
        }

        StartCoroutine(WaitForKinectManagerInitialization());

    }

    private IEnumerator WaitForKinectManagerInitialization()
    {
        yield return new WaitUntil(() => KinectManager.Instance.IsInitialized());

         var currentConfig = ConfigFileManager.Instance.CurrentConfig;

        if (currentConfig != null)
        {
            for (int i = 0; i < currentConfig.pointClouds.Count; i++)
            {
                var id = currentConfig.pointClouds[i].ID;
                var position = currentConfig.pointClouds[i].position.ToVector3();
                var rotation = currentConfig.pointClouds[i].rotation.ToVector3();
                var depthMin = currentConfig.pointClouds[i].depthMin;
                var depthMax = currentConfig.pointClouds[i].depthMax;

                SetVisualEffectPositionAndRotation(position, rotation, id);
                SetCameraDepthValues(depthMin, depthMax, id);
            }
        }
    }

    public PointCloudContainer GetPointCloudContainer(int cameraID)
    {

        if (pointCloudContainers.ContainsKey(cameraID))
        {
            return pointCloudContainers[cameraID];
        }
        else
        {
            Debug.LogError($"Point cloud container with camera ID {cameraID} not found.");
            return null;
        }
    }

    public Transform GetVisualEffectTransform(int cameraID)
    {
        return GetVisualEffect(cameraID).transform;
    }

    public void SetVisualEffectPositionAndRotation(Vector3 postion, Vector3 rotation, int cameraID)
    {
        var vfx = GetVisualEffect(cameraID);
        if (vfx != null)
        {
            vfx.transform.position = postion;
            vfx.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
        }
    }

    public void SetVisualEffectTransform(Transform transform, int cameraID)
    {
        var vfx = GetVisualEffect(cameraID);
        if (vfx != null)
        {
            vfx.transform.position = transform.position;
            vfx.transform.rotation = transform.rotation;
        }
    }

    private void SetCameraDepthValues(float depthMin, float depthMax, int id)
    {
        var sensorData = KinectManager.Instance != null && KinectManager.Instance.IsInitialized() ? KinectManager.Instance.GetSensorData(id - 1) : null;

        if (sensorData != null && sensorData.sensorInterface != null)
        {
            var kinectInerface = (Kinect4AzureInterface)sensorData.sensorInterface;

            kinectInerface.maxDepthDistance = depthMax;
            kinectInerface.minDepthDistance = depthMin;
        }
    }

    public VisualEffect GetVisualEffect(int cameraID)
    {
        var container = GetPointCloudContainer(cameraID);
        return container != null ? container.vfx : null;
    }
    public PointCloudReplayBuffer GetReplayBuffer(int cameraID)
    {
        var container = GetPointCloudContainer(cameraID);
        return container != null ? container.replayBuffer : null;
    }
    public RealtimeDelaySwitcher GetRealtimeDelaySwitcher(int cameraID)
    {
        var container = GetPointCloudContainer(cameraID);
        return container != null ? container.realtimeDelaySwitcher : null;
    }

    public List<int> GetAvailableCameraIds()
    {
        return pointCloudContainers.Keys.ToList();
    }
}