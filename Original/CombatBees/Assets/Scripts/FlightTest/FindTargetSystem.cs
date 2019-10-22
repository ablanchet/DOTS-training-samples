using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[UpdateAfter(typeof(SpawnerResourceSystem))]
public class FindTargetSystem : ComponentSystem
{
    private EntityQuery resources;

    protected override void OnCreate() {
        resources = GetEntityQuery(
            ComponentType.ReadOnly<ResourceData>()
        );
    }
    protected override void OnUpdate()
    {
        // Entities.ForEach processes each set of ComponentData on the main thread. This is not the recommended
        // method for best performance. However, we start with it here to demonstrate the clearer separation
        // between ComponentSystem Update (logic) and ComponentData (data).
        // There is no update logic on the individual ComponentData.
        var resourcesData = GetComponentDataFromEntity<ResourceData>();

        using (NativeArray<Entity> resourcesList = resources.ToEntityArray(Allocator.TempJob)) { 
            Entities.ForEach((Entity e, ref FlightTarget target) =>
            {
                if (target.entity == Entity.Null) {
                    // Get random resource
                    var resourceEntity = resourcesList[UnityEngine.Random.Range(0, resourcesList.Length)];
                    var resData = resourcesData[resourceEntity];

                    if (!resData.held) {
                        PostUpdateCommands.SetComponent(e, new FlightTarget { entity = resourceEntity, isResource = true });
                    }
                } else {
                    // If resoruce, check if its still free
                    if (target.isResource) {
                        var resData = resourcesData[target.entity];

                        if (resData.held) {
                            PostUpdateCommands.SetComponent(e, new FlightTarget());
                        }
                    }
                }
            });
        }
    }
}