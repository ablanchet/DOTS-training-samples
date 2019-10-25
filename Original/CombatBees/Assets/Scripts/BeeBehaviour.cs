using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class BeeBehaviour : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery BeeTeam0GatherQuery;
    EntityQuery BeeTeam1GatherQuery;
    EntityQuery BeeTeam0UpdateQuery;
    EntityQuery BeeTeam1UpdateQuery;
    EntityArchetype deathMessageArchetype;
    public float TeamAttraction;
    public float TeamRepulsion;
    public float FlightJitter;
    public float Damping;
    public float ChaseForce;
    public float AttackForce;
    public float CarryForce;
    public float GrabDistance;
    public float AttackDistance;
    public float ResourceSize;
    public Unity.Mathematics.Random rand;


    //burst is not currently friendly with the command buffer
    [BurstCompile]
    struct BeeBehaviourJob : IJobForEachWithEntity<Translation, Velocity, FlightTarget, BeeSize, Death>
    {
        [ReadOnly]
        public NativeArray<Entity> Friends;
        [ReadOnly]
        public NativeArray<Entity> Enemies;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationsFromEntity;

        [ReadOnly]
        public ComponentDataFromEntity<ResourceData> ResourcesDataFromEntity;
        public float teamId;
        public Unity.Mathematics.Random rand;
        public float DeltaTime;
        public float TeamAttraction;
        public float TeamRepulsion;
        public float FlightJitter;
        public float Damping;
        public float GrabDistance;
        public float AttackDistance;
        public float ResourceSize;
        public float ChaseForce;
        public float AttackForce;
        public float CarryForce;
        public float3 FieldSize;
        //public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity e, int index, [ReadOnly]ref Translation translation, ref Velocity velocity, ref FlightTarget target, ref BeeSize beeSize, [ReadOnly]ref Death death)
        {
            if (!death.Dying) {
                //Jitter & Damping
                {
                    float3 JitterVector = rand.NextFloat3(-1f, 1f);
                    float JitterLength = sqrt(JitterVector.x * JitterVector.x + JitterVector.y * JitterVector.y + JitterVector.z * JitterVector.z);

                    if (JitterLength != 0.0f)
                        JitterVector /= JitterLength;

                    velocity.v += JitterVector * FlightJitter * math.min(DeltaTime, 0.1f);
                    velocity.v *= (1f - Damping);
                }

                //Flocking
                {
                    Entity attractiveFriend = Friends[rand.NextInt(0, Friends.Length)];
                    float3 delta = TranslationsFromEntity[attractiveFriend].Value - translation.Value;
                    float dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if (dist > 0.1f)
                    {
                        velocity.v += delta * (TeamAttraction * DeltaTime / dist);
                    }

                    Entity repellentFriend = Friends[rand.NextInt(0, Friends.Length)];
                    delta = TranslationsFromEntity[repellentFriend].Value - translation.Value;
                    dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if (dist > 0.1f)
                    {
                        velocity.v -= delta * (TeamRepulsion * DeltaTime / dist);
                    }
                }

                //targetting resouce / enemy
                beeSize.Attacking = false;
                if (target.entity != Entity.Null && TranslationsFromEntity.Exists(target.entity))
                {
                    float3 targetPosition = TranslationsFromEntity[target.entity].Value;
                    float3 targetDelta = targetPosition - translation.Value;
                    float sqrDist = targetDelta.x * targetDelta.x + targetDelta.y * targetDelta.y + targetDelta.z * targetDelta.z;

                    if (target.isResource && !ResourcesDataFromEntity.Exists(target.entity)) {
                        // Clear the target since it doesn't exist anymore
                        target = new FlightTarget();
                    } 
                    else if (target.isResource && ResourcesDataFromEntity.Exists(target.entity))
                    {
                        // Get the resource data from the target. To check if it is being held or not
                        var resData = ResourcesDataFromEntity[target.entity];

                        if (target.holding) 
                        {
                            // We are holding our target, fly back to base
                            float3 basePos = new float3(-FieldSize.x * .45f + FieldSize.x * .9f * teamId, 0f, translation.Value.z);
                            float3 baseDelta = basePos - translation.Value;
                            var dist = Mathf.Sqrt(baseDelta.x * baseDelta.x + baseDelta.y * baseDelta.y + baseDelta.z * baseDelta.z);
                            velocity.v += baseDelta * (CarryForce * DeltaTime / dist);

                            if (dist < 5f) {
                                // Remove target
                                target.PendingAction = FlightTarget.Action.DropResource;
                            }
                        } 
                        else if (sqrDist > GrabDistance * GrabDistance) 
                        {
                            //moving to resources
                            velocity.v += targetDelta * (ChaseForce * DeltaTime / Mathf.Sqrt(sqrDist));
                        }
                        else
                        {
                            target.PendingAction = FlightTarget.Action.GrabResource;
                        }
                    }
                    else
                    {
                        if (sqrDist > AttackDistance * AttackDistance)
                        {
                            velocity.v += targetDelta * (ChaseForce * DeltaTime / Mathf.Sqrt(sqrDist));
                        }
                        else
                        {
                            beeSize.Attacking = true;
                            velocity.v += targetDelta * (AttackForce * DeltaTime / Mathf.Sqrt(sqrDist));
                            target.PendingAction = FlightTarget.Action.Kill;
                        }
                    }
                }




                //returning

                //boundaries
                if (System.Math.Abs(translation.Value.x) > FieldSize.x * .5f)
                {
                    translation.Value.x = (FieldSize.x * .5f) * Mathf.Sign(translation.Value.x);
                    velocity.v.x *= -.5f;
                    velocity.v.y *= .8f;
                    velocity.v.z *= .8f;
                }
                if (System.Math.Abs(translation.Value.z) > FieldSize.z * .5f)
                {
                    translation.Value.z = (FieldSize.z * .5f) * Mathf.Sign(translation.Value.z);
                    velocity.v.z *= -.5f;
                    velocity.v.x *= .8f;
                    velocity.v.y *= .8f;
                }
                float resourceModifier = 0f;
                if (target.holding)
                {
                    resourceModifier = ResourceSize;
                }
                if (System.Math.Abs(translation.Value.y) > FieldSize.y * .5f - resourceModifier)
                {
                    translation.Value.y = (FieldSize.y * .5f - resourceModifier) * Mathf.Sign(translation.Value.y);
                    velocity.v.y *= -.5f;
                    velocity.v.z *= .8f;
                    velocity.v.x *= .8f;
                }
            }
        }
    }

    struct BeeBehaviourResolveInteractions : IJobForEachWithEntity<FlightTarget, Velocity, Death>
    {
        // public EntityArchetype deathMessageArchetype;
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationsFromEntity;

        public void Execute(Entity e, int index, ref FlightTarget target, ref Velocity velocity, ref Death death)
        {
            if (target.PendingAction == FlightTarget.Action.None || death.Dying)
            {
                return;
            }

            FlightTarget.Action WantedAction = target.PendingAction;
            target.PendingAction = FlightTarget.Action.None;//ensure we don't leave this action in its pending state when we finish here

            if (TranslationsFromEntity.Exists(target.entity)) {
                switch(WantedAction)
                {
                    case FlightTarget.Action.GrabResource:
                        {
                            target.holding = true;
                            CommandBuffer.SetComponent<FollowEntity>(index, target.entity, new FollowEntity { target = e });
                            CommandBuffer.SetComponent<ResourceData>(index, target.entity, new ResourceData { held = true, holder = e });
                            CommandBuffer.RemoveComponent<TargetCell>(index, target.entity);
                        }
                        break;

                    case FlightTarget.Action.DropResource:
                        {
                            CommandBuffer.SetComponent<ResourceFallingComponent>(index, target.entity, new ResourceFallingComponent() { IsFalling = true});
                            CommandBuffer.SetComponent<FollowEntity>(index, target.entity, new FollowEntity());
                            CommandBuffer.SetComponent(index, target.entity, new ResourceData { held = false, holder = Entity.Null, velocity = velocity.v });
                            target = new FlightTarget();
                        }
                        break;
                    case FlightTarget.Action.Kill:
                        {
                            CommandBuffer.SetComponent<Death>(index, target.entity, new Death { DeathTimer = 1, FirstUpdateDone = false, Dying = true });
                            target = new FlightTarget();
                        }
                        break;
                };
            }
        }
    }

    struct CleanupJob : IJob
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> Entities0;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> Entities1;
        public void Execute()
        {
        }
    }




    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        JobHandle getTeam0Handle;
        var team0Entities = BeeTeam0GatherQuery.ToEntityArray(Allocator.TempJob, out getTeam0Handle);

        JobHandle getTeam1Handle;
        var team1Entities = BeeTeam1GatherQuery.ToEntityArray(Allocator.TempJob, out getTeam1Handle);
        JobHandle allGathersHandle = JobHandle.CombineDependencies(getTeam0Handle, getTeam1Handle);

        var TranslationsFromEntity = GetComponentDataFromEntity<Translation>(true);
        var ResourcesDataFromEntity = GetComponentDataFromEntity<ResourceData>(true);

        var Beehaviour0 = new BeeBehaviourJob();
        Beehaviour0.Friends = team0Entities;
        Beehaviour0.Enemies = team1Entities;
        Beehaviour0.TranslationsFromEntity = TranslationsFromEntity;
        Beehaviour0.ResourcesDataFromEntity = ResourcesDataFromEntity;
        Beehaviour0.DeltaTime = Time.fixedDeltaTime;
        Beehaviour0.TeamAttraction = TeamAttraction;
        Beehaviour0.TeamRepulsion = TeamRepulsion;
        Beehaviour0.FlightJitter = FlightJitter;
        Beehaviour0.Damping = Damping;
        Beehaviour0.GrabDistance = GrabDistance;
        Beehaviour0.AttackDistance = AttackDistance;
        Beehaviour0.ResourceSize = ResourceSize;
        Beehaviour0.ChaseForce = ChaseForce;
        Beehaviour0.AttackForce = AttackForce;
        Beehaviour0.CarryForce = CarryForce;
        Beehaviour0.FieldSize = Field.size;
        Beehaviour0.rand = rand;
        Beehaviour0.teamId = 0;
        rand.NextFloat();
        //Beehaviour0.CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        JobHandle BeeHaviour0Handle = Beehaviour0.Schedule(BeeTeam0UpdateQuery, JobHandle.CombineDependencies(allGathersHandle, inputDependencies));
        //m_EntityCommandBufferSystem.AddJobHandleForProducer(BeeHaviour0Handle);

        var Beehaviour1 = Beehaviour0;
        Beehaviour1.Friends = team1Entities;
        Beehaviour1.Enemies = team0Entities;
        Beehaviour1.teamId = 1;
        Beehaviour1.rand = rand;
        rand.NextFloat();
        JobHandle BeeHaviour1Handle = Beehaviour1.Schedule(BeeTeam1UpdateQuery, BeeHaviour0Handle);  //this doesn't actually need to wait for BeeHaviour0Handle, but safety is confused about whether there might be some query overlap
        //m_EntityCommandBufferSystem.AddJobHandleForProducer(BeeHaviour1Handle);

        JobHandle BothBeeBehaviourHandles = JobHandle.CombineDependencies(BeeHaviour0Handle, BeeHaviour1Handle);

        var resolveInteractionsJob = new BeeBehaviourResolveInteractions() { CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()};
        resolveInteractionsJob.TranslationsFromEntity = TranslationsFromEntity;
        JobHandle resolveInteractionsHandle = resolveInteractionsJob.Schedule(this, BothBeeBehaviourHandles);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(resolveInteractionsHandle);

        var cleanupJob = new CleanupJob();
        cleanupJob.Entities0 = team0Entities;
        cleanupJob.Entities1 = team1Entities;

        // Now that the job is set up, schedule it to be run. 
        JobHandle cleanupHandle = cleanupJob.Schedule(BothBeeBehaviourHandles);

        return JobHandle.CombineDependencies(cleanupHandle, resolveInteractionsHandle);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        BeeTeam0GatherQuery = GetEntityQuery(typeof(BeeTeam0), typeof(Translation));
        BeeTeam1GatherQuery = GetEntityQuery(typeof(BeeTeam1), typeof(Translation));
        BeeTeam0UpdateQuery = GetEntityQuery(typeof(BeeTeam0), ComponentType.Exclude<BeeTeam1>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadWrite<BeeSize>(), ComponentType.ReadOnly<Death>());
        BeeTeam1UpdateQuery = GetEntityQuery(typeof(BeeTeam1), ComponentType.Exclude<BeeTeam0>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadWrite<BeeSize>(), ComponentType.ReadOnly<Death>());

        //deathMessageArchetype = EntityManager.CreateArchetype(typeof(PendingDeath));

        rand = new Unity.Mathematics.Random(3);
    }
}