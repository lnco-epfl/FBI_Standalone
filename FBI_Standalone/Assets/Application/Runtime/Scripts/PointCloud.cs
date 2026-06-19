using com.rfilkov.kinect;
using JetBrains.Annotations;
using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloud : MonoBehaviour
{
  
    [Header("VisualEffect")]
    [SerializeField] private VisualEffect mainVisualEffect;
    [SerializeField] private VisualEffect dissolutionVisualEffect;
    [SerializeField] private VisualEffect interpolationVisualEffect;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;


    [SerializeField] private Transform startT;
    [SerializeField] private Transform endT;


    private Kinect4AzureInterface kinectAzureInterface;

    private int id = 0;
    private Tween fadeTween;

    public int Id { get => id;  }

    public bool isMainVFXVisible => mainVisualEffect.enabled;

    public void Init(int ID)
    {
        mainVisualEffect.enabled = false;
        dissolutionVisualEffect.enabled = false;

        interpolationVisualEffect.enabled = true;

        id = ID;

    }


    public void DisplayMain()
    {
        mainVisualEffect.enabled = true;
        mainVisualEffect.SetFloat("Alpha", 0.0f);

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
        dissolutionVisualEffect.enabled = false;
    }


    public void SetDissolutionDuration(float duration)
    {
        dissolutionVisualEffect.SetFloat("Duration", duration);
    }

    [ContextMenu("StartDissolution")]
    public void StartDissolution()
    {
        mainVisualEffect.enabled = true;
        dissolutionVisualEffect.enabled = true;

        dissolutionVisualEffect.Play();

        Tween.Custom(startValue: 1.0f, endValue: 0.0f, duration: 0.5f, ease: Ease.Linear, onValueChange: (float value) =>
        {
            mainVisualEffect.SetFloat("Alpha", value);
        }).OnComplete(() =>
        {
            mainVisualEffect.enabled = false;
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
    }

    [ContextMenu("SetInterpolationMatrix")]
    private void SetInterpolationMatrix()
    {
        Matrix4x4 startMatrix = Matrix4x4.TRS(startT.localPosition, startT.localRotation, startT.localScale);
        Debug.Log($"Start Matrix: {startMatrix}");
        interpolationVisualEffect.SetMatrix4x4("StartMatrix", startMatrix);


        Matrix4x4 endMatrix = Matrix4x4.TRS(endT.localPosition, endT.localRotation, endT.localScale);
        Debug.Log($"End Matrix: {endMatrix}");
        interpolationVisualEffect.SetMatrix4x4("EndMatrix", startT.worldToLocalMatrix * endT.localToWorldMatrix);
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
    }

}
