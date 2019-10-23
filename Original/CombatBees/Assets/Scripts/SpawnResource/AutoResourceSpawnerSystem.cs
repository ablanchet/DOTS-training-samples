using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public class AutoResourceSpawnerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
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
                    var pos = random.NextFloat3(minGridPos, maxGridPos);

                    var spannedX = (math.round(pos.x / resourceSize) * resourceSize) + (resourceSize / 2);
                    var spannedZ = (math.round(pos.z / resourceSize) * resourceSize) - (resourceSize / 2);

                    var instance = PostUpdateCommands.Instantiate(spawnerConfiguration.resourcePrefab);
                    PostUpdateCommands.SetComponent(instance, new Translation { Value = new float3(spannedX, pos.y, spannedZ) });
                    PostUpdateCommands.AddComponent(instance, new ResourceFallingTag());
                }

                PostUpdateCommands.RemoveComponent<SpawnRandomResourceOnStart>(e);
            });
    }
}