using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Transforms;

public class ResourceSpawnerAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject ResourcePrefab;
    public float SpawnRate;
    [Space(10)]
    public int startResourceCount;

    private void OnDrawGizmos()
    {
        var transform = gameObject.GetComponent<Transform>();
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (ResourcePrefab == null)
            return;

        dstManager.AddComponentData(entity, new ResourceSpawnerConfiguration
        {
            resourcePrefab = conversionSystem.GetPrimaryEntity(ResourcePrefab),
            spawnRate = SpawnRate
        });
        dstManager.AddComponentData(entity, new SpawnRandomResourceOnStart
        {
            startResourceCount = startResourceCount
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (ResourcePrefab != null)
            referencedPrefabs.Add(ResourcePrefab);
    }
}

public struct ResourceSpawnerConfiguration : IComponentData
{
    public Entity resourcePrefab;
    public float spawnRate;
}

public struct SpawnRandomResourceOnStart : IComponentData
{
    public int startResourceCount;
}