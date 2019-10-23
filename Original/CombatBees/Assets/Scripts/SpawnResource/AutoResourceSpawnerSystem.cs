using Unity.Entities;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public class AutoResourceSpawnerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var xzMaxBoundaries = GetSingleton<ResourceGround>().xzMaxBoundaries;

        Entities
            .WithAllReadOnly<SpawnRandomResourceOnStart, ResourceSpawnerConfiguration, Translation, NonUniformScale>()
            .ForEach((Entity e, ref SpawnRandomResourceOnStart spawnOnStart, ref ResourceSpawnerConfiguration spawnerConfiguration, ref Translation translation, ref NonUniformScale spawnerArea) =>
            {
                var resourceSize = spawnerConfiguration.resourceScale.x;
                var minGridPos = translation.Value - spawnerArea.Value / 2;
                var maxGridPos = translation.Value + spawnerArea.Value / 2;
                var random = new Random(32);

                for (var i = 0; i < spawnOnStart.startResourceCount; i++)
                {
                    var pos = Grid.SnapPointToGroundGrid(xzMaxBoundaries, resourceSize, random.NextFloat3(minGridPos, maxGridPos));

                    var instance = PostUpdateCommands.Instantiate(spawnerConfiguration.resourcePrefab);
                    PostUpdateCommands.SetComponent(instance, new Translation { Value = pos });
                    PostUpdateCommands.AddComponent(instance, new ResourceFallingTag());
                    PostUpdateCommands.AddComponent(instance, new ResourceData { held = false });
                }

                PostUpdateCommands.RemoveComponent<SpawnRandomResourceOnStart>(e);
            });
    }
}