using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(BeeSpawner))]
public class ResourceFlightSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        // Entities.ForEach processes each set of ComponentData on the main thread. This is not the recommended
        // method for best performance. However, we start with it here to demonstrate the clearer separation
        // between ComponentSystem Update (logic) and ComponentData (data).
        // There is no update logic on the individual ComponentData.
        Entities.ForEach((Entity e, ref FollowEntity follow, ref Translation translation, ref NonUniformScale scale) =>
        {
            if (follow.target != Entity.Null) {
                if (EntityManager.HasComponent<Death>(follow.target)) {
                    PostUpdateCommands.RemoveComponent<FollowEntity>(e);
                    PostUpdateCommands.AddComponent<ResourceFallingTag>(e);
                } else {
                    var targetTranslation = EntityManager.GetComponentData<Translation>(follow.target);
                    var targetSize = EntityManager.GetComponentData<BeeSize>(follow.target).Size;
                    translation.Value = new float3(targetTranslation.Value.x, targetTranslation.Value.y - targetSize - scale.Value.y, targetTranslation.Value.z);
                }
            }
        });
    }
}