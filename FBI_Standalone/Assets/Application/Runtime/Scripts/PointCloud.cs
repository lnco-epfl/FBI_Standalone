using com.rfilkov.kinect;
using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class PointCloud : MonoBehaviour
{

    [Header("VisualEffect")]
    [SerializeField] private VisualEffect mainVisualEffect;
    [SerializeField] private VisualEffect dissolutionVisualEffect;
    [SerializeField] private VisualEffect interpolationVisualEffect;

    [Header("Share Settings")]
    [Range(1000, 1000000)]
    [SerializeField] private int particuleCount = 500000;
    [Range(0.001f, 1.0f)]
    [SerializeField] private float particleSize = 0.0075f;

    [Header("Interpolation")]
    [SerializeField] private float interpolationNoiseSpeed = 0.1f;
    [SerializeField] private float interpolationNoiseAmplitude = 0.1f;

    [Header("Dissolution")]
    [SerializeField] private float stage1NoiseSpeed = 0.1f;
    [SerializeField] private float stage1NoiseAmplitude = 0.1f;
    [SerializeField] private float stage1End = 0.4f;
    [SerializeField] private float stage1SphereRadius = 0.05f;
    [SerializeField] private float stage2End = 0.6f;
    [SerializeField] private float stage2ColorIntensity = 10.0f;
    [SerializeField] private float stage2ParticleGrow = 0.1f;
    [SerializeField] private float stage3DelayFactor = 0.25f;
    [SerializeField] private float stage3SpreadFactor = 0.05f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Reference Point")]
    [Tooltip("Child transform of this PointCloud, exposed as a general-purpose local reference point relative to it. Currently drives Stage1SphereCenter for the dissolution effect.")]
    [SerializeField] private Transform referencePointTransform;
    [Tooltip("Visual gizmo (e.g. a simple sphere mesh), child of referencePointTransform, used only to help placing it in the editor. Never saved to the config file.")]
    [SerializeField] private GameObject referencePointGizmo;

    [Header("Debug")]
    [SerializeField] private InputActionReference debugDissolution;

    private Kinect4AzureInterface kinectAzureInterface;

    private FibonacciSphereBaker baker = new FibonacciSphereBaker();

    private int id = 0;
    private Tween fadeTween;

    public int Id { get => id; }

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
        if(isMainVFXVisible)
        {
            StartDissolution(5.0f, () => DisplayMain());
        }
        
    }

    public void Init(int ID)
    {
        mainVisualEffect.enabled = false;
        dissolutionVisualEffect.enabled = false;
        interpolationVisualEffect.enabled = false;

        id = ID;

        if (referencePointGizmo != null)
        {
            referencePointGizmo.SetActive(false);
        }
    }

    /// <summary>
    /// Local position (relative to this PointCloud) of the dissolution effect's sphere center.
    /// </summary>
    public void SetReferencePoint(Vector3 localPosition)
    {
        if (referencePointTransform != null)
        {
            referencePointTransform.localPosition = localPosition;
        }
    }

    public Vector3 GetReferencePoint() =>
        referencePointTransform != null ? referencePointTransform.localPosition : Vector3.zero;

    /// <summary>
    /// Offset from this PointCloud's own position, expressed along world-aligned axes (i.e. NOT
    /// affected by this PointCloud's rotation). An offset of (0,0,0) places the reference point
    /// exactly at the PointCloud's own position. Handy for UI controls that want to move the
    /// reference point along the scene's global axes (like Unity's "Global" tool handle mode)
    /// instead of the PointCloud's own (possibly rotated) local axes. The value stored in the
    /// config file (and sent to the VFX) always stays local to the PointCloud — this only
    /// changes how it's edited.
    /// </summary>
    public void SetReferencePointOffset(Vector3 worldAlignedOffset)
    {
        if (referencePointTransform != null)
        {
            referencePointTransform.position = transform.position + worldAlignedOffset;
        }
    }

    public Vector3 GetReferencePointOffset() =>
        referencePointTransform != null ? referencePointTransform.position - transform.position : Vector3.zero;

    /// <summary>
    /// Shows/hides the editor-only gizmo at the reference point. Purely a visual aid, never
    /// persisted to the config file.
    /// </summary>
    public void SetReferencePointGizmoVisible(bool visible)
    {
        if (referencePointGizmo != null)
        {
            referencePointGizmo.SetActive(visible);
        }
    }

    public void DisplayMain(bool NoFade = false)
    {
        mainVisualEffect.enabled = true;

        mainVisualEffect.SetInt("ParticuleCount", particuleCount);
        mainVisualEffect.SetFloat("ParticuleSize", particleSize);
        mainVisualEffect.SetFloat("Alpha", 0.0f);

        if (fadeTween.isAlive)
        {
            fadeTween.Stop();
        }


        if(NoFade)
        {
            mainVisualEffect.SetFloat("Alpha", 1.0f);
        }
        else
        {
            fadeTween = Tween.Custom(startValue: 0.0f, endValue: 1.0f, duration: fadeDuration, ease: Ease.InOutSine, onValueChange: (float value) =>
            {
                mainVisualEffect.SetFloat("Alpha", value);
            });
        }

    }

    public void HideMain()
    {
        if (mainVisualEffect == null)
        {
            return;
        }

        if (fadeTween.isAlive)
        {
            fadeTween.Stop();
        }

        fadeTween = Tween.Custom(startValue: 1.0f, endValue: 0.0f, duration: fadeDuration, ease: Ease.InOutSine, onValueChange: (float value) =>
        {
            if (mainVisualEffect != null)
            {
                mainVisualEffect.SetFloat("Alpha", value);
            }
        }).OnComplete(() =>
        {
            if (mainVisualEffect != null)
            {
                mainVisualEffect.enabled = false;
            }
          
        });
    }

    public void HideDissolution()
    {
        if (dissolutionVisualEffect != null)
        {
            dissolutionVisualEffect.enabled = false;
        }
    }

    public void HideInterpolation()
    {
        if (interpolationVisualEffect != null)
        {
            interpolationVisualEffect.enabled = false;
        }   
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
    public void StartDissolution(float duration, Action callback = null)
    {
        mainVisualEffect.enabled = true;
        dissolutionVisualEffect.enabled = true;

        var pos = GetReferencePoint();
        var texture = baker.BakeSphereTexture(particuleCount, stage1SphereRadius);

        dissolutionVisualEffect.SetInt("ParticuleCount", particuleCount);
        dissolutionVisualEffect.SetFloat("ParticuleSize", particleSize);
        dissolutionVisualEffect.SetFloat("Alpha", 1.0f);

        dissolutionVisualEffect.SetFloat("Duration", duration);

        //stage 1
        dissolutionVisualEffect.SetFloat("Stage1End", stage1End);
        dissolutionVisualEffect.SetTexture("Stage1FibonacciSphere", texture);
        dissolutionVisualEffect.SetVector3("Stage1SphereCenter", pos);
        dissolutionVisualEffect.SetFloat("Stage1NoiseSpeed", stage1NoiseSpeed);
        dissolutionVisualEffect.SetFloat("Stage1NoiseAmplitude", stage1NoiseAmplitude);

        //stage 2
        dissolutionVisualEffect.SetFloat("Stage2End", stage2End);
        dissolutionVisualEffect.SetFloat("Stage2ColorIntensity", stage2ColorIntensity);
        dissolutionVisualEffect.SetFloat("Stage2ParticleGrow", stage2ParticleGrow);

        //stage 3
        dissolutionVisualEffect.SetFloat("Stage3DelayFactor", stage3DelayFactor);
        dissolutionVisualEffect.SetFloat("Stage3SpreadFactor", stage3SpreadFactor);

        dissolutionVisualEffect.Play();

        StartFadeOut(fadeDuration);

        Tween.Delay(duration: duration).OnComplete(() =>
        {
            if(dissolutionVisualEffect != null)
            {
                dissolutionVisualEffect.enabled = false;
            }
            callback?.Invoke();
        });


    }

    public void StartInterpolation(float duration, EasingType ease, Action callback = null)
    {
        interpolationVisualEffect.enabled = true;
        mainVisualEffect.enabled = false;

        var curve = EasingCurveFactory.Create(ease);

        interpolationVisualEffect.SetInt("ParticuleCount", particuleCount);
        interpolationVisualEffect.SetFloat("ParticuleSize", particleSize);
        interpolationVisualEffect.SetFloat("Alpha", 1.0f);

        interpolationVisualEffect.SetFloat("Duration", duration);
        interpolationVisualEffect.SetAnimationCurve("LerpEasingCurve", curve);
        interpolationVisualEffect.SetFloat("NoiseSpeed", interpolationNoiseSpeed);
        interpolationVisualEffect.SetFloat("NoiseAmplitude", interpolationNoiseAmplitude);


        interpolationVisualEffect.Play();

    
        Tween.Delay(duration: duration).OnComplete(() =>
        {
            if (interpolationVisualEffect != null)
            {
                interpolationVisualEffect.enabled = false;
            }
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
        SetTextures(interpolationVisualEffect, colorRenderTexture, vertexRenderTexture);
        SetTextures(dissolutionVisualEffect, colorRenderTexture, vertexRenderTexture);

    }

    private void SetTextures(VisualEffect visualEffect, RenderTexture colorTexture, RenderTexture vertexTexture)
    {
        visualEffect.SetTexture("ColorTexture", colorTexture);
        visualEffect.SetTexture("VertexTexture", vertexTexture);
    }

    public void SetInterpolationMatrix(ObjectTransformData startData, ObjectTransformData endData)
    {
        Matrix4x4 startMatrix = ObjectTransformData.ToMatrix(startData);
        Matrix4x4 endMatrix = ObjectTransformData.ToMatrix(endData);

        Matrix4x4 relativeMatrix = startMatrix.inverse * endMatrix;

        interpolationVisualEffect.SetMatrix4x4("EndMatrix", relativeMatrix);
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
        mainVisualEffect.SetFloat("ClampXMin", xMin);
        mainVisualEffect.SetFloat("ClampXMax", xMax);
        mainVisualEffect.SetFloat("ClampYMin", yMin);
        mainVisualEffect.SetFloat("ClampYMax", yMax);

        dissolutionVisualEffect.SetFloat("ClampXMin", xMin);
        dissolutionVisualEffect.SetFloat("ClampXMax", xMax);
        dissolutionVisualEffect.SetFloat("ClampYMin", yMin);
        dissolutionVisualEffect.SetFloat("ClampYMax", yMax);

        interpolationVisualEffect.SetFloat("ClampXMin", xMin);
        interpolationVisualEffect.SetFloat("ClampXMax", xMax);
        interpolationVisualEffect.SetFloat("ClampYMin", yMin);
        interpolationVisualEffect.SetFloat("ClampYMax", yMax);
    }





}