using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BeeSpawnerFromResource : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    public BeeSpawner beeSpawner;

    public int beesPerResource = 8;
    struct BeeSpawnerJob : IJobForEachWithEntity<Translation, ResourceData, ResourceFallingComponent>
    {
        public int beesPerResource;
        public BeeSpawner beeSpawner;
        public EntityCommandBuffer.Concurrent CommandBuffer;
        //public NativeQueue<float3>.ParallelWriter SpawnFXQueue;

        public ParticleSpawner particleSpawner;

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation, ref ResourceData resourceData, [ReadOnly] ref ResourceFallingComponent falling)
        {
            if (falling.IsFalling)
                return;

            if (resourceData.dying)
            {
                CommandBuffer.DestroyEntity(index, entity);
                return;
            }

            if (!resourceData.held && Mathf.Abs(translation.Value.x) > Field.size.x * .4f)
            {
                sbyte team = 0;
                if (translation.Value.x > 0f)
                {
                    team = 1;
                }

                beeSpawner.SpawnBees(CommandBuffer, index, translation.Value, beesPerResource, team);
                particleSpawner.CreateSpawnFlash(CommandBuffer, index, translation.Value, 1, new float3(0,0,0), 6f);


                //SpawnFXQueue.Enqueue(translation.Value);
                //ParticleManager.SpawnParticle(translation.Value, ParticleType.SpawnFlash, Vector3.zero, 6f, 5);
                // DeleteResource(resource);
                resourceData.dying = true; //delay destruction 1 frame, because we have scheduling issues where a bee will try to grab a resource as it is being destroyed
            }
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new BeeSpawnerJob();
        job.beesPerResource = beesPerResource;
        job.beeSpawner = beeSpawner;
        beeSpawner.AdvanceRandomizer();
        job.CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        //job.SpawnFXQueue = World.Active.GetExistingSystem<StartFxSystem>().GetSpawnQueue().AsParallelWriter();
        job.particleSpawner = World.Active.GetExistingSystem<ParticleDrawSystem>().Spawner;

        JobHandle handle = job.Schedule(this, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        EntityManager manager = World.Active.EntityManager;
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

    }
}