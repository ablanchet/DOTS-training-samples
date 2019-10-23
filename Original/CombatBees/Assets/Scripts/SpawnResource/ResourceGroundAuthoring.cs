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

        dstManager.AddComponentData(entity, new ResourceGround
        {
            scale = scale.Value,
            xzMaxBoundaries = new float2(translation.Value.x + scale.Value.x / 2, translation.Value.z + scale.Value.z / 2),
            groundHeight = translation.Value.y
        });

        var grid = Vector2Int.RoundToInt(new Vector2(scale.Value.x, scale.Value.z) / resourceSize);

        var gridEntity = dstManager.CreateEntity(typeof(GridTag), typeof(IndexedCell));
        for (var i = 0; i < grid.x; i++)
        {
            for (var j = 0; j < grid.y; j++)
            {
                var cellEntity = dstManager.CreateEntity(typeof(CellComponent));
                var cells = dstManager.GetBuffer<IndexedCell>(gridEntity);
                cells.Add(new IndexedCell { cellEntity = cellEntity});
            }
        }
    }
}

public struct ResourceGround : IComponentData
{
    public float2 xzMaxBoundaries;
    public float3 scale;
    public float groundHeight;
}

public struct GridTag : IComponentData
{
}

public struct CellComponent : IComponentData
{
    public int resourceCount;
}

public struct IndexedCell : IBufferElementData
{
    public Entity cellEntity;
}