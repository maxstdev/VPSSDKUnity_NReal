using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulateData
{
    public string imagePath { get; set; }
    public float imageWidth { get; set; }
    public float imageHeight { get; set; }
    public float[] intrinsic { get; set; }
    public float[] pose { get; set; }
    public double altitude { get; set; }
    public double latitude { get; set; }
    public double longitude { get; set; }
}
