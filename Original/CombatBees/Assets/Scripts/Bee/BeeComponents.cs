using Unity.Entities;
using Unity.Mathematics;

public struct BeeTeam0 : IComponentData { }

public struct BeeTeam1 : IComponentData { }

public struct BeeSize : IComponentData
{
    public float Size;
    public bool Attacking; //used to change the bee appearance during an attack

    public float3 SmoothPosition;
    public float3 SmoothDirection;
}

public struct PendingDeath : IComponentData
{
    public Entity EntityThatWillDie;
}

public struct Velocity : IComponentData
{
    public float3 v;
}

public struct Death : IComponentData
{
    public float DeathTimer;
    public bool FirstUpdateDone;
}