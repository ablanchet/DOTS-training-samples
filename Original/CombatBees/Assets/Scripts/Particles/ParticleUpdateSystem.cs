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
        public float3 FieldSize;

        public void Execute(ref ParticleComponent particle, ref Translation translation, ref Velocity velocity)
        {
            particle.life -= DeltaTime / particle.lifeDuration;

            if (particle.stuck)
            {
                particle.life -= DeltaTime / particle.lifeDuration;
                return;
            }

            velocity.v += float3(0, 1, 0) * (Gravity * DeltaTime);
            translation.Value += velocity.v * DeltaTime;

            if (math.abs(translation.Value.x) > FieldSize.x * .5f)
            {
                translation.Value.x = FieldSize.x * .5f * math.sign(translation.Value.x);
                float splat = math.abs(velocity.v.x * .3f) + 1f;
                particle.size.y *= splat;
                particle.size.z *= splat;
                particle.stuck = true;
            }
            if (math.abs(translation.Value.y) > FieldSize.y * .5f)
            {
                translation.Value.y = FieldSize.y * .5f * math.sign(translation.Value.y);
                float splat = math.abs(velocity.v.y * .3f) + 1f;
                particle.size.z *= splat;
                particle.size.x *= splat;
                particle.stuck = true;
            }
            if (math.abs(translation.Value.z) > FieldSize.z * .5f)
            {
                translation.Value.z = FieldSize.z * .5f * math.sign(translation.Value.z);
                float splat = math.abs(velocity.v.z * .3f) + 1f;
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
        job.FieldSize = Field.size;
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