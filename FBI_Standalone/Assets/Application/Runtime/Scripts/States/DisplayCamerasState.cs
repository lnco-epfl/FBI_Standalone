using com.rfilkov.kinect;
using PrimeTween;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.VFX;

public class DisplayCamerasState : IState
{
    private DisplayCamerasStep step;

    private ConfigFile previousConfig;
    private PrimeTween.Sequence sequence;

    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayCamerasStep;
    }

    public IEnumerator Execute()
    {

        // to DO, boucle for multiple cameras in the same step
        // for interpolation Create a tween sequence for executing all the transition a the in the timelime 

        var sequence = PrimeTween.Sequence.Create(cycles: -1, CycleMode.Restart);

        StringBuilder displayText = new StringBuilder();

        StringBuilder cameradelays = new StringBuilder();
        cameradelays.Append("[");
        StringBuilder cameraIDs = new StringBuilder();
        cameraIDs.Append("[");

        bool asInterpolation = false;

        float afterInterpolationMaxWait = 0.0f;

        for (int i = 0; i < step.camerasData.Count; i++)
        {
            var cameraData = step.camerasData[i];

            var pointCloudID = int.Parse(cameraData.ID);

            cameradelays.Append(cameraData.ID);
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

            displayText.AppendLine($"Display Camera {cameraData.ID} for {step.GetDuration()} seconds with {cameraData.delay} of delay");

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

        if (asInterpolation)
        {
            yield return sequence.ToYieldInstruction();

            yield return new WaitForSeconds(afterInterpolationMaxWait);
        }
        else
        {
            yield return new WaitForSeconds(step.displayTime);
        }


        PointCloudManager.Instance.HideSpawnedPointClouds();

        for (int i = 0; i < step.camerasData.Count; i++)
        {
            var cameraData = step.camerasData[i];

            var pointCloudID = int.Parse(cameraData.ID);

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


        /*var pointCloudID = int.Parse(step.cameraID);
        var pointCloudContainer = PointCloudManager.Instance.GetPointCloudContainer(pointCloudID);

        VisualEffect vfxEffect = pointCloudContainer.vfx;
        PointCloudReplayBuffer pointCloudReplayBuffer = pointCloudContainer.replayBuffer;
        RealtimeDelaySwitcher realtimeDelaySwitcher = pointCloudContainer.realtimeDelaySwitcher;

        if(step.interpolation != null )
        {
            previousConfig = ConfigFileManager.Instance.CurrentConfig;
        }

        if(!string.IsNullOrEmpty(step.configFileName))
        {
            var configs = ConfigFileManager.Instance.GetAvailableConfigs();

            if (configs.Contains(step.configFileName) && ConfigFileManager.Instance.CurrentConfig.configName != step.configFileName)
            {
                ConfigFileManager.Instance.Load(step.configFileName);
            }
            else
            {
                EventFileManager.Error($"[DisplayCameraState] Config file {step.configFileName} not found");
            }
        }

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "DisplayCamera";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        OutputFileManager.Instance.OutputFileData.CameraDelay = step.delay;
        OutputFileManager.Instance.OutputFileData.CameraDisplayDuration = step.displayTime;
        OutputFileManager.Instance.OutputFileData.CameraID = step.cameraID;

        realtimeDelaySwitcher.displayMode = step.delay > 0.0f ? RealtimeDelaySwitcher.DisplayMode.Delay : RealtimeDelaySwitcher.DisplayMode.Realtime;
        pointCloudReplayBuffer.replayDelaySeconds = step.delay;

        //pointCloudReplayBuffer.enabled = step.delay > 0.0f ? true : false;
        pointCloudReplayBuffer.enableReplay = step.delay > 0.0f ? true : false;

        vfxEffect.enabled = true;

        EventFileManager.Log($"[DisplayCameraState] Display Camera {step.cameraID} for {step.GetDuration()} seconds with {step.delay} of delay");

        if (step.interpolation != null)
        {
            var currentConfig = ConfigFileManager.Instance.CurrentConfig;

            yield return new WaitForSeconds(step.interpolation.delay);

            yield return Tween.Position(target: vfxEffect.transform, startValue: previousConfig.pointClouds[pointCloudID - 1].position.ToVector3(), endValue: currentConfig.pointClouds[pointCloudID - 1].position.ToVector3(), duration: step.interpolation.duration, ease: step.interpolation.ease)
                .Group(Tween.Rotation(target: vfxEffect.transform, startValue: previousConfig.pointClouds[pointCloudID - 1].rotation.ToVector3(), endValue: currentConfig.pointClouds[pointCloudID - 1].rotation.ToVector3(), duration: step.interpolation.duration, ease: step.interpolation.ease)).ToYieldInstruction();
            //.Group(Tween.Scale(target: vfxEffect.transform, startValue: previousConfig.pointClouds[pointCloudID].scale.ToVector3(), endValue: currentConfig.pointClouds[pointCloudID].scale.ToVector3(), duration: step.interpolation.duration, ease: step.interpolation.ease)).ToYieldInstruction();

            yield return new WaitForSeconds(step.displayTime - step.interpolation.duration - step.interpolation.delay);
        }
        else
        {
            yield return new WaitForSeconds(step.displayTime);
        }

        //pointCloudReplayBuffer.enabled = false;
        pointCloudReplayBuffer.enableReplay = false;
        vfxEffect.enabled = false;

        OutputFileManager.Instance.SaveOutputEntry();

        // wait for the memory properly allocated
        yield return new WaitForSeconds(0.5f);*/

    }

    public void Exit()
    {
        /*var pointCloudContainer = PointCloudManager.Instance.GetPointCloudContainer(int.Parse(step.cameraID));

        VisualEffect vfxEffect = pointCloudContainer.vfx;
        PointCloudReplayBuffer pointCloudReplayBuffer = pointCloudContainer.replayBuffer;
        RealtimeDelaySwitcher realtimeDelaySwitcher = pointCloudContainer.realtimeDelaySwitcher;

        pointCloudReplayBuffer.enableReplay = false;
        vfxEffect.enabled = false;

        Tween.StopAll(onTarget: vfxEffect.transform);*/

    }
}
