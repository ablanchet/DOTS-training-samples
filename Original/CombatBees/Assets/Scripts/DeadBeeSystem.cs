using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(VelocityBasedMovement)), UpdateAfter(typeof(BeeBehaviour))]
public class DeadBeeSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        EntityManager manager = World.Active.EntityManager;
        Entities.ForEach((Entity e, ref PendingDeath p) =>
        {
            Entity target = p.EntityThatWillDie;
            if (!manager.HasComponent<Death>(target) && manager.Exists(target))
            {
                FlightTarget flightTarget = manager.GetComponentData<FlightTarget>(target);

                if (flightTarget.entity != Entity.Null && flightTarget.isResource) {
                    manager.RemoveComponent<FollowEntity>(flightTarget.entity);
                    manager.AddComponent<ResourceFallingTag>(flightTarget.entity);
                }

                manager.SetComponentData<FlightTarget>(target, new FlightTarget());
                manager.AddComponent<Death>(target);
                manager.SetComponentData<Death>(target, new Death() { FirstUpdateDone = false, DeathTimer = 1 });
            }
        }
        );

        manager.DestroyEntity(GetEntityQuery(typeof(PendingDeath)));


        using (NativeList<Entity> entitiesToDestroy = new NativeList<Entity>(Allocator.Temp))
        {
            Entities.ForEach((Entity e, ref Death death, ref Velocity velocity, ref Translation translation) =>
            {
                if (!death.FirstUpdateDone)
                {
                    velocity.v *= 0.5f;
                    ParticleManager.SpawnParticle(translation.Value, ParticleType.Blood, velocity.v * .35f, 2f, 6);
                    death.FirstUpdateDone = true;
                }

                if (UnityEngine.Random.value < (death.DeathTimer - .5f) * .5f)
                {
                    ParticleManager.SpawnParticle(translation.Value, ParticleType.Blood, Vector3.zero);
                }

                velocity.v.y += Field.gravity * deltaTime;

                if (translation.Value.y < (-Field.size.y/2))
                {
                    velocity.v = float3(0);
                }

                death.DeathTimer -= deltaTime;

                if (death.DeathTimer < 0f)
                {
                    entitiesToDestroy.Add(e);
                }
            });

            foreach (Entity e in entitiesToDestroy)
            {
                manager.DestroyEntity(e);
            }
        }
    }
}