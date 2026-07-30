using com.rfilkov.kinect;
using JetBrains.Annotations;
using PrimeTween;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using YamlDotNet.Core.Tokens;

public class PointCloud : MonoBehaviour
{
  
    [Header("VisualEffect")]
    [SerializeField] private VisualEffect mainVisualEffect;
    [SerializeField] private VisualEffect dissolutionVisualEffect;
    [SerializeField] private VisualEffect interpolationVisualEffect;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Debug")]
    [SerializeField] private InputActionReference debugDissolution;

    private Kinect4AzureInterface kinectAzureInterface;

    private int id = 0;
    private Tween fadeTween;

    public int Id { get => id;  }

    public bool isMainVFXVisible => mainVisualEffect.enabled;

    private void OnEnable()
    {
        debugDissolution.action.performed += OnDebugDissolutionPerformed;
    }

    private void OnDisable()
    {
        debugDissolution.action.performed -= OnDebugDissolutionPerformed;
    }

    private void OnDebugDissolutionPerformed(InputAction.CallbackContext obj)
    {
        StartDissolution(5.0f);
    }

    public void Init(int ID)
    {
        mainVisualEffect.enabled = false;
        dissolutionVisualEffect.enabled = false;
        interpolationVisualEffect.enabled = false;

        id = ID;

    }


    public void DisplayMain()
    {
        mainVisualEffect.enabled = true;
        mainVisualEffect.SetFloat("Alpha", 0.0f);

        Debug.Log($"pointcloud {this.name} DisplayMain");

        if (fadeTween.isAlive)
        {
            fadeTween.Stop();
        }

        fadeTween = Tween.Custom(startValue: 0.0f, endValue: 1.0f, duration: fadeDuration, ease: Ease.InOutSine, onValueChange: (float value) =>
        {
            mainVisualEffect.SetFloat("Alpha", value);
        });
    }

    public void HideMain()
    {
        if (fadeTween.isAlive)
        {
            fadeTween.Stop();
        }

        Debug.Log($"pointcloud {this.name} HideMain");

        fadeTween = Tween.Custom(startValue: 1.0f, endValue: 0.0f, duration: fadeDuration, ease: Ease.InOutSine, onValueChange: (float value) =>
        {
            mainVisualEffect.SetFloat("Alpha", value);
        }).OnComplete(() =>
        {
            mainVisualEffect.enabled = false;
        });
    }

    public void HideDissolution()
    {
        Debug.Log($"pointcloud {this.name} HideDissolution");
        dissolutionVisualEffect.enabled = false;
    }

    public void HideInterpolation()
    {
        Debug.Log($"pointcloud {this.name} HideInterpolation");
        interpolationVisualEffect.enabled = false;
    }

    public void SetDissolutionDuration(float duration)
    {
        Debug.Log($"pointcloud {this.name} SetDissolutionDuration {duration}");
        dissolutionVisualEffect.SetFloat("Duration", duration);
    }

    public void StartFadeOut(float duration)
    {
        fadeTween = Tween.Custom(startValue: 1.0f, endValue: 0.0f, duration: duration, ease: Ease.InOutSine, onValueChange: (float value) =>
        {
            mainVisualEffect.SetFloat("Alpha", value);
        }).OnComplete(() =>
        {
            mainVisualEffect.enabled = false;
        });
    }

    [ContextMenu("StartDissolution")]
    public void StartDissolution(float duration)
    {
        Debug.Log($"pointcloud {this.name} StartDissolution");

        mainVisualEffect.enabled = false;
        dissolutionVisualEffect.enabled = true;

        int particuleCount = 500000;
        float sphereRadius = 0.05f;

        var pos = new Vector3(0.0f, -0.148f, 1.658f);

        dissolutionVisualEffect.SetVector3("SphereCenter", pos);

        dissolutionVisualEffect.SetInt("ParticuleCount", particuleCount);

        var texture = FibonacciSphereBaker.BakeSphereTexture(particuleCount, sphereRadius);
        //dissolutionVisualEffect.SetFloat("SphereRadius", sphereRadius);
        dissolutionVisualEffect.SetTexture("FibonacciSphere", texture);

        var EffectAgeID = Shader.PropertyToID("EffectAge");
        dissolutionVisualEffect.SetFloat(EffectAgeID, 0.0f);

        dissolutionVisualEffect.SetFloat("Duration", duration);

        dissolutionVisualEffect.Play();

        Tween.Custom(startValue: 0.0f, endValue: 1.0f, duration: duration, ease: Ease.Linear, onValueChange: (float value) =>
        {
            dissolutionVisualEffect.SetFloat(EffectAgeID, value);
        }).OnComplete(() =>
        {
            dissolutionVisualEffect.enabled = false;
        });

        Tween.Custom(startValue: 1.0f, endValue: 0.0f, duration: 0.5f, ease: Ease.Linear, onValueChange: (float value) =>
        {
            //mainVisualEffect.SetFloat("Alpha", value);
        }).OnComplete(() =>
        {
            mainVisualEffect.enabled = false;
        });
    }

