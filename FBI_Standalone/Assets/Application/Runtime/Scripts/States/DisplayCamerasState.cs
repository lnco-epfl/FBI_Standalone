using com.rfilkov.kinect;
using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static PrimeTween.Sequence;

public class DisplayCamerasState : IState
{
    private DisplayCamerasStep step;

    private PrimeTween.Sequence sequence;

    private Tween rigPositionTween;
    private Tween rigRotationTween;

    private bool rigInterpolationStarted = false;



    public void Enter(SequenceStep sequenceStep)
    {
        step = sequenceStep as DisplayCamerasStep;

        rigInterpolationStarted = false;
        rigPositionTween = default;
        rigRotationTween = default;

    }

    public IEnumerator Execute()
    {

        sequence = PrimeTween.Sequence.Create(cycles: 1, SequenceCycleMode.Restart);

        StringBuilder displayText = new StringBuilder();

        StringBuilder cameradelays = new StringBuilder();
        cameradelays.Append("[");
        StringBuilder cameraIDs = new StringBuilder();
        cameraIDs.Append("[");

        bool asInterpolation = false;
        bool asDissolution = false;
        bool asFade = false;

        float afterInterpolationMaxWait = 0.0f;
        float afterDissolutionMaxWait = 0.0f;
        float afterFadeMaxWait = 0.0f;


        for (int i = 0; i < step.camerasData.Count; i++)
        {
            var cameraData = step.camerasData[i];

            var pointCloudID = int.Parse(cameraData.id);

            displayText.AppendLine("");

            cameradelays.Append(cameraData.delay);
            if(step.camerasData.Count >= 1 && step.camerasData.Count -1 > i)
            {
                cameradelays.Append(",");
            }
         
            cameraIDs.Append(pointCloudID);
            if (step.camerasData.Count >= 1 && step.camerasData.Count -1 > i)
            {
                cameraIDs.Append(",");
            }

            CameraConfigFile configFile = null;
            CameraConfigFile startConfigFile = null;

            if (cameraData.delay > 0.0f)
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
                if (CameraConfigFileManager.Instance.IsValideConfigName(cameraData.configName))
                {
                    if (CameraConfigFileManager.Instance.CurrentConfig == null || CameraConfigFileManager.Instance.CurrentConfig.configName != cameraData.configName)
                    {
                        configFile = CameraConfigFileManager.Instance.Load(cameraData.configName);
                    }
                }
                else
                {
                    EventFileManager.Error($"[DisplayCamerasState] Config file configName:{cameraData.configName} not found or config was already loaded");
                }
            }

            if (configFile == null)
            {
                configFile = CameraConfigFileManager.Instance.CurrentConfig;
            }

            PointCloud pointCloud = null;

            if (configFile != null)
            {
                pointCloud = PointCloudManager.Instance.SpawnPointCloud(pointCloudID, cameraData.delay, configFile);
                if (pointCloud != null)
                {
                    step.ownedPointClouds.Add(pointCloud);
                }
            }

            displayText.Append($"Display Camera {cameraData.id} for {step.GetDuration()} seconds with {cameraData.delay} of delay");

            if (cameraData.interpolation != null)
            {
                asInterpolation = true;

                displayText.Append(" with interpolation");

                if (!string.IsNullOrEmpty(cameraData.interpolation.startConfigName))
                {
                    if (CameraConfigFileManager.Instance.IsValideConfigName(cameraData.interpolation.startConfigName) && CameraConfigFileManager.Instance.CurrentConfig.configName != cameraData.interpolation.startConfigName)
                    {
                        startConfigFile = CameraConfigFileManager.Instance.Load(cameraData.interpolation.startConfigName);

                        pointCloud.SetTransform(startConfigFile.pointClouds[pointCloudID - 1].position.ToVector3(), startConfigFile.pointClouds[pointCloudID - 1].rotation.ToVector3(), startConfigFile.pointClouds[pointCloudID - 1].scale.ToVector3());
                    }
                    else
                    {
                        EventFileManager.Error($"[DisplayCamerasState] Config file startConfigName:{cameraData.interpolation.startConfigName} not found");
                    }
                }

                var startTransformData = startConfigFile.pointClouds[pointCloudID - 1];
                var endTransformData = configFile.pointClouds[pointCloudID - 1];

                var capturedPointCloud = pointCloud;

                pointCloud.SetInterpolationMatrix(startTransformData, endTransformData);

                sequence.Insert(atTime: 0, Tween.Delay(duration: cameraData.interpolation.delay).OnComplete(() => capturedPointCloud.StartInterpolation(cameraData.interpolation.duration, () =>
                {
                    pointCloud.SetTransform(endTransformData.position.ToVector3(), endTransformData.rotation.ToVector3(), endTransformData.scale.ToVector3());
                    pointCloud.HideInterpolation();
                    pointCloud.DisplayMain();
                }))); 

                /*sequence.Insert(atTime: cameraData.interpolation.delay,
                    Tween.Position(target: pointCloud.transform, startValue: startConfigFile.pointClouds[pointCloudID - 1].position.ToVector3(), endValue: configFile.pointClouds[pointCloudID - 1].position.ToVector3(), duration: cameraData.interpolation.duration, ease: cameraData.interpolation.ease)
                    .Group(Tween.Rotation(target: pointCloud.transform, startValue: startConfigFile.pointClouds[pointCloudID - 1].rotation.ToVector3(), endValue: configFile.pointClouds[pointCloudID - 1].rotation.ToVector3(), duration: cameraData.interpolation.duration, ease: cameraData.interpolation.ease))
                    .Group(Tween.Scale(target: pointCloud.transform, startValue: startConfigFile.pointClouds[pointCloudID - 1].scale.ToVector3(), endValue: configFile.pointClouds[pointCloudID - 1].scale.ToVector3(), duration: cameraData.interpolation.duration, ease: cameraData.interpolation.ease)));
                */

                afterInterpolationMaxWait = Mathf.Max(step.displayTime - cameraData.interpolation.duration - cameraData.interpolation.delay, afterInterpolationMaxWait);
            }

            if (cameraData.dissolution != null)
            {
                asDissolution = true;

                displayText.Append(" with dissolution");

                pointCloud.SetDissolutionDuration(cameraData.dissolution.duration);

                var capturedPointCloud = pointCloud;

                sequence.Insert(atTime: 0, Tween.Delay(duration: cameraData.dissolution.delay).OnComplete(() => capturedPointCloud.StartDissolution()));

                afterDissolutionMaxWait = Mathf.Max(step.displayTime - cameraData.dissolution.delay, afterDissolutionMaxWait);

            }

            if (cameraData.fade != null)
            {
                asFade = true;

                displayText.Append(" with fade");

                var capturedPointCloud = pointCloud;

                sequence.Insert(atTime: 0, Tween.Delay(duration: cameraData.fade.delay).OnComplete(() => capturedPointCloud.StartFadeOut(cameraData.fade.duration)));

                afterFadeMaxWait = Mathf.Max(step.displayTime - cameraData.fade.delay, afterFadeMaxWait);

            }


        }

