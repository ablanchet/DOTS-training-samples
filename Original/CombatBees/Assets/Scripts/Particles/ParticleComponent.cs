using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ParticleComponent : IComponentData
{
    public ParticleType type;
    bool stuck;
    public float life;
    public float lifeDuration;
    float4 color;
}
