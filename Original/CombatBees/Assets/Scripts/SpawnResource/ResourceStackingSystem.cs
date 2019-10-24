using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ResourceStackingSystem : ComponentSystem
{
    const float k_Gravity = -20f;

    float m_ResourcePrefabHeight;
    ResourceGround m_Ground;

    public NativeArray<short> StackHeights;

    protected override void OnUpdate()
    {
        if (m_ResourcePrefabHeight == default)
        {
            m_ResourcePrefabHeight = GetSingleton<ResourceSpawnerConfiguration>().resourceScale.y;
            m_Ground = GetSingleton<ResourceGround>();
        }

        var dt = Time.deltaTime;

        Entities.ForEach((Entity e, ref ResourceFallingTag tag, ref Translation t, ref TargetCell target, ref ResourceHeight resourceHeight) =>
        {
            t.Value.y += k_Gravity * dt;

            var expectedGroundHeight = m_Ground.groundHeight + m_ResourcePrefabHeight + StackHeights[target.cellIdx] * m_ResourcePrefabHeight * 2;

            if (t.Value.y <= expectedGroundHeight)
            {
                t.Value.y = expectedGroundHeight;
                var height = StackHeights[target.cellIdx];
                resourceHeight.value = height;
                StackHeights[target.cellIdx] = ++height;

                PostUpdateCommands.RemoveComponent<ResourceFallingTag>(e);
            }
        });
    }

    public bool IsTopResource(TargetCell targetCell, ResourceHeight resourceHeight)
    {
        return StackHeights[targetCell.cellIdx] == resourceHeight.value;
    }

    public void PickTopResource(int cellIdx)
    {
        var height = StackHeights[cellIdx];
        StackHeights[cellIdx] = --height < 0 ? (short)0 : height;
    }

    protected override void OnDestroy()
    {
        StackHeights.Dispose();
    }
}