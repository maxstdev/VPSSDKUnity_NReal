using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathModel
{
    public string id { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    public float[] matrix = new float[16];
}
