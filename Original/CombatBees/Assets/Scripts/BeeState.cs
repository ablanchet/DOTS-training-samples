using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct BeeState : IComponentData
{
    public bool Attacking;
    public bool Dead;    
}
