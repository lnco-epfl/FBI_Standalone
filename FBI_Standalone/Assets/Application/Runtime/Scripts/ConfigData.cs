using CsvHelper;
using System;
using System.Collections.Generic;
using UnityEngine;

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
public class ObjectTransformData
{
    public int ID;
    public SerializableVector3 position;
    public SerializableVector3 rotation; 
    public SerializableVector3 scale;

    public float depthMax = 3.0f;
    public float depthMin = 0.1f;

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

    public override string ToString()
    {
        return $"ObjectTransformData(ID={ID}, position={position}, rotation={rotation}, scale={scale}, depthMin={depthMin}, depthMax={depthMax})";
    }
}

[Serializable]
public class ConfigFile
{
    public string configName = "NewConfig";
    public string createdAt = "";
    public string lastModified = "";
    public List<ObjectTransformData> pointClouds = new List<ObjectTransformData>();

    public override string ToString()
    {
        return $"ConfigFile(configName={configName}, createdAt={createdAt}, lastModified={lastModified}, pointClouds=[{string.Join(", ", pointClouds)}])";
    }
}