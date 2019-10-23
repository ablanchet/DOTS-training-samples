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

        var ground = GetSingletonEntity<ResourceGroundTag>();
        var groundTranslation = EntityManager.GetComponentData<Translation>(ground).Value;
        var groundScale = EntityManager.GetComponentData<NonUniformScale>(ground).Value;
        var groundBoundaries = new float2(groundTranslation.x + groundScale.x / 2, groundTranslation.z + groundScale.z / 2);
        var resourcesPerRow = (int)math.round(groundScale.x / config.resourceScale.x);

        var gridEntity = GetSingletonEntity<GridTag>();
        var indexedCells = EntityManager.GetBuffer<IndexedCell>(gridEntity).ToNativeArray(Allocator.TempJob);

        var handle = new SetTargetCellJob
        {
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            resourceSize = config.resourceScale.x,
            indexedCells = indexedCells,
            groundBoundaries = groundBoundaries,
            resourcesPerRow = resourcesPerRow
        }.Schedule(m_Query, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(handle);
        return handle;
    }

//    [BurstCompile]
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

            var horizontalResourceCount = xDistance <= resourceSize ? 0 : math.round(xDistance / resourceSize) - 1;
            var verticalResourceCount = zDistance <= resourceSize ? 0 : math.round(zDistance / resourceSize) - 1;
            var cellIdx = (int)(verticalResourceCount * resourcesPerRow + horizontalResourceCount);
            var cellEntity = indexedCells[cellIdx].cellEntity;

            commandBuffer.AddComponent(index, entity, new TargetCell { cellEntity = cellEntity });
        }
    }
}