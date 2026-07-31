using CsvHelper;
using Intel.RealSense;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableColor
{
    public float r, g, b, a;

    public SerializableColor() { r = 0; g = 0; b = 0; a = 1; }
    public SerializableColor(Color c) { r = c.r; g = c.g; b = c.b; a = c.a; }
    public Color ToColor() => new Color(r, g, b, a);
}

[Serializable]
public class SerializableVector3
{
    public float x, y, z;

    public SerializableVector3() { }
    public SerializableVector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}


[Serializable]
public class UITransformData
{
    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public SerializableVector3 scale;
    public SerializableColor backgroundColor;

    public UITransformData()
    {
        position = new SerializableVector3(0f, 0f, 0f);
        rotation = new SerializableVector3(0f, 0f, 0f);
        scale = new SerializableVector3(1f, 1f, 1f);
        backgroundColor = new SerializableColor();
    }

    public UITransformData(Transform t)
    {
        position = new SerializableVector3(t.position);
        rotation = new SerializableVector3(t.eulerAngles);
        scale = new SerializableVector3(t.localScale);
        backgroundColor = new SerializableColor();
    }

    public UITransformData(Transform t, Color color)
    {
        position = new SerializableVector3(t.position);
        rotation = new SerializableVector3(t.eulerAngles);
        scale = new SerializableVector3(t.localScale);
        backgroundColor = new SerializableColor(color);
    }

    public void ApplyTo(Transform t)
    {
        t.position = position.ToVector3();
        t.eulerAngles = rotation.ToVector3();
        t.localScale = scale.ToVector3();
    }

    public override string ToString()
    {
        return $"UITransformData(position={position}, rotation={rotation}, scale={scale}, backgroundColor={backgroundColor})";
    }
}

[Serializable]
public class ObjectTransformData
{
    public int ID;

    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public SerializableVector3 scale;

    public float depthMax = 3.0f;
    public float depthMin = 0.1f;

    public float clampXMin = 0f;
    public float clampXMax = 1f;
    public float clampYMin = 0f;
    public float clampYMax = 1f;

    public SerializableVector3 referencePoint = new SerializableVector3(0.0f, 0.0f, 0.0f);

    public ObjectTransformData() { }

    public ObjectTransformData(int cameraID)
    {
        ID = cameraID;
        position = new SerializableVector3(0f, 0f, 0f);
        rotation = new SerializableVector3(0f, 0f, 0f);
        scale = new SerializableVector3(1f, 1f, 1f);
    }

    public ObjectTransformData(int cameraID, Transform t) : this(cameraID)
    {
        position = new SerializableVector3(t.position);
        rotation = new SerializableVector3(t.eulerAngles);
        scale = new SerializableVector3(t.localScale);
    }

    public void ApplyTo(Transform t)
    {
        t.position = position.ToVector3();
        t.eulerAngles = rotation.ToVector3();
        t.localScale = scale.ToVector3();
    }

    public static Matrix4x4 ToMatrix(ObjectTransformData data)
    {
        Vector3 pos = data.position.ToVector3();
        Quaternion rot = Quaternion.Euler(data.rotation.ToVector3());
        Vector3 scale = data.scale.ToVector3();
        return Matrix4x4.TRS(pos, rot, scale);
    }

    public override string ToString()
    {
        return $"ObjectTransformData(ID={ID}, position={position}, rotation={rotation}, scale={scale}, depthMin={depthMin}, depthMax={depthMax}, clampX=[{clampXMin},{clampXMax}], clampY=[{clampYMin},{clampYMax}], referencePoint={referencePoint})";
    }
}

[Serializable]
public class CameraConfigFile
{
    public string configName = "NewCameraConfig";
    public string createdAt = "";
    public string lastModified = "";

    public List<ObjectTransformData> pointClouds = new List<ObjectTransformData>();

    public override string ToString()
    {
        return $"ConfigFile(configName={configName}, createdAt={createdAt}, lastModified={lastModified}, pointClouds=[{string.Join(", ", pointClouds)}])";
    }
}

[Serializable]
public class DisplayConfigFile
{
    public string configName = "NewDisplayConfig";
    public string createdAt = "";
    public string lastModified = "";

    public UITransformData stimulusDisplay;

    public override string ToString()
    {
        return $"ConfigFile(configName={configName}, createdAt={createdAt}, lastModified={lastModified}, UICanvas=[{stimulusDisplay}])";
    }
}