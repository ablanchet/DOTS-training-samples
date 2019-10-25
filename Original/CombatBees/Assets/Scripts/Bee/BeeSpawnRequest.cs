using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct BeeSpawnRequest : IComponentData
{
    public sbyte teamIdx;
}
