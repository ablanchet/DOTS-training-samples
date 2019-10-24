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
                var minGridPos = translation.Value - spawnerArea.Value / 2;
                var maxGridPos = translation.Value + spawnerArea.Value / 2;
                var random = new Random(32);

                for (var i = 0; i < spawnOnStart.startResourceCount; i++)
                {
                    var pos = GridHelper.SnapPointToGroundGrid(random.NextFloat3(minGridPos, maxGridPos));
                    var stackIdx = GridHelper.GetIndexOf(pos.xz);

                    var instance = PostUpdateCommands.Instantiate(spawnerConfiguration.resourcePrefab);
                    PostUpdateCommands.AddComponent(instance, new FallingResource());
                    PostUpdateCommands.AddComponent(instance, new TargetStack { stackIdx = stackIdx });
                    PostUpdateCommands.SetComponent(instance, new Translation { Value = pos });
                }

                PostUpdateCommands.RemoveComponent<SpawnRandomResourceOnStart>(e);
            });
    }
}