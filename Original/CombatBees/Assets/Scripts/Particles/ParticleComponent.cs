using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ParticleComponent : IComponentData
{
    public ParticleType type;
    public bool stuck;
    public float life;
    public float lifeDuration;
    public float4 color;
    public float3 size;
}
