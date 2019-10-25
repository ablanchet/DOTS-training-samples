using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public class BeeBehaviour : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_BeeTeam0GatherQuery;
    EntityQuery m_BeeTeam1GatherQuery;
    EntityQuery m_BeeTeam0UpdateQuery;
    EntityQuery m_BeeTeam1UpdateQuery;
    Random m_Rand;

    public float teamAttraction;
    public float teamRepulsion;
    public float flightJitter;
    public float damping;
    public float chaseForce;
    public float attackForce;
    public float attackDistance;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_BeeTeam0GatherQuery = GetEntityQuery(typeof(BeeTeam0), typeof(Translation));
        m_BeeTeam1GatherQuery = GetEntityQuery(typeof(BeeTeam1), typeof(Translation));
        m_BeeTeam0UpdateQuery = GetEntityQuery(typeof(BeeTeam0), ComponentType.Exclude<BeeTeam1>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadWrite<BeeAppearance>(), ComponentType.Exclude<Death>());
        m_BeeTeam1UpdateQuery = GetEntityQuery(typeof(BeeTeam1), ComponentType.Exclude<BeeTeam0>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadWrite<BeeAppearance>(), ComponentType.Exclude<Death>());
        m_Rand = new Random(3);
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var team0Entities = m_BeeTeam0GatherQuery.ToEntityArray(Allocator.TempJob);
        var team1Entities = m_BeeTeam1GatherQuery.ToEntityArray(Allocator.TempJob);
        var translationsFromEntity = GetComponentDataFromEntity<Translation>(true);

        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        m_Rand.NextFloat();
        var beehaviour0 = new BeeBehaviourJob
        {
            friends = team0Entities,
            enemies = team1Entities,
            translationsFromEntity = translationsFromEntity,
            deltaTime = Time.fixedDeltaTime,
            teamAttraction = teamAttraction,
            teamRepulsion = teamRepulsion,
            flightJitter = flightJitter,
            damping = damping,
            attackDistance = attackDistance,
            chaseForce = chaseForce,
            attackForce = attackForce,
            fieldSize = Field.size,
            rand = m_Rand,
            commandBuffer = commandBuffer
        };

        var beehaviour0Handle = beehaviour0.Schedule(m_BeeTeam0UpdateQuery, inputDependencies);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(beehaviour0Handle);

        m_Rand.NextFloat();
        var beehaviour1 = beehaviour0;
        beehaviour1.friends = team1Entities;
        beehaviour1.enemies = team0Entities;
        beehaviour1.rand = m_Rand;
        var beeHaviour1Handle = beehaviour1.Schedule(m_BeeTeam1UpdateQuery, beehaviour0Handle);  //this doesn't actually need to wait for BeeHaviour0Handle, but safety is confused about whether there might be some query overlap
        m_EntityCommandBufferSystem.AddJobHandleForProducer(beeHaviour1Handle);

        // Now that the job is set up, schedule it to be run.
        return new CleanupJob
        {
            entities0 = team0Entities,
            entities1 = team1Entities
        }.Schedule(beeHaviour1Handle);
    }

    struct BeeBehaviourJob : IJobForEachWithEntity<Translation, Velocity, FlightTarget, BeeAppearance>
    {
        [ReadOnly]
        public NativeArray<Entity> friends;
        [ReadOnly]
        public NativeArray<Entity> enemies;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> translationsFromEntity;

        public Random rand;
        public float deltaTime;
        public float teamAttraction;
        public float teamRepulsion;
        public float flightJitter;
        public float damping;
        public float attackDistance;
        public float chaseForce;
        public float attackForce;
        public float3 fieldSize;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity e, int index, [ReadOnly]ref Translation translation, ref Velocity velocity, ref FlightTarget target, ref BeeAppearance beeAppearance)
        {
            //Jitter & Damping
            {
                var jitterVector = rand.NextFloat3(-1f, 1f);
                var jitterLength = sqrt(jitterVector.x * jitterVector.x + jitterVector.y * jitterVector.y + jitterVector.z * jitterVector.z);

                if (jitterLength != 0.0f)
                    jitterVector /= jitterLength;

                velocity.v += jitterVector * flightJitter * math.min(deltaTime, 0.1f);
                velocity.v *= (1f - damping);
            }

            //Flocking
            {
                var attractiveFriend = friends[rand.NextInt(0, friends.Length)];
                var delta = translationsFromEntity[attractiveFriend].Value - translation.Value;
                var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0.1f)
                {
                    velocity.v += delta * (teamAttraction * deltaTime / dist);
                }

                var repellentFriend = friends[rand.NextInt(0, friends.Length)];
                delta = translationsFromEntity[repellentFriend].Value - translation.Value;
                dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0.1f)
                {
                    velocity.v -= delta * (teamRepulsion * deltaTime / dist);
                }
            }

            //targetting resouce / enemy
            beeAppearance.Attacking = false;
            if (target.entity != Entity.Null && translationsFromEntity.Exists(target.entity))
            {
                var targetPosition = translationsFromEntity[target.entity].Value;
                var targetDelta = targetPosition - translation.Value;
                var sqrDist = targetDelta.x * targetDelta.x + targetDelta.y * targetDelta.y + targetDelta.z * targetDelta.z;

                if (sqrDist > attackDistance * attackDistance)
                {
                    velocity.v += targetDelta * (chaseForce * deltaTime / sqrt(sqrDist));
                }
                else
                {
                    beeAppearance.Attacking = true;
                    velocity.v += targetDelta * (attackForce * deltaTime / sqrt(sqrDist));

                    var deathMessage = commandBuffer.CreateEntity(index);
                    commandBuffer.AddComponent(index, deathMessage, new PendingDeath { EntityThatWillDie = target.entity });
                }
            }

            //returning

            //boundaries
            if (abs(translation.Value.x) > fieldSize.x * .5f)
            {
                translation.Value.x = (fieldSize.x * .5f) * Mathf.Sign(translation.Value.x);
                velocity.v.x *= -.5f;
                velocity.v.y *= .8f;
                velocity.v.z *= .8f;
            }
            if (abs(translation.Value.z) > fieldSize.z * .5f)
            {
                translation.Value.z = (fieldSize.z * .5f) * Mathf.Sign(translation.Value.z);
                velocity.v.z *= -.5f;
                velocity.v.x *= .8f;
                velocity.v.y *= .8f;
            }
            if (abs(translation.Value.y) > fieldSize.y * .5f)
            {
                translation.Value.y = (fieldSize.y * .5f) * Mathf.Sign(translation.Value.y);
                velocity.v.y *= -.5f;
                velocity.v.z *= .8f;
                velocity.v.x *= .8f;
            }
        }
    }

    struct CleanupJob : IJob
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> entities0;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> entities1;
        public void Execute()
        {
        }
    }
}