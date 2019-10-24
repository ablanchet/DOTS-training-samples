using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ResourceFallSystem : ComponentSystem
{
    const float k_Gravity = -20f;

    float m_ResourcePrefabHeight;
    ResourceGround m_Ground;

    public NativeArray<int> StackHeights;

    protected override void OnUpdate()
    {
        if (m_ResourcePrefabHeight == default)
        {
            m_ResourcePrefabHeight = GetSingleton<ResourceSpawnerConfiguration>().resourceScale.y;
            m_Ground = GetSingleton<ResourceGround>();
        }

        var dt = Time.deltaTime;

        Entities.ForEach((Entity e, ref ResourceFallingTag tag, ref Translation t, ref TargetCell target, ref ResourceData resData) =>
        {
            t.Value.x += resData.velocity.x * dt;

            t.Value.y += k_Gravity * dt;

            var expectedGroundHeight = m_Ground.groundHeight + m_ResourcePrefabHeight + StackHeights[target.cellIdx] * m_ResourcePrefabHeight * 2;

            if (t.Value.y <= expectedGroundHeight)
            {
                t.Value.y = expectedGroundHeight;
                var height = StackHeights[target.cellIdx];
                StackHeights[target.cellIdx] = ++height;

                PostUpdateCommands.RemoveComponent<ResourceFallingTag>(e);
            }
        });
    }

    protected override void OnDestroy()
    {
        StackHeights.Dispose();
    }
}