    public void StartInterpolation(float duration, EasingType ease, Action callback = null)
    {
        interpolationVisualEffect.enabled = true;
        interpolationVisualEffect.Play();

        mainVisualEffect.enabled = false;

        var curve = EasingCurveFactory.Create(ease);

        interpolationVisualEffect.SetAnimationCurve("EasingCurve", curve);
        interpolationVisualEffect.SetFloat("Duration", duration);

        Tween.Delay(duration: duration).OnComplete(() =>
        {
            interpolationVisualEffect.enabled = false;
            callback?.Invoke();
        });
    }

    public void SetKinectInterface(Kinect4AzureInterface kinect4Azure)
    {
        kinectAzureInterface = kinect4Azure;
    }

    public void SetRenderTextures(RenderTexture colorRenderTexture, RenderTexture vertexRenderTexture)
    {
        SetTextures(mainVisualEffect, colorRenderTexture, vertexRenderTexture);
        SetTextures(dissolutionVisualEffect, colorRenderTexture, vertexRenderTexture);
        SetInterpolationRenderTextures(colorRenderTexture, vertexRenderTexture, colorRenderTexture, vertexRenderTexture);
    }

    public void SetInterpolationRenderTextures(RenderTexture startColorRenderTexture, RenderTexture startVertexRenderTexture, RenderTexture endColorRenderTexture, RenderTexture endVertexRenderTexture)
    {
        interpolationVisualEffect.SetTexture("Color Start", startColorRenderTexture);
        interpolationVisualEffect.SetTexture("Vertex Start", startVertexRenderTexture);

        interpolationVisualEffect.SetTexture("Color End", endColorRenderTexture);
        interpolationVisualEffect.SetTexture("Vertex End", endVertexRenderTexture);
    }

    public void SetInterpolationMatrix(ObjectTransformData startData, ObjectTransformData endData)
    {
        Matrix4x4 startMatrix = ObjectTransformData.ToMatrix(startData);
        Matrix4x4 endMatrix = ObjectTransformData.ToMatrix(endData);

        Matrix4x4 relativeMatrix = startMatrix.inverse * endMatrix;

        interpolationVisualEffect.SetMatrix4x4("StartMatrix", startMatrix);
        interpolationVisualEffect.SetMatrix4x4("EndMatrix", relativeMatrix);
    }


    private void SetTextures(VisualEffect visualEffect, RenderTexture colorTexture, RenderTexture vertexTexture)
    {
        visualEffect.SetTexture("Color", colorTexture);
        visualEffect.SetTexture("Vertex", vertexTexture);
    }

    public void SetTransform(Vector3 postion, Vector3 rotation, Vector3 scale)
    {
        transform.position = postion;
        transform.rotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
        transform.localScale = scale;
    }

    public void SetTransform(Transform transform)
    {
        transform.position = transform.position;
        transform.rotation = transform.rotation;
    }

    public void SetCameraDeptValues(float depthMin, float depthMax)
    {
        kinectAzureInterface.maxDepthDistance = depthMax;
        kinectAzureInterface.minDepthDistance = depthMin;
    }

    public void SetClampValues(float xMin, float xMax, float yMin, float yMax)
    {
        mainVisualEffect.SetFloat("Clamp X Min", xMin);
        mainVisualEffect.SetFloat("Clamp X Max", xMax);
        mainVisualEffect.SetFloat("Clamp Y Min", yMin);
        mainVisualEffect.SetFloat("Clamp Y Max", yMax);

        dissolutionVisualEffect.SetFloat("Clamp X Min", xMin);
        dissolutionVisualEffect.SetFloat("Clamp X Max", xMax);
        dissolutionVisualEffect.SetFloat("Clamp Y Min", yMin);
        dissolutionVisualEffect.SetFloat("Clamp Y Max", yMax);

        interpolationVisualEffect.SetFloat("Clamp X Min", xMin);
        interpolationVisualEffect.SetFloat("Clamp X Max", xMax);
        interpolationVisualEffect.SetFloat("Clamp Y Min", yMin);
        interpolationVisualEffect.SetFloat("Clamp Y Max", yMax);
    }

 
   


}
