using com.rfilkov.kinect;
using UnityEngine;
using static com.rfilkov.kinect.DepthSensorBase;

public class PointCloudRealtime : MonoBehaviour
{
    [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
    public int sensorIndex = 0;

    [Tooltip("Resolution of the generated point-cloud textures.")]
    public PointCloudResolution pointCloudResolution = PointCloudResolution.DepthCameraResolution;

    [Tooltip("Render texture, used for point-cloud vertex mapping. The texture resolution should match the depth or color image resolution.")]
    public RenderTexture pointCloudVertexTexture = null;

    [Tooltip("Render texture, used for point-cloud color mapping. The texture resolution should match the depth or color image resolution.")]
    public RenderTexture pointCloudColorTexture = null;

    // references
    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    private DepthSensorBase sensorInt = null;


    private void Awake()
    {
        Initialize();
    }

    public void  Initialize()
    {
        kinectManager = KinectManager.Instance;
        sensorData =  kinectManager.GetSensorData(sensorIndex);

        if (sensorData != null && sensorData.sensorInterface != null)
        {
            sensorInt = (DepthSensorBase)sensorData.sensorInterface;

            sensorInt.pointCloudResolution = pointCloudResolution;
            sensorInt.pointCloudVertexTexture = pointCloudVertexTexture;
            sensorInt.pointCloudColorTexture = pointCloudColorTexture;

            //Debug.Log("PointCloudResolution: " + pointCloudResolution + ", PointCloudVertexTexture: " + pointCloudVertexTexture + ", PointCloudColorTexture: " + pointCloudColorTexture);
        }
    }
}

