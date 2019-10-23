using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class ResourceFallSystem : JobComponentSystem
{
    const float k_Gravity = -20f;

    EntityQuery m_Query;
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    float m_ResourcePrefabHeight;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_Query = GetEntityQuery(ComponentType.ReadOnly<ResourceFallingTag>(),
                                 ComponentType.ReadOnly<TargetCell>(),
                                 ComponentType.ReadWrite<Translation>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_ResourcePrefabHeight == default)
            m_ResourcePrefabHeight = GetSingleton<ResourceSpawnerConfiguration>().resourceScale.y;

        var ground = GetSingletonEntity<ResourceGroundTag>();
        var groundY = EntityManager.GetComponentData<Translation>(ground).Value.y;

        var handle = new FreeFallJob
        {
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            dt = Time.deltaTime,
            groundY = groundY,
            resourcePrefabHeight = m_ResourcePrefabHeight,
            cellComponentFromEntity = GetComponentDataFromEntity<CellComponent>()
        }.Schedule(m_Query, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

//    [BurstCompile]
    struct FreeFallJob : IJobForEachWithEntity_ECC<Translation, TargetCell>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;

        public float dt;
        public float groundY;
        public float resourcePrefabHeight;

        [ReadOnly] public ComponentDataFromEntity<CellComponent> cellComponentFromEntity;

        public void Execute(Entity e, int index, ref Translation t, [ReadOnly] ref TargetCell targetCell)
        {
            var myCell = cellComponentFromEntity[targetCell.cellEntity];

            t.Value.y += k_Gravity * dt;

            if (myCell.resourceCount > 0 && t.Value.y <= groundY +  + myCell.resourceCount * resourcePrefabHeight)
            {
                t.Value.y = groundY +  resourcePrefabHeight + myCell.resourceCount * resourcePrefabHeight;

                commandBuffer.SetComponent(index, targetCell.cellEntity, new CellComponent { resourceCount = myCell.resourceCount + 1 });
                commandBuffer.RemoveComponent<ResourceFallingTag>(index, e);
                return;
            }

            if (t.Value.y <= groundY + resourcePrefabHeight)
            {
                t.Value.y = groundY + resourcePrefabHeight;
                myCell.resourceCount++;

                commandBuffer.SetComponent(index, targetCell.cellEntity, new CellComponent { resourceCount = myCell.resourceCount + 1 });
                commandBuffer.RemoveComponent<ResourceFallingTag>(index, e);
            }
        }
    }
}