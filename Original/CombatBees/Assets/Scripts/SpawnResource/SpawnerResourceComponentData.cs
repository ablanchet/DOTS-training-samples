using Unity.Entities;

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