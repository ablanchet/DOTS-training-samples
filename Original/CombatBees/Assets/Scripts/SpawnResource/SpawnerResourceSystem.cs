using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class SpawnerResourceSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities
            .WithAllReadOnly<SpawnerResourceOnStartTag, SpawnerResourceComponentData, Translation, NonUniformScale>()
            .ForEach((Entity e, ref SpawnerResourceComponentData componentData, ref Translation translation, ref NonUniformScale scale) =>
        {
            var minGridPos = translation.Value - scale.Value/2;
            var maxGridPos = translation.Value + scale.Value/2;
            var random = new Random(32);

            for (int i = 0; i < componentData.startResourceCount; i++)
            {
                var pos = random.NextFloat3(minGridPos, maxGridPos);
                var instance = PostUpdateCommands.Instantiate(componentData.ResourcePrefab);
                PostUpdateCommands.SetComponent(instance, new Translation { Value = pos });
            }

            PostUpdateCommands.RemoveComponent<SpawnerResourceOnStartTag>(e);
        });
    }
}