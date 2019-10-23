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
        m_Query = GetEntityQuery(ComponentType.ReadOnly<ResourceFallingTag>(),
                                 ComponentType.ReadOnly<Translation>(),
                                 ComponentType.Exclude<TargetCell>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var config = GetSingleton<ResourceSpawnerConfiguration>();

        var ground = GetSingleton<ResourceGround>();
        var resourcesPerRow = (int)math.round(ground.scale.x / config.resourceScale.x);

        var gridEntity = GetSingletonEntity<GridTag>();
        var indexedCells = EntityManager.GetBuffer<IndexedCell>(gridEntity).ToNativeArray(Allocator.TempJob);

        var handle = new SetTargetCellJob
        {
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            resourceSize = config.resourceScale.x,
            indexedCells = indexedCells,
            groundBoundaries = ground.xzMaxBoundaries,
            resourcesPerRow = resourcesPerRow
        }.Schedule(m_Query, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

    struct SetTargetCellJob : IJobForEachWithEntity_EC<Translation>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<IndexedCell> indexedCells;
        public float resourceSize;
        public float2 groundBoundaries;
        public int resourcesPerRow;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation t)
        {
            var xDistance = math.abs(t.Value.x - groundBoundaries.x);
            var zDistance = math.abs(t.Value.z - groundBoundaries.y);

            var horizontalResourceCount = xDistance <= resourceSize ? 0 : math.trunc(xDistance / resourceSize);
            var verticalResourceCount = zDistance <= resourceSize ? 0 : math.trunc(zDistance / resourceSize);

            var cellIdx = (int)(verticalResourceCount * resourcesPerRow + horizontalResourceCount);
            var cellEntity = indexedCells[cellIdx].cellEntity;

            commandBuffer.AddComponent(index, entity, new TargetCell { cellEntity = cellEntity });
        }
    }
}