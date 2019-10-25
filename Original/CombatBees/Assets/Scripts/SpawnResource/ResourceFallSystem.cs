using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class ResourceFallSystem : JobComponentSystem
{
    const float k_Gravity = -20f;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new FallingResourceJob
        {
            dt = Time.deltaTime
        }.Schedule(this, inputDeps);
    }

    [BurstCompile]
    [RequireComponentTag(typeof(FallingResource))]
    struct FallingResourceJob : IJobForEach_C<Translation>
    {
        public float dt;

        public void Execute(ref Translation t)
        {
            t.Value.y += k_Gravity * dt;
        }
    }

    protected override void OnDestroy()
    {
        GridHelper.stackHeights.Dispose();
        GridHelper.stackReferences.Dispose();
    }
}

[UpdateAfter(typeof(ResourceFallSystem))]
public class ResourceStackSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var gridHeight = GridHelper.gridHeight;
        var stackHeights = GridHelper.stackHeights;
        var stackReferences = GridHelper.stackReferences;
        var stackPrefab = GetSingleton<SpawnGrid>().stackPrefab;

        Entities.ForEach((Entity e, ref Translation t, ref TargetStack targetStack) =>
        {
            var stackHeight = stackHeights[targetStack.stackIdx];
            var expectedGroundHeight = gridHeight + stackHeight * GridHelper.resourceHeight;

            if (t.Value.y - (GridHelper.resourceHeight / 2) <= expectedGroundHeight)
            {
                stackHeights[targetStack.stackIdx] = ++stackHeight;
                var stackReference = stackReferences[targetStack.stackIdx];
                if (stackReference == Entity.Null)
                {
                    stackReference = PostUpdateCommands.Instantiate(stackPrefab);
                    stackReferences[targetStack.stackIdx] = stackReference;
                    PostUpdateCommands.AddComponent(stackReference, new Stack { index = targetStack.stackIdx });
                    PostUpdateCommands.SetComponent(stackReference, t);
                }

                PostUpdateCommands.DestroyEntity(e);
            }
        });
    }
}

[UpdateAfter(typeof(ResourceStackSystem))]
public class ResizeStackSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new ResizeStackJob
        {
            resourceHeight = GridHelper.resourceHeight,
            resourceHeightScale = GridHelper.resourceHeightScale,
            gridHeight = GridHelper.gridHeight,
            stackHeights = GridHelper.stackHeights
        }.Schedule(this, inputDeps);
    }

    [BurstCompile]
    struct ResizeStackJob : IJobForEach_CCC<Stack, NonUniformScale, Translation>
    {
        [ReadOnly] public NativeArray<short> stackHeights;
        public float resourceHeight;
        public float resourceHeightScale;
        public float gridHeight;

        public void Execute([ReadOnly] ref Stack stack, ref NonUniformScale s, ref Translation t)
        {
            s.Value.y = stackHeights[stack.index] * resourceHeightScale;
            t.Value.y = gridHeight + (stackHeights[stack.index] * resourceHeight) / 2;
        }
    }
}