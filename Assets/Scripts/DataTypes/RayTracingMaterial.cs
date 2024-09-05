using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RayTracingMaterial
{
    public Color color;
    public Color emissionColor;
    public float emissionStrength;
    [Range(0.0f, 1.0f)] public float albedo;
};


