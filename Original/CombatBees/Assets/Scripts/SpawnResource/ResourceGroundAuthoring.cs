using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ResourceGroundAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject ResourcePrefab;

    void OnDrawGizmos()
    {
        var transform = gameObject.GetComponent<Transform>();
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (ResourcePrefab == null)
            return;

        var resourceSize = ResourcePrefab.transform.localScale.x;
        var scale = dstManager.GetComponentData<NonUniformScale>(entity);
        var translation = dstManager.GetComponentData<Translation>(entity);

        dstManager.AddComponentData(entity, new ResourceGroundComponent
        {
            Dimensions = scale.Value,
            Y = translation.Value.y
        });

        var gridCounts = Vector2Int.RoundToInt(new Vector2(scale.Value.x, scale.Value.z) / resourceSize);

        var gridEntity = dstManager.CreateEntity(typeof(GridComponent), typeof(IndexedCell));
//        dstManager.AddComponentData(gridEntity, new GridComponent());

//        var cells = dstManager.AddBuffer<IndexedCell>(gridEntity);

//        cells.Reserve(gridCounts.x * gridCounts.y);
        for (var i = 0; i < gridCounts.x; i++)
        {
            for (var j = 0; j < gridCounts.y; j++)
            {
                var cellEntity = dstManager.CreateEntity(typeof(CellComponent));
//                dstManager.AddComponentData(cellEntity, new CellComponent());
                var cells = dstManager.GetBuffer<IndexedCell>(gridEntity);
                cells.Add(new IndexedCell { CellEntity = cellEntity});
            }
        }

        Debug.Log($"item size = {resourceSize}");
        Debug.Log($"grid x = {gridCounts.x}, y = {gridCounts.y}");
    }
}

public struct ResourceGroundComponent : IComponentData
{
    public float Y;
    public float3 Dimensions;
}

public struct CellComponent : IComponentData
{
    public int CellHeight;
}

public struct IndexedCell : IBufferElementData
{
    public Entity CellEntity;
}

public struct GridComponent : IComponentData
{
}