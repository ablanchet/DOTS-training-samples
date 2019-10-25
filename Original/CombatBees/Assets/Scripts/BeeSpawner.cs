using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using UnityEngine;

public struct BeeSpawner
{
    private EntityArchetype BeePrototype0;
    private EntityArchetype BeePrototype1;
    public float minBeeSize;
    public float maxBeeSize;
    public float maxSpawnSpeed;
    private Unity.Mathematics.Random rand;

    public void AdvanceRandomizer()
    {
        rand.NextFloat();
    }

    public void SpawnBees(EntityManager manager, float3 location, int spawncount, sbyte team)
    {
        for (int x = 0; x < spawncount; ++x)
        {
            Entity spawnedEntity = manager.CreateEntity(team == 0 ? BeePrototype0 : BeePrototype1);
            manager.SetComponentData<BeeSize>(spawnedEntity, new BeeSize() { Size = rand.NextFloat(minBeeSize, maxBeeSize), TeamColor = team });
            manager.SetComponentData<Translation>(spawnedEntity, new Translation() { Value = location });
            manager.SetComponentData<Velocity>(spawnedEntity, GetInitialVelocity(ref rand));
            manager.SetComponentData<Death>(spawnedEntity, new Death { Dying = false });
        }
    }
    public void SpawnBees(EntityCommandBuffer.Concurrent commandBuffer, int index, float3 location, int spawncount, sbyte team)
    {
        for(int x = 0; x < spawncount; ++x)
        {
            Entity spawnedEntity = commandBuffer.CreateEntity(index, team==0 ? BeePrototype0 : BeePrototype1);
            commandBuffer.SetComponent<BeeSize>(index, spawnedEntity, new BeeSize() { Size = rand.NextFloat(minBeeSize, maxBeeSize), TeamColor = team });
            commandBuffer.SetComponent<Translation>(index, spawnedEntity, new Translation() { Value = location });
            commandBuffer.SetComponent<Velocity>(index, spawnedEntity, GetInitialVelocity(ref rand));
            commandBuffer.SetComponent<Death>(index, spawnedEntity, new Death { Dying = false });
        }
    }

    [BurstCompile]
    private Velocity GetInitialVelocity(ref Unity.Mathematics.Random rand)
    {
        Vector3 dir = rand.NextFloat3();
        dir.Normalize();
        return new Velocity() { v = dir * maxSpawnSpeed };
    }


     public BeeSpawner(EntityManager manager, float _minBeeSize, float _maxBeeSize, float _maxSpawnSpeed)
    {
        minBeeSize = _minBeeSize;
        maxBeeSize = _maxBeeSize;
        maxSpawnSpeed = _maxSpawnSpeed;

        BeePrototype0 = manager.CreateArchetype(typeof(BeeTeam0), typeof(BeeSize), typeof(Velocity), typeof(FlightTarget), typeof(Translation), typeof(Death));
        BeePrototype1 = manager.CreateArchetype(typeof(BeeTeam1), typeof(BeeSize), typeof(Velocity), typeof(FlightTarget), typeof(Translation), typeof(Death));

        rand = new Unity.Mathematics.Random(3);
    }
}