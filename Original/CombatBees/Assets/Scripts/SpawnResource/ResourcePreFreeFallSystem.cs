using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(ResourceFallSystem))]
[UpdateAfter(typeof(AutoResourceSpawnerSystem))]
public class ResourcePreFreeFallSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    EntityQuery m_Query;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_Query = GetEntityQuery(ComponentType.ReadOnly<ResourceFallingComponent>(),
                                 ComponentType.ReadOnly<Translation>(),
                                 ComponentType.Exclude<TargetCell>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var config = GetSingleton<ResourceSpawnerConfiguration>();

        var ground = GetSingleton<ResourceGround>();
        var resourcesPerRow = (int)math.round(ground.scale.x / config.resourceScale.x);

        var handle = new SetTargetCellJob
        {
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            resourceSize = config.resourceScale.x,
            groundBoundaries = ground.xzMaxBoundaries,
            resourcesPerRow = resourcesPerRow
        }.Schedule(m_Query, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

    struct SetTargetCellJob : IJobForEachWithEntity<Translation, ResourceFallingComponent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float resourceSize;
        public float2 groundBoundaries;
        public int resourcesPerRow;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation t, [ReadOnly] ref ResourceFallingComponent fallingComponent)
        {
            if (!fallingComponent.IsFalling)
                return;

            var xDistance = math.abs(t.Value.x - groundBoundaries.x);
            var zDistance = math.abs(t.Value.z - groundBoundaries.y);

            var horizontalResourceCount = xDistance <= resourceSize ? 0 : math.trunc(xDistance / resourceSize);
            var verticalResourceCount = zDistance <= resourceSize ? 0 : math.trunc(zDistance / resourceSize);

            var cellIdx = (int)(verticalResourceCount * resourcesPerRow + horizontalResourceCount);

            commandBuffer.AddComponent(index, entity, new TargetCell { cellIdx = cellIdx });
        }
    }
}