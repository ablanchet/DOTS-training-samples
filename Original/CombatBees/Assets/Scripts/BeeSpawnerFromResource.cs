using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BeeSpawnerFromResource : ComponentSystem
{
    public int beesPerResource = 8;

    protected override void OnUpdate()
    {
        EntityManager manager = World.Active.EntityManager;
        BeeSpawner spawner = World.Active.GetExistingSystem<BeeSpawner>();

        EntityArchetype spawnRequest = manager.CreateArchetype(new ComponentType[] {typeof(Translation), typeof(BeeSpawnRequest)});

        Entities.ForEach((Entity e, ref Translation t, ref ResourceData resourceData) =>
        {
            if (resourceData.dying)
            {
                manager.DestroyEntity(e);
            }
            if (!resourceData.held && Mathf.Abs(t.Value.x) > Field.size.x * .4f && !manager.HasComponent<ResourceFallingTag>(e))
            {
                sbyte team = 0;
                if (t.Value.x > 0f)
                {
                    team = 1;
                }

                for (int j = 0; j < beesPerResource; j++)
                {
                    var request = manager.CreateEntity(spawnRequest);
                    manager.SetComponentData<BeeSpawnRequest>(request, new BeeSpawnRequest { Team = team });
                    manager.SetComponentData<Translation>(request, new Translation { Value = t.Value });
                }

                ParticleManager.SpawnParticle(t.Value, ParticleType.SpawnFlash, Vector3.zero, 6f, 5);
                // DeleteResource(resource);
                resourceData.dying = true; //delay destruction 1 frame, because we have scheduling issues where a bee will try to grab a resource as it is being destroyed
            }
        });
    }
}