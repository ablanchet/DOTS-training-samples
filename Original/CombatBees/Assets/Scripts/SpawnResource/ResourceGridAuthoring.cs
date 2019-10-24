using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ResourceGridAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
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

        var resourceScale = ResourcePrefab.transform.localScale;
        var scale = dstManager.GetComponentData<NonUniformScale>(entity);
        var translation = dstManager.GetComponentData<Translation>(entity);

        var gridDimensions = Vector2Int.RoundToInt(new Vector2(scale.Value.x, scale.Value.z) / resourceScale.x);
        GridHelper.resourceSize = resourceScale.x;
        GridHelper.resourceHeight = resourceScale.y * 2; // because cylinder mesh is 2 unit tall
        GridHelper.resourceHeightScale = resourceScale.y;
        GridHelper.gridDimensions = new float2(gridDimensions.x, gridDimensions.y);
        GridHelper.gridBoundaries = new float2(translation.Value.x + scale.Value.x / 2, translation.Value.z + scale.Value.z / 2);
        GridHelper.gridHeight = translation.Value.y;
        GridHelper.stackHeights = new NativeArray<short>(gridDimensions.x * gridDimensions.y, Allocator.Persistent);
        GridHelper.stackReferences = new NativeArray<Entity>(gridDimensions.x * gridDimensions.y, Allocator.Persistent);

        dstManager.AddComponentData(entity, new SpawnGrid { stackPrefab = conversionSystem.GetPrimaryEntity(ResourcePrefab) });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (ResourcePrefab != null)
            referencedPrefabs.Add(ResourcePrefab);
    }
}

public struct SpawnGrid : IComponentData
{
    public Entity stackPrefab;
}

public struct Stack : IComponentData
{
    public int index;
}

public struct FallingResource : IComponentData
{
}

public struct TargetStack : IComponentData
{
    public int stackIdx;
}