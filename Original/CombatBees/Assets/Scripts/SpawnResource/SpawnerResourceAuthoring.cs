using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

public class SpawnerResourceAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject ResourcePrefab;
    public float resourceSize;
    public float snapStiffness;
    public float carryStiffness;
    public float spawnRate = .1f;
    [Space(10)]
    public int startResourceCount;

    private void OnDrawGizmos()
    {
        var transform = gameObject.GetComponent<Transform>();
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var component = new SpawnerResourceComponentData
        {
            ResourcePrefab = conversionSystem.GetPrimaryEntity(ResourcePrefab),
            carryStiffness = carryStiffness,
            resourceSize = resourceSize,
            snapStiffness = snapStiffness,
            spawnRate = spawnRate,
            startResourceCount = startResourceCount
        };

        dstManager.AddComponentData(entity, component);
        dstManager.AddComponent<SpawnerResourceOnStartTag>(entity);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (ResourcePrefab != null)
        {
            referencedPrefabs.Add(ResourcePrefab);
        }
    }
}

public struct SpawnerResourceComponentData : IComponentData
{
    public Entity ResourcePrefab;
    public float resourceSize;
    public float snapStiffness;
    public float carryStiffness;
    public float spawnRate;
    public int startResourceCount;
}

public struct SpawnerResourceOnStartTag : IComponentData
{

}

public struct ResourceFreeFallTag : IComponentData
{

}