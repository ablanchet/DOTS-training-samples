using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class ResourceFreeFallSystem : JobComponentSystem
{
    const float k_Gravity = -20f;

    EntityQuery m_Query;
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_Query = GetEntityQuery(ComponentType.ReadOnly<ResourceFreeFallTag>(), ComponentType.ReadWrite<Translation>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ground = GetSingleton<ResourceGroundComponent>();

        var handle = new FreeFallJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            dt = Time.deltaTime,
            groundY = ground.Y,
            gravity = k_Gravity
        }.Schedule(m_Query, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

    struct FreeFallJob : IJobForEachWithEntity_EC<Translation>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public float dt;
        public float groundY;
        public float gravity;

        public void Execute(Entity e, int index, ref Translation t)
        {
            t.Value.y += k_Gravity * dt;
            if (t.Value.y <= groundY)
            {
                t.Value.y = groundY;
                CommandBuffer.RemoveComponent<ResourceFreeFallTag>(index, e);
            }
        }
    }
}