using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ResourceFallSystem : ComponentSystem
{
    const float k_Gravity = -20f;

    float m_ResourcePrefabHeight;
    ResourceGround m_Ground;

    protected override void OnUpdate()
    {
        if (m_ResourcePrefabHeight == default)
        {
            m_ResourcePrefabHeight = GetSingleton<ResourceSpawnerConfiguration>().resourceScale.y;
            m_Ground = GetSingleton<ResourceGround>();
        }

        var dt = Time.deltaTime;

        Entities.ForEach((Entity e, ref ResourceFallingTag tag, ref Translation t, ref TargetCell target) =>
        {
            t.Value.y += k_Gravity * dt;

            var expectedGroundHeight = m_Ground.groundHeight + m_ResourcePrefabHeight + GridHelper.StackHeights[target.cellIdx] * m_ResourcePrefabHeight * 2;

            if (t.Value.y <= expectedGroundHeight)
            {
                t.Value.y = expectedGroundHeight;
                var height = GridHelper.StackHeights[target.cellIdx];
                GridHelper.StackHeights[target.cellIdx] = ++height;

                PostUpdateCommands.RemoveComponent<ResourceFallingTag>(e);
            }
        });
    }
}