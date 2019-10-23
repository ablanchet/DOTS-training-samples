using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public class AutoResourceSpawnerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var ground = GetSingletonEntity<ResourceGroundTag>();
        var groundTranslation = EntityManager.GetComponentData<Translation>(ground).Value;
        var groundScale = EntityManager.GetComponentData<NonUniformScale>(ground).Value;
        var groundBoundaries = new float2(groundTranslation.x + groundScale.x / 2, groundTranslation.z + groundScale.z / 2);

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
                    var pos = SnapPointToGroundGrid(groundBoundaries, resourceSize, random.NextFloat3(minGridPos, maxGridPos));

                    var instance = PostUpdateCommands.Instantiate(spawnerConfiguration.resourcePrefab);
                    PostUpdateCommands.SetComponent(instance, new Translation { Value = pos });
                    PostUpdateCommands.AddComponent(instance, new ResourceFallingTag());
                    PostUpdateCommands.AddComponent(instance, new ResourceData { held = false });
                }

                PostUpdateCommands.RemoveComponent<SpawnRandomResourceOnStart>(e);
            });
    }

    public static float3 SnapPointToGroundGrid(float2 groundBoundaries, float cellSize, float3 point)
    {
        var xDistance = math.abs(point.x - groundBoundaries.x);
        var zDistance = math.abs(point.z - groundBoundaries.y);
        var horizontalResourceCount = math.trunc(xDistance / cellSize);
        var verticalResourceCount = math.trunc(zDistance / cellSize);
        var x = groundBoundaries.x - horizontalResourceCount * cellSize;
        var z = groundBoundaries.y - verticalResourceCount * cellSize;

        return new float3(x, point.y, z);
    }
}