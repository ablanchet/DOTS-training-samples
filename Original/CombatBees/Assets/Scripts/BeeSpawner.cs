using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class BeeSpawner : ComponentSystem
{
    NativeArray<Entity> BeePrototypes;
    public float minBeeSize;
    public float maxBeeSize;
    public float maxSpawnSpeed;
    Random rand;

    public void SetPrototypes(Entity Bee0, Entity Bee1)
    {
        BeePrototypes[0] = Bee0;
        BeePrototypes[1] = Bee1;

        EntityManager.AddComponent<BeeTeam0>(Bee0);
        EntityManager.AddComponent<BeeTeam1>(Bee1);

        foreach (var bee in BeePrototypes)
        {
            EntityManager.AddComponent<BeeSize>(bee);
            EntityManager.AddComponent<Velocity>(bee);
//            EntityManager.AddComponent<FlightTarget>(bee);
            EntityManager.AddComponent<NonUniformScale>(bee);
        }

    }

    protected override void OnCreate()
    {
        base.OnCreate();
        BeePrototypes = new NativeArray<Entity>(2, Allocator.Persistent);
        rand = new Random(3);
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref Translation translation, ref BeeSpawnRequest request) =>
        {
            var dir = rand.NextFloat3(-1.0f, 1.0f);
            var magnitude = sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            var startingVelocity = float3(0);
            if (magnitude != 0)
            {
                startingVelocity = dir / magnitude * maxSpawnSpeed;
            }

            var spawnedEntity = EntityManager.Instantiate(BeePrototypes[request.Team]);
            EntityManager.SetComponentData(spawnedEntity, new BeeSize { Size = rand.NextFloat(minBeeSize, maxBeeSize) });
            EntityManager.SetComponentData(spawnedEntity, new Translation { Value = translation.Value });

            EntityManager.SetComponentData(spawnedEntity, new Velocity { v = startingVelocity });
        });

        EntityManager.DestroyEntity(GetEntityQuery(typeof(BeeSpawnRequest)));
    }

    protected override void OnDestroy()
    {
        if (BeePrototypes.IsCreated)
            BeePrototypes.Dispose();
    }
}