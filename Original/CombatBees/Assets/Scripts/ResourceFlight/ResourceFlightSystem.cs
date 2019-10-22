using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(BeeSpawner))]
public class ResourceFlightSystem : ComponentSystem
{
    private EntityQuery bees;

    protected override void OnCreate() {
        bees = GetEntityQuery(
            ComponentType.ReadOnly<BeeTeam0>()
        );
    }
    protected override void OnUpdate()
    {
        // Entities.ForEach processes each set of ComponentData on the main thread. This is not the recommended
        // method for best performance. However, we start with it here to demonstrate the clearer separation
        // between ComponentSystem Update (logic) and ComponentData (data).
        // There is no update logic on the individual ComponentData.
        using (NativeArray<Entity> beesList = bees.ToEntityArray(Allocator.TempJob)) {
            Entities.ForEach((Entity e, ref FollowEntity follow, ref Translation translation, ref NonUniformScale scale) =>
            {
                if (follow.target != Entity.Null) {
                    var targetTranslation = EntityManager.GetComponentData<Translation>(follow.target);
                    var targetSize = EntityManager.GetComponentData<BeeSize>(follow.target).Size;
                    translation.Value = new float3(targetTranslation.Value.x, targetTranslation.Value.y - targetSize - scale.Value.y, targetTranslation.Value.z);
                } else {
                    PostUpdateCommands.SetComponent<FollowEntity>(e, new FollowEntity { target = beesList[0] });
                }
            });
        }
    }
}