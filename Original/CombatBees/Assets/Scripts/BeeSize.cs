using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct BeeSize : IComponentData
{
    public float3 SmoothPosition;
    public float3 SmoothDirection;

    public float Size;
    public bool Attacking; //used to change the bee appearance during an attack
    public bool Faded;
    public sbyte TeamColor;

}
