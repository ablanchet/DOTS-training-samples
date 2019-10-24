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

        dstManager.AddComponentData(entity, new ResourceGround
        {
            scale = scale.Value,
            xzMaxBoundaries = new float2(translation.Value.x + scale.Value.x / 2, translation.Value.z + scale.Value.z / 2),
            groundHeight = translation.Value.y
        });

        var grid = Vector2Int.RoundToInt(new Vector2(scale.Value.x, scale.Value.z) / resourceSize);

        dstManager.World.GetOrCreateSystem<ResourceFallSystem>().StackHeights = new NativeArray<int>(grid.x * grid.y, Allocator.Persistent);
    }
}

public struct ResourceGround : IComponentData
{
    public float2 xzMaxBoundaries;
    public float3 scale;
    public float groundHeight;
}