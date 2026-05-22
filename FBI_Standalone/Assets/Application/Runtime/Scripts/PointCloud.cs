using com.rfilkov.kinect;
using PrimeTween;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloud : MonoBehaviour
{
  
    [Header("VisualEffect")]
    [SerializeField] private VisualEffect mainVisuEffect;
    [SerializeField] private VisualEffect dissolutionVisuEffect;

    private Kinect4AzureInterface kinectAzureInterface;

    private void Awake()
    {
        
    }

    public void SetDissolutionDuration(float duration)
    {
        dissolutionVisuEffect.SetFloat("Duration", duration);
    }
    public void StartDissolution()
    {
        dissolutionVisuEffect.enabled = true;
        Tween.Custom(startValue: 0.0f, endValue: 1.0f, duration: 1.0f, ease: Ease.Linear, onValueChange: (float value) =>
        {
            dissolutionVisuEffect.SetFloat("Float", value);
        }).OnComplete(() =>
        {
            mainVisuEffect.enabled = false;
        });
    }

    public void SetKinectInterface(Kinect4AzureInterface kinect4Azure)
    {
        kinectAzureInterface = kinect4Azure;
    }

    public void SetRenderTextures(RenderTexture colorRenderTexture, RenderTexture vertexRenderTexture)
    {
        SetTextures(mainVisuEffect, colorRenderTexture, vertexRenderTexture);
        SetTextures(dissolutionVisuEffect, colorRenderTexture, vertexRenderTexture);
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
        mainVisuEffect.SetFloat("Clamp X Min", xMin);
        mainVisuEffect.SetFloat("Clamp X Max", xMax);
        mainVisuEffect.SetFloat("Clamp Y Min", yMin);
        mainVisuEffect.SetFloat("Clamp Y Max", yMax);

        dissolutionVisuEffect.SetFloat("Clamp X Min", xMin);
        dissolutionVisuEffect.SetFloat("Clamp X Max", xMax);
        dissolutionVisuEffect.SetFloat("Clamp Y Min", yMin);
        dissolutionVisuEffect.SetFloat("Clamp Y Max", yMax);
    }

}
