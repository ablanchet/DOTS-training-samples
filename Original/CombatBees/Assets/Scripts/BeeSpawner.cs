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
    public NativeArray<Entity> BeePrototypes;
    public float minBeeSize;
    public float maxBeeSize;
    public float maxSpawnSpeed;
    private Unity.Mathematics.Random rand;

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

            Entity spawnedEntity = manager.Instantiate(BeePrototypes[request.Team]);
            manager.AddComponent<BeeSize>(spawnedEntity);
            manager.SetComponentData<BeeSize>(spawnedEntity, new BeeSize() { Size = rand.NextFloat(minBeeSize, maxBeeSize) });
            manager.SetComponentData<Translation>(spawnedEntity, new Translation() { Value = translation.Value });

            manager.AddComponent<Velocity>(spawnedEntity);
            manager.SetComponentData<Velocity>(spawnedEntity, new Velocity() { v = startingVelocity });
            manager.AddComponent<FlightTarget>(spawnedEntity);
            manager.AddComponent<NonUniformScale>(spawnedEntity);

            if (request.Team == 0)
            {
                manager.AddComponent<BeeTeam0>(spawnedEntity);
            }
            else
            {
                manager.AddComponent<BeeTeam1>(spawnedEntity);
            }
        });

        manager.DestroyEntity(GetEntityQuery(typeof(BeeSpawnRequest)));
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        BeePrototypes = new NativeArray<Entity>(2, Allocator.Persistent);
        rand = new Unity.Mathematics.Random(3);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (BeePrototypes.IsCreated)
            BeePrototypes.Dispose();
    }
}