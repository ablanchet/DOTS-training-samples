using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class ParticleUpdateSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    [BurstCompile]
    struct ParticleUpdateSystemJob : IJobForEach<ParticleComponent, Translation, Velocity>
    {
        public float DeltaTime;
        public float Gravity;

        public void Execute(ref ParticleComponent particle, ref Translation translation, ref Velocity velocity)
        {
            particle.life -= DeltaTime / particle.lifeDuration;

            if (particle.stuck)
                return;

            velocity.v += float3(0, 1, 0) * (Gravity * DeltaTime);
            translation.Value += velocity.v * DeltaTime;

            if (System.Math.Abs(translation.Value.x) > Field.size.x * .5f)
            {
                translation.Value.x = Field.size.x * .5f * Mathf.Sign(translation.Value.x);
                float splat = Mathf.Abs(velocity.v.x * .3f) + 1f;
                particle.size.y *= splat;
                particle.size.z *= splat;
                particle.stuck = true;
            }
            if (System.Math.Abs(translation.Value.y) > Field.size.y * .5f)
            {
                translation.Value.y = Field.size.y * .5f * Mathf.Sign(translation.Value.y);
                float splat = Mathf.Abs(velocity.v.y * .3f) + 1f;
                particle.size.z *= splat;
                particle.size.x *= splat;
                particle.stuck = true;
            }
            if (System.Math.Abs(translation.Value.z) > Field.size.z * .5f)
            {
                translation.Value.z = Field.size.z * .5f * Mathf.Sign(translation.Value.z);
                float splat = Mathf.Abs(velocity.v.z * .3f) + 1f;
                particle.size.x *= splat;
                particle.size.y *= splat;
                particle.stuck = true;
            }
        }
    }
    struct ParticlecleanupJob : IJobForEachWithEntity<ParticleComponent>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public void Execute(Entity entity, int index, ref ParticleComponent particle)
        {
            if (particle.life < 0)
                CommandBuffer.DestroyEntity(index, entity);
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new ParticleUpdateSystemJob();
        job.DeltaTime = Time.deltaTime;
        job.Gravity = Field.gravity;
        JobHandle updateHandle = job.Schedule(this, inputDependencies);

        var cleanupJob = new ParticlecleanupJob();
        cleanupJob.CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        JobHandle cleanupHandle = cleanupJob.Schedule(this, updateHandle);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(cleanupHandle);

        return cleanupHandle;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
}