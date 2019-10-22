using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class ResourceFreeFallSystem : JobComponentSystem
{
    const float k_Gravity = -20f;
    const float k_ResourceHeight = 0.375f;
    const float k_ResourceRadius = 0.075f;

    EntityQuery m_Query;
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_Query = GetEntityQuery(ComponentType.ReadOnly<ResourceFreeFallTag>(),
                                 ComponentType.ReadOnly<ResourceComponent>(),
                                 ComponentType.ReadWrite<Translation>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ground = GetSingleton<ResourceGroundComponent>();

        var handle = new FreeFallJob
        {
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            dt = Time.deltaTime,
            groundY = ground.Y,
            CellComponentFromEntity = GetComponentDataFromEntity<CellComponent>()
        }.Schedule(m_Query, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

    struct FreeFallJob : IJobForEachWithEntity_ECC<Translation, ResourceComponent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float dt;
        public float groundY;

        [ReadOnly] public ComponentDataFromEntity<CellComponent> CellComponentFromEntity;

        public void Execute(Entity e, int index, ref Translation t, [ReadOnly] ref ResourceComponent resourceComponent)
        {
            var myCell = CellComponentFromEntity[resourceComponent.CellEntity];

            t.Value.y += k_Gravity * dt;

            if (myCell.CellHeight > 0 && t.Value.y <= groundY + k_ResourceHeight + myCell.CellHeight * k_ResourceHeight)
            {
                t.Value.y = groundY +  k_ResourceHeight + myCell.CellHeight * k_ResourceHeight;

                myCell.CellHeight++;

                commandBuffer.SetComponent(index, resourceComponent.CellEntity, new CellComponent() { CellHeight = myCell.CellHeight });
                commandBuffer.RemoveComponent<ResourceFreeFallTag>(index, e);
                return;
            }

            if (t.Value.y <= groundY + k_ResourceHeight)
            {
                t.Value.y = groundY + k_ResourceHeight;
                myCell.CellHeight++;

                commandBuffer.SetComponent(index, resourceComponent.CellEntity, new CellComponent() { CellHeight = myCell.CellHeight });
                commandBuffer.RemoveComponent<ResourceFreeFallTag>(index, e);
            }
        }
    }
}