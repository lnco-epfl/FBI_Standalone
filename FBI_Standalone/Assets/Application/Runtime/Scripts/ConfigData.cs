using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableVector3
{
    public float x, y, z;

    public SerializableVector3() { }
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class ObjectTransformData
{
    public int cameraID;
    public SerializableVector3 position;
    public SerializableVector3 rotation; // eulerAngles
    public SerializableVector3 scale;

    public ObjectTransformData() { }

    public ObjectTransformData(int cameraID, Transform t)
    {
        cameraID = cameraID;
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
}

[Serializable]
public class ConfigFile
{
    public string configName = "NewConfig";
    public string createdAt = "";
    public string lastModified = "";
    public List<ObjectTransformData> pointClouds = new List<ObjectTransformData>();
}