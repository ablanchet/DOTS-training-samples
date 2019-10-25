using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class BeeSpawner : ComponentSystem
{
    public float minBeeSize;
    public float maxBeeSize;
    public float maxSpawnSpeed;

    NativeArray<Entity> m_BeePrototypes;
    Random m_Rand;

    public void SetPrototypes(Entity bee0, Entity bee1)
    {
        m_BeePrototypes[0] = bee0;
        m_BeePrototypes[1] = bee1;

        EntityManager.AddComponent<BeeTeam0>(bee0);
        EntityManager.AddComponent<BeeTeam1>(bee1);

        foreach (var bee in m_BeePrototypes)
        {
            EntityManager.AddComponent<BeeSize>(bee);
            EntityManager.AddComponent<Velocity>(bee);
            EntityManager.AddComponent<FlightTarget>(bee);
            EntityManager.AddComponent<NonUniformScale>(bee);
        }
    }

    protected override void OnCreate()
    {
        m_BeePrototypes = new NativeArray<Entity>(2, Allocator.Persistent);
        m_Rand = new Random(3);
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((Entity e, ref Translation translation, ref BeeSpawnRequest request) =>
        {
            var dir = m_Rand.NextFloat3(-1.0f, 1.0f);
            var magnitude = sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            var startingVelocity = float3(0);
            if (magnitude != 0)
                startingVelocity = dir / magnitude * maxSpawnSpeed;

            var spawnedEntity = EntityManager.Instantiate(m_BeePrototypes[request.teamIdx]);
            EntityManager.SetComponentData(spawnedEntity, new BeeSize { Size = m_Rand.NextFloat(minBeeSize, maxBeeSize) });
            EntityManager.SetComponentData(spawnedEntity, new Translation { Value = translation.Value });
            EntityManager.SetComponentData(spawnedEntity, new Velocity { v = startingVelocity });

            PostUpdateCommands.DestroyEntity(e);
        });
    }

    protected override void OnDestroy()
    {
        if (m_BeePrototypes.IsCreated)
            m_BeePrototypes.Dispose();
    }
}