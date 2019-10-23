﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class BeeBehaviour : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery BeeTeam0GatherQuery;
    EntityQuery BeeTeam1GatherQuery;
    EntityQuery BeeTeam0UpdateQuery;
    EntityQuery BeeTeam1UpdateQuery;
    public float TeamAttraction;
    public float TeamRepulsion;
    public float FlightJitter;
    public float Damping;
    public float ChaseForce;
    public float AttackForce;
    public float GrabDistance;
    public float AttackDistance;
    public Unity.Mathematics.Random rand;


    //burst is not currently friendly with the command buffer
    //[BurstCompile]
    struct BeeBehaviourJob : IJobForEachWithEntity<Translation, Velocity, FlightTarget, BeeState>
    {
        [ReadOnly]
        public NativeArray<Entity> Friends;
        [ReadOnly]
        public NativeArray<Entity> Enemies;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationsFromEntity;
        public Unity.Mathematics.Random rand;
        public float DeltaTime;
        public float TeamAttraction;
        public float TeamRepulsion;
        public float FlightJitter;
        public float Damping;
        public float GrabDistance;
        public float AttackDistance;
        public float ChaseForce;
        public float AttackForce;
        public float3 FieldSize;
        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity e, int index, [ReadOnly]ref Translation translation, ref Velocity velocity, [ReadOnly]ref FlightTarget target, ref BeeState beeState)
        {
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
            if (target.entity != Entity.Null)
            {
                float3 targetPosition = TranslationsFromEntity[target.entity].Value;
                float3 targetDelta = targetPosition - translation.Value;
                float sqrDist = targetDelta.x * targetDelta.x + targetDelta.y * targetDelta.y + targetDelta.z * targetDelta.z;

                if (target.isResource)
                {
                    //moving to resources
                    if (sqrDist > GrabDistance * GrabDistance)
                    {
                        velocity.v += targetDelta * (ChaseForce * DeltaTime / Mathf.Sqrt(sqrDist));
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                    //                    else if (resource.stacked)
                    //                    {
                    //                        ResourceManager.GrabResource(bee, resource);
                    //                    }

                }
                else
                {
                    if (sqrDist > AttackDistance * AttackDistance)
                    {
                        velocity.v += targetDelta * (ChaseForce * DeltaTime / Mathf.Sqrt(sqrDist));
                    }
                    else
                    {
                        beeState.Attacking = true;
                        velocity.v += targetDelta * (AttackForce * DeltaTime / Mathf.Sqrt(sqrDist));
                        CommandBuffer.AddComponent<Death>(index, target.entity, new Death() { DeathTimer = 1, FirstUpdateDone = false }); ;
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
            //            if (bee.isHoldingResource)
            //            {
            //                resourceModifier = ResourceManager.instance.resourceSize;
            //            }
            if (System.Math.Abs(translation.Value.y) > FieldSize.y * .5f - resourceModifier)
            {
                translation.Value.y = (FieldSize.y * .5f - resourceModifier) * Mathf.Sign(translation.Value.y);
                velocity.v.y *= -.5f;
                velocity.v.z *= .8f;
                velocity.v.x *= .8f;
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

        var Beehaviour0 = new BeeBehaviourJob();
        Beehaviour0.Friends = team0Entities;
        Beehaviour0.Enemies = team1Entities;
        Beehaviour0.TranslationsFromEntity = TranslationsFromEntity;
        Beehaviour0.DeltaTime = Time.deltaTime;
        Beehaviour0.TeamAttraction = TeamAttraction;
        Beehaviour0.TeamRepulsion = TeamRepulsion;
        Beehaviour0.FlightJitter = FlightJitter;
        Beehaviour0.Damping = Damping;
        Beehaviour0.GrabDistance = GrabDistance;
            Beehaviour0.AttackDistance = AttackDistance;
        Beehaviour0.ChaseForce = ChaseForce;
        Beehaviour0.AttackForce = AttackForce;
        Beehaviour0.FieldSize = Field.size;
        Beehaviour0.rand = rand;
        rand.NextFloat();
        Beehaviour0.CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        JobHandle BeeHaviour0Handle = Beehaviour0.Schedule(BeeTeam0UpdateQuery, allGathersHandle);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(BeeHaviour0Handle);

        var Beehaviour1 = Beehaviour0;
        Beehaviour1.Friends = team1Entities;
        Beehaviour1.Enemies = team0Entities;
        Beehaviour1.rand = rand;
        rand.NextFloat();
        JobHandle BeeHaviour1Handle = Beehaviour1.Schedule(BeeTeam1UpdateQuery, BeeHaviour0Handle);  //this doesn't actually need to wait for BeeHaviour0Handle, but safety is confused about whether there might be some query overlap
        m_EntityCommandBufferSystem.AddJobHandleForProducer(BeeHaviour1Handle);

        var cleanupJob = new CleanupJob();
        cleanupJob.Entities0 = team0Entities;
        cleanupJob.Entities1 = team1Entities;

        // Now that the job is set up, schedule it to be run. 
        return cleanupJob.Schedule(JobHandle.CombineDependencies(BeeHaviour0Handle, BeeHaviour1Handle));
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        BeeTeam0GatherQuery = GetEntityQuery(typeof(BeeTeam0), typeof(Translation));
        BeeTeam1GatherQuery = GetEntityQuery(typeof(BeeTeam1), typeof(Translation));
        BeeTeam0UpdateQuery = GetEntityQuery(typeof(BeeTeam0), ComponentType.Exclude<BeeTeam1>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadWrite<BeeState>(), ComponentType.Exclude<Death>());
        BeeTeam1UpdateQuery = GetEntityQuery(typeof(BeeTeam1), ComponentType.Exclude<BeeTeam0>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadWrite<BeeState>(), ComponentType.Exclude<Death>());
        rand = new Unity.Mathematics.Random(3);
    }
}