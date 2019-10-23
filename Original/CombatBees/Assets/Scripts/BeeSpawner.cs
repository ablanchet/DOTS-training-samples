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
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    public NativeArray<Entity> BeePrototypes;
    public float minBeeSize;
    public float maxBeeSize;
    public float maxSpawnSpeed;
    private Unity.Mathematics.Random rand;
    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.

    //note: burst seems to cause an error at runtime?
    //[BurstCompile]
    struct SpawnerJob : IJobForEachWithEntity<Translation, BeeSpawnRequest>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public NativeArray<Entity> BeePrototypes;
        public float minBeeSize;
        public float maxBeeSize;
        public float maxSpawnSpeed;
        public Unity.Mathematics.Random rand;

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation, [ReadOnly]ref BeeSpawnRequest request)
        {
            float3 dir = rand.NextFloat3(-1.0f, 1.0f);
            float magnitude = sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            float3 startingVelocity = float3(0);
            if (magnitude != 0)
            {
                startingVelocity = dir / magnitude * maxSpawnSpeed;
            }

            Entity spawnedEntity;
            spawnedEntity = CommandBuffer.Instantiate(index, BeePrototypes[request.Team]);
            
            CommandBuffer.AddComponent<BeeSize>(index, spawnedEntity, new BeeSize() { Size = rand.NextFloat(minBeeSize, maxBeeSize) });
            CommandBuffer.SetComponent<Translation>(index, spawnedEntity, new Translation() { Value = translation.Value });
            CommandBuffer.AddComponent<Velocity>(index, spawnedEntity, new Velocity() { v = float3(0) });
            CommandBuffer.AddComponent<FlightTarget>(index, spawnedEntity, new FlightTarget() );

            if (request.Team == 0)
            {
                CommandBuffer.AddComponent<BeeTeam0>(index, spawnedEntity);
            }
            else
            {
                CommandBuffer.AddComponent<BeeTeam1>(index, spawnedEntity);
            }
            CommandBuffer.DestroyEntity(index, entity);
        }
    }
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new SpawnerJob();
        job.CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        job.BeePrototypes = BeePrototypes;
        job.minBeeSize = minBeeSize;
        job.maxBeeSize = maxBeeSize;
        job.maxSpawnSpeed = maxSpawnSpeed;
        job.rand = rand;
        rand.NextFloat();

        // Now that the job is set up, schedule it to be run. 
        JobHandle  jobHandle = job.Schedule(this, inputDependencies);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        BeePrototypes = new NativeArray<Entity>(2, Allocator.Persistent);
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        rand = new Unity.Mathematics.Random(3);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (BeePrototypes.IsCreated)
            BeePrototypes.Dispose();
    }
}