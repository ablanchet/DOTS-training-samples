using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ResourceFallSystem : ComponentSystem
{
    const float k_Gravity = -20f;

    float m_ResourcePrefabHeight;

    protected override void OnUpdate()
    {
        if (m_ResourcePrefabHeight == default)
            m_ResourcePrefabHeight = GetSingleton<ResourceSpawnerConfiguration>().resourceScale.y;

        var stackHeights = new Dictionary<int, int>();

        var ground = GetSingleton<ResourceGround>();

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

            var expectedGroundHeight = ground.groundHeight + m_ResourcePrefabHeight + currentHeight * m_ResourcePrefabHeight * 2;

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