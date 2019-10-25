using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using UnityEngine;

public struct ParticleSpawner
{
    private EntityArchetype ParticlePrototype;
    private Unity.Mathematics.Random rand;

    //const int instancesPerBatch = 1023;
	//const int maxParticleCount = 10*instancesPerBatch;

    public void SpawnParticle(EntityCommandBuffer.Concurrent commandBuffer, int index, float3 location, int spawncount, ParticleType type, float lifeDuration, float3 size, float4 color, float3 velocity, float velocityJitter)
    {
        for (int x = 0; x < spawncount; ++x)
        {
            Entity spawnedEntity = commandBuffer.CreateEntity(index, ParticlePrototype);
            commandBuffer.SetComponent<ParticleComponent>(index, spawnedEntity, new ParticleComponent() {
                type = type,
                stuck = false,
                life = 1,
                lifeDuration = lifeDuration,
                size = size,
                color = color
            });
            commandBuffer.SetComponent<Velocity>(index, spawnedEntity, new Velocity {
                v = velocity
            });
            commandBuffer.SetComponent<Translation>(index, spawnedEntity, new Translation { Value = location });
        }
    }

    public void CreateBlood(EntityCommandBuffer.Concurrent commandBuffer, int index, float3 location, int spawnCount, float3 velocity, float velocityJitter=6f) {
        float4 color = new float4(0.8f+rand.NextFloat(0.2f), rand.NextFloat(0.15f), rand.NextFloat(0.15f), 1f) * rand.NextFloat(0.7f, 0.9f);
        float3 size = rand.NextFloat3(.1f, .2f);
        float lifeDuration = rand.NextFloat(3f, 5f);
        velocity = velocity + rand.NextFloat3(-1, 1) * 5f;
        SpawnParticle(commandBuffer, index, location, spawnCount, ParticleType.Blood, lifeDuration, size, color, velocity, velocityJitter);
    }

    public void CreateSpawnFlash(EntityCommandBuffer.Concurrent commandBuffer, int index, float3 location, int spawnCount, float3 velocity, float velocityJitter=6f) {
        float4 color = new float4(1f, 1f, 1f, 1f);
        float3 size = rand.NextFloat3(1f, 2f);
        float lifeDuration = rand.NextFloat(.25f, .5f);
        velocity = velocity + rand.NextFloat3(-1, 1) * 5f;
        SpawnParticle(commandBuffer, index, location, spawnCount, ParticleType.SpawnFlash, lifeDuration, size, color, velocity, velocityJitter);
    }

     public ParticleSpawner(EntityManager manager)
    {
        ParticlePrototype = manager.CreateArchetype(typeof(ParticleComponent),typeof(Velocity), typeof(Translation), typeof(Rotation));

        rand = new Unity.Mathematics.Random(3);
    }
}