using com.rfilkov.kinect;
using Intel.RealSense;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;


public class RenderTextureContainer
{
    public RenderTexture colorTexture;
    public RenderTexture vertexTexture;
    public RenderTextureContainer(RenderTexture colorTexture, RenderTexture vertexTexture)
    {
        this.colorTexture = colorTexture;
        this.vertexTexture = vertexTexture;
    }
}

public class  ConditionRenderTextureContainer
{
    public RenderTextureContainer realtimeTextures;
    public RenderTextureContainer delayTextures;
}


public class PointCloudManager : MonoBehaviour
{

    [SerializeField] private GameObject pointCloudPrefab;

    private Dictionary<int, ConditionRenderTextureContainer> renderTextureDictionary = new Dictionary<int, ConditionRenderTextureContainer>();

    private List<PointCloud> spawnedPointClouds = new List<PointCloud>();

    private static PointCloudManager instance;

    public static PointCloudManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); } else { instance = this; }
    }

    private void Start()
    {
        if(KinectManager.Instance.IsInitialized())
        {
            var cameraCount = KinectManager.Instance.GetSensorCount();

            for (int i = 0; i < cameraCount; i++)
            {

                var conditionRenderTexture = new ConditionRenderTextureContainer();

                var sensorData = KinectManager.Instance.GetSensorData(i);

                if (sensorData != null && sensorData.sensorInterface != null)
                {
                    var sensorInterface = (DepthSensorBase)sensorData.sensorInterface;

                    if (sensorInterface.pointCloudVertexTexture != null && sensorInterface.pointCloudColorTexture != null)
                    {
                        conditionRenderTexture.realtimeTextures = new RenderTextureContainer(sensorInterface.pointCloudColorTexture, sensorInterface.pointCloudVertexTexture);
                    }

                    var replayBuffer = sensorInterface.GetComponent<PointCloudReplayBuffer>();

                    if(replayBuffer.replayColorTexture != null && replayBuffer.replayVertexTexture != null)
                    {
                        conditionRenderTexture.delayTextures = new RenderTextureContainer(replayBuffer.replayColorTexture, replayBuffer.replayVertexTexture);
                    }

                    renderTextureDictionary.Add(i+1, conditionRenderTexture);
                }
            }
        }

      
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    public PointCloud SpawnPointCloud(int cameraID, float delay, ConfigFile configFile)
    {
        if (!pointCloudPrefab)
        {
            Debug.LogError("Point cloud prefab is not assigned.");
            return null;
        }

        bool isDelay = delay > 0.0f ? true : false;

        var pointcloudGO = Instantiate(pointCloudPrefab, transform);

        pointcloudGO.name = $"PointCloud_{cameraID}_{(delay > 0 ? "delay" : "realtime")}";

        pointcloudGO.transform.position = Vector3.zero;
        pointcloudGO.transform.rotation = Quaternion.identity;

        var pointcloud = pointcloudGO.GetComponent<PointCloud>();

        pointcloud.Init(cameraID);

        if (renderTextureDictionary.Count > 0)
        {

            if (isDelay)
            {
                pointcloud.SetRenderTextures(renderTextureDictionary[cameraID].delayTextures.colorTexture, renderTextureDictionary[cameraID].delayTextures.vertexTexture);
            }
            else
            {
                pointcloud.SetRenderTextures(renderTextureDictionary[cameraID].realtimeTextures.colorTexture, renderTextureDictionary[cameraID].realtimeTextures.vertexTexture);

            }
        }else
        {
            Debug.LogError("No render textures found for any camera.");
        }

        var sensorData = KinectManager.Instance.GetSensorData(cameraID - 1);

        if (sensorData == null)
        {
            Debug.LogError($"No sensor data found for camera ID {cameraID}");
            return null;
        }

        var sensorInterface = (Kinect4AzureInterface)sensorData.sensorInterface;

        pointcloud.SetKinectInterface(sensorInterface);


        var currentConfig = configFile;

        if (currentConfig != null)
        {
            for (int i = 0; i < currentConfig.pointClouds.Count; i++)
            {
                var id = currentConfig.pointClouds[i].ID;

                if(id == cameraID)
                {
                    var position = currentConfig.pointClouds[i].position.ToVector3();
                    var rotation = currentConfig.pointClouds[i].rotation.ToVector3();
                    var scale = currentConfig.pointClouds[i].scale.ToVector3();
                    var depthMin = currentConfig.pointClouds[i].depthMin;
                    var depthMax = currentConfig.pointClouds[i].depthMax;
                    var clampXMin = currentConfig.pointClouds[i].clampXMin;
                    var clampXMax = currentConfig.pointClouds[i].clampXMax;
                    var clampYMin = currentConfig.pointClouds[i].clampYMin;
                    var clampYMax = currentConfig.pointClouds[i].clampYMax;

                    pointcloud.SetTransform(position, rotation, scale);
                    pointcloud.SetCameraDeptValues(depthMin, depthMax);
                    pointcloud.SetClampValues(clampXMin, clampXMax, clampYMin, clampYMax);

                }
            }
        }

        spawnedPointClouds.Add(pointcloud);

        return pointcloud;

    }

    public void DespawnPointClouds()
    {
        for (int i = spawnedPointClouds.Count - 1; i >= 0; i--)
        {
            Destroy(spawnedPointClouds[i].gameObject);
        }
    }

    public void DisplaySpawnedPointClouds()
    {
        foreach (var pointCloud in spawnedPointClouds)
        {
            pointCloud.DisplayMain();
        }
    }

    public void HideSpawnedPointClouds()
    {
        foreach (var pointCloud in spawnedPointClouds)
        {
            pointCloud.HideMain();
            pointCloud.HideDissolution();
        }
    }

    public PointCloud GetPointCloud(int cameraID)
    {
        foreach(var pointCloud in spawnedPointClouds)
        {
            if(pointCloud.Id == cameraID)
            {
                return pointCloud;
            }
        }
        return null;
    }


}
