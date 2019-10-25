using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;

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
                    PostUpdateCommands.SetComponent<FollowEntity>(e, new FollowEntity());
                    PostUpdateCommands.SetComponent<ResourceFallingComponent>(e, new ResourceFallingComponent(){IsFalling = true});
                } else {
                    var targetTranslation = EntityManager.GetComponentData<Translation>(follow.target);
                    var targetSize = EntityManager.GetComponentData<BeeSize>(follow.target).Size;
                    var targetPos = targetTranslation.Value - new float3(0,1,0) * (scale.Value.y + targetSize) * .5f;
                    var resourcePos = math.lerp(translation.Value, targetPos, 15 * Time.deltaTime);

                    translation.Value = targetPos;
                }
            }
        });
    }
}