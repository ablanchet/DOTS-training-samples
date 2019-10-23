using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
//
//public class ResourceFallSystem : JobComponentSystem
//{
//    const float k_Gravity = -20f;
//
//    EntityQuery m_Query;
//    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
//    float m_ResourcePrefabHeight;
//
//    protected override void OnCreate()
//    {
//        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
//        m_Query = GetEntityQuery(ComponentType.ReadOnly<ResourceFallingTag>(),
//                                 ComponentType.ReadOnly<TargetCell>(),
//                                 ComponentType.ReadWrite<Translation>());
//    }
//
//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        if (m_ResourcePrefabHeight == default)
//            m_ResourcePrefabHeight = GetSingleton<ResourceSpawnerConfiguration>().resourceScale.y;
//
//        var ground = GetSingletonEntity<ResourceGroundTag>();
//        var groundY = EntityManager.GetComponentData<Translation>(ground).Value.y;
//
//        var handle = new FreeFallJob
//        {
//            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
//            dt = Time.deltaTime,
//            groundY = groundY,
//            resourcePrefabHeight = m_ResourcePrefabHeight,
//            cellComponentFromEntity = GetComponentDataFromEntity<CellComponent>()
//        }.Schedule(m_Query, inputDeps);
//
//        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
//        return handle;
//    }
//
////    [BurstCompile]
//    struct FreeFallJob : IJobForEachWithEntity_ECC<Translation, TargetCell>
//    {
//        public EntityCommandBuffer.Concurrent commandBuffer;
//
//        public float dt;
//        public float groundY;
//        public float resourcePrefabHeight;
//
//        [ReadOnly] public ComponentDataFromEntity<CellComponent> cellComponentFromEntity;
//
//        public void Execute(Entity e, int index, ref Translation t, [ReadOnly] ref TargetCell targetCell)
//        {
//            var myCell = cellComponentFromEntity[targetCell.cellEntity];
//
//            t.Value.y += k_Gravity * dt;
//
//            if (myCell.resourceCount > 0 && t.Value.y <= groundY + resourcePrefabHeight + myCell.resourceCount * resourcePrefabHeight * 2)
//            {
//                t.Value.y = groundY + resourcePrefabHeight + myCell.resourceCount * resourcePrefabHeight * 2;
//                myCell.resourceCount++;
//
//                commandBuffer.SetComponent(index, targetCell.cellEntity, new CellComponent { resourceCount = myCell.resourceCount});
//                commandBuffer.RemoveComponent<ResourceFallingTag>(index, e);
//                return;
//            }
//            else if (myCell.resourceCount == 0 && t.Value.y <= groundY + resourcePrefabHeight)
//            {
//                t.Value.y = groundY + resourcePrefabHeight;
//                myCell.resourceCount++;
//
//                commandBuffer.SetComponent(index, targetCell.cellEntity, new CellComponent { resourceCount = myCell.resourceCount });
//                commandBuffer.RemoveComponent<ResourceFallingTag>(index, e);
//            }
//        }
//    }
//}

public class ResourceFallSystem : ComponentSystem
{
    const float k_Gravity = -20f;

    float m_ResourcePrefabHeight;

    protected override void OnUpdate()
    {
        if (m_ResourcePrefabHeight == default)
            m_ResourcePrefabHeight = GetSingleton<ResourceSpawnerConfiguration>().resourceScale.y;

        var stackHeights = new Dictionary<int, int>();

        var ground = GetSingletonEntity<ResourceGroundTag>();
        var groundY = EntityManager.GetComponentData<Translation>(ground).Value.y;

        var dt = Time.deltaTime;

        Entities.ForEach((Entity e, ref ResourceFallingTag tag, ref Translation t, ref TargetCell target) =>
        {
            t.Value.y += k_Gravity * dt;

            if (!stackHeights.TryGetValue(target.cellEntity.Index, out var currentHeight))
            {
                var myCell = EntityManager.GetComponentData<CellComponent>(target.cellEntity);
                currentHeight = myCell.resourceCount;
                stackHeights.Add(target.cellEntity.Index, 0);
            }

            var expectedGroundHeight = groundY + m_ResourcePrefabHeight + currentHeight * m_ResourcePrefabHeight * 2;

            if (t.Value.y <= expectedGroundHeight)
            {
                t.Value.y = expectedGroundHeight;
                stackHeights[target.cellEntity.Index] = ++currentHeight;

                PostUpdateCommands.SetComponent(target.cellEntity, new CellComponent { resourceCount = currentHeight });
                PostUpdateCommands.RemoveComponent<ResourceFallingTag>(e);
            }
        });
    }
}