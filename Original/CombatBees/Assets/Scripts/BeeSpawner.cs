using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BeeSpawner : ComponentSystem
{
    private NativeArray<EntityArchetype> BeePrototypes;
    public float minBeeSize;
    public float maxBeeSize;
    public float maxSpawnSpeed;
    private Unity.Mathematics.Random rand;

    private void SetupPrototypes()
    {
        if (BeePrototypes.IsCreated)
            BeePrototypes.Dispose();
        BeePrototypes = new NativeArray<EntityArchetype>(2, Allocator.Persistent);
        EntityManager manager = World.Active.EntityManager;

        BeePrototypes[0] = manager.CreateArchetype(typeof(BeeTeam0), typeof(BeeSize), typeof(Velocity), typeof(FlightTarget), typeof(Translation));
        BeePrototypes[1] = manager.CreateArchetype(typeof(BeeTeam1), typeof(BeeSize), typeof(Velocity), typeof(FlightTarget), typeof(Translation));
    }

    protected override void OnUpdate()
    {
        EntityManager manager = World.Active.EntityManager;
        Entities.ForEach((Entity e, ref Translation translation, ref BeeSpawnRequest request) =>
        {
            float3 dir = rand.NextFloat3(-1.0f, 1.0f);
            float magnitude = sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            float3 startingVelocity = float3(0);
            if (magnitude != 0)
            {
                startingVelocity = dir / magnitude * maxSpawnSpeed;
            }

            Entity spawnedEntity = manager.CreateEntity(BeePrototypes[request.Team]);
            manager.SetComponentData<BeeSize>(spawnedEntity, new BeeSize() { Size = rand.NextFloat(minBeeSize, maxBeeSize), TeamColor = request.Team });
            manager.SetComponentData<Translation>(spawnedEntity, new Translation() { Value = translation.Value });
            manager.SetComponentData<Velocity>(spawnedEntity, new Velocity() { v = startingVelocity });
});

        manager.DestroyEntity(GetEntityQuery(typeof(BeeSpawnRequest)));
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        SetupPrototypes();        
        rand = new Unity.Mathematics.Random(3);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (BeePrototypes.IsCreated)
            BeePrototypes.Dispose();
    }
}