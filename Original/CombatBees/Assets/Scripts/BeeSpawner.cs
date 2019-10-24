using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BeeSpawner : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

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

    struct BeeSpawnerJob : IJobForEachWithEntity<BeeSpawnRequest, Translation>
    {
        [ReadOnly]
        public NativeArray<EntityArchetype> BeePrototypes;
        public float minBeeSize;
        public float maxBeeSize;
        public float maxSpawnSpeed;
        public Unity.Mathematics.Random rand;
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity e, int index, ref BeeSpawnRequest request, [ReadOnly] ref Translation translation)
        {
            float3 dir = rand.NextFloat3(-1.0f, 1.0f);
            float magnitude = sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            float3 startingVelocity = float3(0);
            if (magnitude != 0)
            {
                startingVelocity = dir / magnitude * maxSpawnSpeed;
            }

            Entity spawnedEntity = commandBuffer.CreateEntity(index, BeePrototypes[request.Team]);
            commandBuffer.SetComponent<BeeSize>(index, spawnedEntity, new BeeSize() { Size = rand.NextFloat(minBeeSize, maxBeeSize), TeamColor = request.Team });
            commandBuffer.SetComponent<Translation>(index, spawnedEntity, new Translation() { Value = translation.Value });
            commandBuffer.SetComponent<Velocity>(index, spawnedEntity, new Velocity() { v = startingVelocity });
            commandBuffer.DestroyEntity(index, e);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GetEntityQuery(typeof(BeeSpawnRequest)).CalculateChunkCount() == 0)
            return inputDeps;

        var spawnJob = new BeeSpawnerJob();
        spawnJob.commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        spawnJob.maxBeeSize = maxBeeSize;
        spawnJob.minBeeSize = minBeeSize;
        spawnJob.maxSpawnSpeed = maxSpawnSpeed;
        spawnJob.rand = rand;
        rand.NextFloat();
        spawnJob.BeePrototypes = BeePrototypes;

        JobHandle handle = spawnJob.Schedule(this, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

protected override void OnCreate()
    {
        base.OnCreate();
        SetupPrototypes();        
        rand = new Unity.Mathematics.Random(3);
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (BeePrototypes.IsCreated)
            BeePrototypes.Dispose();
    }
}