using Intel.RealSense;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;


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

    public void SetVisualEffectTransform(Transform transform, int cameraID)
    {
        var vfx = GetVisualEffect(cameraID);
        if (vfx != null)
        {
            vfx.transform.position = transform.position;
            vfx.transform.rotation = transform.rotation;
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