        float afterRigInterpolationMaxWait = 0.0f;

        if (step.rigInterpolation != null)
        {
            var rigData = step.rigInterpolation;
            var origin = PlayerManager.Instance.transform;

            displayText.Append($" with Rig interpolation from {rigData.startPosition} to {rigData.endPosition}");

            rigPositionTween = Tween.Position(target: origin, startValue: rigData.startPosition, endValue: rigData.endPosition, duration: rigData.duration, ease: rigData.ease);

            sequence.Insert(atTime: rigData.delay, rigPositionTween);

            sequence.Insert(atTime: rigData.delay, Tween.Delay(duration: 0f).OnComplete(() => rigInterpolationStarted = true));

            if (!Mathf.Approximately(rigData.startYaw, rigData.endYaw))
            {
                rigRotationTween = Tween.Custom(startValue: rigData.startYaw, endValue: rigData.endYaw, duration: rigData.duration, ease: rigData.ease,
                    onValueChange: yaw => origin.rotation = Quaternion.Euler(0f, yaw, 0f));

                sequence.Insert(atTime: rigData.delay, rigRotationTween);
            }

            afterRigInterpolationMaxWait = Mathf.Max(step.displayTime - rigData.delay, afterRigInterpolationMaxWait);
        }

        cameradelays.Append("]");
        OutputFileManager.Instance.OutputFileData.CameraDelays = cameradelays.ToString();

        cameraIDs.Append("]");
        OutputFileManager.Instance.OutputFileData.CameraIDs = cameraIDs.ToString();

        OutputFileManager.Instance.OutputFileData.TimeSinceStart = ExperimentManager.Instance.ElaspedTimeSinceStart;

        OutputFileManager.Instance.OutputFileData.StepType = "DisplayCameras";
        OutputFileManager.Instance.OutputFileData.StepCount = ExperimentManager.Instance.SequenceCurrentStep;

        OutputFileManager.Instance.OutputFileData.CameraDisplayDuration = step.displayTime;

        OutputFileManager.Instance.OutputFileData.AsDissolution = asDissolution;
        OutputFileManager.Instance.OutputFileData.AsInterpolation = asInterpolation;

        PointCloudManager.Instance.DisplaySpawnedPointClouds(step.ownedPointClouds);

        EventFileManager.Log($"[DisplayCameraState] Display Cameras :" + displayText.ToString());

        if (asInterpolation || asDissolution || asFade || step.rigInterpolation != null)
        {
            yield return sequence.ToYieldInstruction();

            List<float> afterWaits = new List<float>();

            if(afterDissolutionMaxWait > 0)
            {
                afterWaits.Add(afterDissolutionMaxWait);
            }

            if (afterInterpolationMaxWait > 0)
            {
                afterWaits.Add(afterInterpolationMaxWait);
            }

            if (afterRigInterpolationMaxWait > 0)
            {
                afterWaits.Add(afterRigInterpolationMaxWait);
            }

            if (afterFadeMaxWait > 0)
            {
                afterWaits.Add(afterFadeMaxWait);
            }

            yield return new WaitForSeconds(afterWaits.Min());
        }
        else
        {
            yield return new WaitForSeconds(step.displayTime);
        }


        Debug.Log($"DisplayCamerasState execute {step.startTime}");
        PointCloudManager.Instance.HideSpawnedPointClouds(step.ownedPointClouds);

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

        if (sequence.isAlive)
        {
            sequence.Stop();
        }

        if (step.rigInterpolation != null && rigInterpolationStarted)
        {
            var rigData = step.rigInterpolation;
            var origin = PlayerManager.Instance.transform;

            origin.position = rigData.endPosition;

            if (!Mathf.Approximately(rigData.startYaw, rigData.endYaw))
            {
                origin.rotation = Quaternion.Euler(0f, rigData.endYaw, 0f);
            }
        }

        Debug.Log($"DisplayCamerasState Exit {step.startTime}");
        PointCloudManager.Instance.HideSpawnedPointClouds(step.ownedPointClouds);

        PointCloudManager.Instance.DespawnPointClouds(step.ownedPointClouds);

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