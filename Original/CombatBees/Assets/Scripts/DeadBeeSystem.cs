using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(VelocityBasedMovement)), UpdateAfter(typeof(BeeBehaviour))]
[ExecuteAlways]
public class DeadBeeSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    Unity.Mathematics.Random rand;

    struct ProcessDeadJob : IJobForEachWithEntity<Death, Velocity, BeeSize, FlightTarget, Translation>
    {
        public float deltaTime;
        public float gravity;
        public float floorY;
        //public NativeQueue<BloodInfo>.ParallelWriter BloodEffectsQueue;
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public Unity.Mathematics.Random rand;
        public ParticleSpawner particleSpawner;

        public void Execute(Entity entity, int index, ref Death death, ref Velocity velocity, ref BeeSize beeSize, ref FlightTarget flightTarget, [ReadOnly] ref Translation translation)
        {
            if (death.Dying) {
                if (flightTarget.entity != Entity.Null && flightTarget.isResource)
                {
                    flightTarget.PendingAction = FlightTarget.Action.DropResource; //try to ensure we don't get floating entities
                }

                if (!death.FirstUpdateDone)
                {
                    velocity.v *= 0.5f;
                    particleSpawner.CreateBlood(CommandBuffer, index, translation.Value, 6, velocity.v, 2f);
                    //BloodEffectsQueue.Enqueue(new BloodInfo() { Position = translation.Value, Velocity = velocity.v, VelocityJitter = 2f, count = 6 });
                    death.FirstUpdateDone = true;
                    beeSize.Faded = true;
                }

                if (rand.NextFloat() < (death.DeathTimer - .5f) * .5f)
                {
                    particleSpawner.CreateBlood(CommandBuffer, index, translation.Value, 1, float3(Vector3.zero), 6f);
                    //BloodEffectsQueue.Enqueue(new BloodInfo() { Position = translation.Value, Velocity = Vector3.zero, VelocityJitter = 6f, count = 1 });
                }

                velocity.v.y += gravity * deltaTime;

                if (translation.Value.y <= floorY)
                {
                    velocity.v = float3(0);
                }

                death.DeathTimer -= deltaTime;
                if (death.DeathTimer < 0f)
                {
                    CommandBuffer.DestroyEntity(index, entity);
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.fixedDeltaTime;
        ParticleSpawner particleSpawner = World.GetExistingSystem<ParticleDrawSystem>().Spawner;

        //the already dead (bees that have the Death tag)
        var processDeadJob = new ProcessDeadJob();
        processDeadJob.CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        processDeadJob.particleSpawner = particleSpawner;
        processDeadJob.deltaTime = deltaTime;
        processDeadJob.gravity = Field.gravity;
        processDeadJob.floorY = (-Field.size.y / 2);
        processDeadJob.rand = rand;
        rand.NextFloat();
        JobHandle processDeadHandle = processDeadJob.Schedule(this, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(processDeadHandle);

        return processDeadHandle;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        rand = new Unity.Mathematics.Random(3);
    }
}