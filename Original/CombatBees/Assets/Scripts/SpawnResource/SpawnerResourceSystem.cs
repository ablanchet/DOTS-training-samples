using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class SpawnerResourceSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var gridEntity = GetSingletonEntity<GridComponent>();
        var indexedCells = EntityManager.GetBuffer<IndexedCell>(gridEntity);

        Entities
            .WithAllReadOnly<SpawnerResourceOnStartTag, SpawnerResourceComponentData, Translation, NonUniformScale>()
            .ForEach((Entity e, ref SpawnerResourceComponentData componentData, ref Translation translation, ref NonUniformScale gridScale) =>
            {
                var resourcePrefab = componentData.ResourcePrefab;
                var resourceSize = EntityManager.GetComponentData<NonUniformScale>(resourcePrefab).Value.x;
                var minGridPos = translation.Value - gridScale.Value / 2;
                var maxGridPos = translation.Value + gridScale.Value / 2;
                var random = new Random(32);

                for (var i = 0; i < componentData.startResourceCount; i++)
                {
//                    var pos = random.NextFloat3(minGridPos, maxGridPos);
                    var pos = new float3(5, i+5, 5);

                    var spannedX = (math.round(pos.x / resourceSize) * resourceSize) + (resourceSize / 2);
                    var spannedZ = (math.round(pos.z / resourceSize) * resourceSize) - (resourceSize / 2);

                    var xDistance = math.abs(spannedX - maxGridPos.x);
                    var zDistance = math.abs(spannedZ - maxGridPos.z);

                    var cellIdx = (int) (math.round(xDistance / resourceSize) * math.round(zDistance / resourceSize));
                    var cellEntity = indexedCells[cellIdx].CellEntity;

                    var instance = PostUpdateCommands.Instantiate(componentData.ResourcePrefab);
                    PostUpdateCommands.SetComponent(instance, new Translation { Value = new float3(spannedX, pos.y, spannedZ) });
                    PostUpdateCommands.AddComponent(instance, new ResourceFreeFallTag());
                    PostUpdateCommands.AddComponent(instance, new ResourceComponent()
                    {
                        CellEntity = cellEntity
                    });
                }

                PostUpdateCommands.RemoveComponent<SpawnerResourceOnStartTag>(e);
            });
    }
}

public struct ResourceComponent : IComponentData
{
    public Entity CellEntity;
}