using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;

public class FlightTestSystem : ComponentSystem
{
    private float jitterSpeed = 5;

    protected override void OnUpdate()
    {
        // Entities.ForEach processes each set of ComponentData on the main thread. This is not the recommended
        // method for best performance. However, we start with it here to demonstrate the clearer separation
        // between ComponentSystem Update (logic) and ComponentData (data).
        // There is no update logic on the individual ComponentData.
        Entities.ForEach((Entity e, ref FlightData flightData, ref FlightTarget target, ref Translation translation) =>
        {
            // See if there are available resources where to fly

            if (target.entity != Entity.Null) {
                // Get the direction of flight
                var targetTranslation = EntityManager.GetComponentData<Translation>(target.entity);

                // Calculate velocity
                var velocity = new float3(0,0,0);

                var delta = targetTranslation.Value - translation.Value;
                float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                if (sqrDist > flightData.grabDistance * flightData.grabDistance) {
                    velocity += delta * (flightData.chaseForce * Time.deltaTime / Mathf.Sqrt(sqrDist));
                } else {
                    if (target.type == 1) {
                        // Grab Resource
                        PostUpdateCommands.SetComponent(target.entity, new ResourceData { held = true });
                    }
                }

                translation.Value += velocity;
            } else {
                translation.Value += new float3(UnityEngine.Random.insideUnitSphere * (jitterSpeed * Time.deltaTime));
            }
        });
    }
}