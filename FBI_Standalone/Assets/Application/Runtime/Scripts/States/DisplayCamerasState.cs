using com.rfilkov.kinect;
using PrimeTween;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static PrimeTween.Sequence;

public class DisplayCamerasState : IState
{
    private DisplayCamerasStep step;

    private PrimeTween.Sequence sequence;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayCamerasStep;
    }

    public IEnumerator Execute()
    {

        // to DO, boucle for multiple cameras in the same step
        // for interpolation Create a tween sequence for executing all the transition a the in the timelime 

        sequence = PrimeTween.Sequence.Create(cycles: -1, SequenceCycleMode.Restart);

        StringBuilder displayText = new StringBuilder();

        StringBuilder cameradelays = new StringBuilder();
        cameradelays.Append("[");
        StringBuilder cameraIDs = new StringBuilder();
        cameraIDs.Append("[");

        bool asInterpolation = false;
        bool asDissolution = false;

        float afterInterpolationMaxWait = 0.0f;
        float afterDissolutionMaxWait = 0.0f;

        for (int i = 0; i < step.camerasData.Count; i++)
        {
            var cameraData = step.camerasData[i];

            var pointCloudID = int.Parse(cameraData.id);

            cameradelays.Append(cameraData.id);
            cameradelays.Append(",");

            cameraIDs.Append(pointCloudID);
            cameraIDs.Append(",");

            ConfigFile configFile = null;
            ConfigFile startConfigFile = null;

            if(cameraData.delay > 0.0f)
            {
                var sensorData = KinectManager.Instance.GetSensorData(pointCloudID - 1);

                if (sensorData != null && sensorData.sensorInterface != null)
                {
                    var sensorInterface = (DepthSensorBase)sensorData.sensorInterface;

                    var replayBuffer = sensorInterface.GetComponent<PointCloudReplayBuffer>();

                    replayBuffer.SetReplayDelay(cameraData.delay);
                    replayBuffer.enableReplay = true;
                }
            }

            if (!string.IsNullOrEmpty(cameraData.configName))
            {
                if (ConfigFileManager.Instance.IsValideConfigName(cameraData.configName) && ConfigFileManager.Instance.CurrentConfig.configName != cameraData.configName)
                {
                    configFile = ConfigFileManager.Instance.Load(cameraData.configName);
                }
                else
                {
                    EventFileManager.Error($"[DisplayCamerasState] Config file configName:{cameraData.configName} not found");
                }
            }

            PointCloud pointCloud = null;

            if (configFile != null)
            {
                pointCloud = PointCloudManager.Instance.SpawnPointCloud(pointCloudID, cameraData.delay, configFile);
            }

            displayText.AppendLine($"Display Camera {cameraData.id} for {step.GetDuration()} seconds with {cameraData.delay} of delay");

            //setup interporlation

            if(cameraData.interpolation != null)
            {
                asInterpolation = true;

                if (!string.IsNullOrEmpty(cameraData.interpolation.startConfigName))
                {
                    if (ConfigFileManager.Instance.IsValideConfigName(cameraData.interpolation.startConfigName) && ConfigFileManager.Instance.CurrentConfig.configName != cameraData.interpolation.startConfigName)
                    {
                        startConfigFile = ConfigFileManager.Instance.Load(cameraData.interpolation.startConfigName);
                    }
                    else
                    {
                        EventFileManager.Error($"[DisplayCamerasState] Config file startConfigName:{cameraData.interpolation.startConfigName} not found");
                    }
                }

                sequence.Insert(atTime: cameraData.interpolation.delay,
                    Tween.Position(target: pointCloud.transform, startValue: startConfigFile.pointClouds[pointCloudID - 1].position.ToVector3(), endValue: configFile.pointClouds[pointCloudID - 1].position.ToVector3(), duration: cameraData.interpolation.duration, ease: cameraData.interpolation.ease)
                    .Group(Tween.Rotation(target: pointCloud.transform, startValue: startConfigFile.pointClouds[pointCloudID - 1].rotation.ToVector3(), endValue: configFile.pointClouds[pointCloudID - 1].rotation.ToVector3(), duration: cameraData.interpolation.duration, ease: cameraData.interpolation.ease)));

                afterInterpolationMaxWait = Mathf.Max(step.displayTime - cameraData.interpolation.duration - cameraData.interpolation.delay, afterInterpolationMaxWait);
            }

            if(cameraData.dissolution != null)
            {
                asDissolution = true;

                pointCloud.SetDissolutionDuration(cameraData.dissolution.duration);

                sequence.Insert(atTime: 0, Tween.Delay(duration: cameraData.dissolution.delay).OnComplete(() => pointCloud.StartDissolution()));

                afterDissolutionMaxWait = Mathf.Max(step.displayTime - cameraData.dissolution.duration - cameraData.dissolution.delay, afterDissolutionMaxWait);

            }

        }

        cameradelays.Append("]");
        OutputFileManager.Instance.OutputFileData.CameraDelay = cameradelays.ToString();

        cameraIDs.Append("]");
        OutputFileManager.Instance.OutputFileData.CameraID = cameraIDs.ToString();

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "DisplayCameras";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        OutputFileManager.Instance.OutputFileData.CameraDisplayDuration = step.displayTime;

        PointCloudManager.Instance.DisplaySpawnedPointClouds();

        EventFileManager.Log($"[DisplayCameraState] Display Cameras" + displayText.ToString());

        if (asInterpolation || asDissolution)
        {
            yield return sequence.ToYieldInstruction();

            yield return new WaitForSeconds(Mathf.Max(afterDissolutionMaxWait, afterInterpolationMaxWait));
        }
        else
        {
            yield return new WaitForSeconds(step.displayTime);
        }


        PointCloudManager.Instance.HideSpawnedPointClouds();

        for (int i = 0; i < step.camerasData.Count; i++)
        {
            var cameraData = step.camerasData[i];

            var pointCloudID = int.Parse(cameraData.id);

            if (cameraData.delay > 0.0f)
            {
                var sensorData = KinectManager.Instance.GetSensorData(pointCloudID - 1);

                if (sensorData != null && sensorData.sensorInterface != null)
                {
                    var sensorInterface = (DepthSensorBase)sensorData.sensorInterface;

                    var replayBuffer = sensorInterface.GetComponent<PointCloudReplayBuffer>();

                    replayBuffer.enableReplay = false;
                }
            }

        }

        OutputFileManager.Instance.SaveOutputEntry();

    }

    public void Exit()
    {

        if(sequence.isAlive)
        {
            sequence.Stop();
        }

        PointCloudManager.Instance.HideSpawnedPointClouds();

        PointCloudManager.Instance.DespawnPointClouds();

        for (int i = 0; i < step.camerasData.Count; i++)
        {
            var cameraData = step.camerasData[i];

            var pointCloudID = int.Parse(cameraData.id);

            if (cameraData.delay > 0.0f)
            {
                var sensorData = KinectManager.Instance.GetSensorData(pointCloudID - 1);

                if (sensorData != null && sensorData.sensorInterface != null)
                {
                    var sensorInterface = (DepthSensorBase)sensorData.sensorInterface;

                    var replayBuffer = sensorInterface.GetComponent<PointCloudReplayBuffer>();

                    replayBuffer.enableReplay = false;
                }
            }

        }

  

    }
}
