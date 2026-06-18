using com.rfilkov.kinect;
using System.IO;
using UnityEngine;

public class RecordingSensorSwitcher : MonoBehaviour
{

    public string fileName;

    private void Awake()
    {


       var azureInterface = GetComponent<Kinect4AzureInterface>();

#if CONNECTED_SENSOR
        azureInterface.deviceStreamingMode = KinectInterop.DeviceStreamingMode.ConnectedSensor;
#elif PLAY_RECORDING
        azureInterface.deviceStreamingMode = KinectInterop.DeviceStreamingMode.PlayRecording;
#endif


        string path = Path.Combine(Application.streamingAssetsPath, "Recordings", fileName);
        path = path.Replace("\\", "/");
        Debug.Log( path );
        azureInterface.recordingFile = path;

    }
}
