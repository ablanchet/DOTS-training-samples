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
    EntityQuery BeeTeam0GatherQuery;
    EntityQuery BeeTeam1GatherQuery;
    EntityQuery BeeTeam0UpdateQuery;
    EntityQuery BeeTeam1UpdateQuery;
    public float TeamAttraction;
    public float TeamRepulsion;
    public float FlightJitter;
    public float Damping;
    public float ChaseForce;
    public float GrabDistance;
    public Unity.Mathematics.Random rand;


    [BurstCompile]
    struct BeeBehaviourJob : IJobForEach<Translation, Velocity, FlightTarget>
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
        public float ChaseForce;
        public float3 FieldSize;


        public void Execute([ReadOnly]ref Translation translation, ref Velocity velocity, [ReadOnly]ref FlightTarget target)
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

                //Chasing enemy
                //delta = bee.enemyTarget.position - bee.position;
                //float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                //if (sqrDist > attackDistance * attackDistance)
                //{
                //    bee.velocity += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
                //}
                //else
                //{
                //    bee.isAttacking = true;
                //    bee.velocity += delta * (attackForce * deltaTime / Mathf.Sqrt(sqrDist));
                //    if (sqrDist < hitDistance * hitDistance)
                //    {
                //        ParticleManager.SpawnParticle(bee.enemyTarget.position, ParticleType.Blood, bee.velocity * .35f, 2f, 6);
                //        bee.enemyTarget.dead = true;
                //        bee.enemyTarget.velocity *= .5f;
                //        bee.enemyTarget = null;
                //    }
                //}
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
        Beehaviour0.ChaseForce = ChaseForce;
        Beehaviour0.FieldSize = Field.size;
        Beehaviour0.rand = rand;
        rand.NextFloat();
        JobHandle BeeHaviour0Handle = Beehaviour0.Schedule(BeeTeam0UpdateQuery, allGathersHandle);

        var Beehaviour1 = new BeeBehaviourJob();
        Beehaviour1.Friends = team1Entities;
        Beehaviour1.Enemies = team0Entities;
        Beehaviour1.TranslationsFromEntity = TranslationsFromEntity;
        Beehaviour1.DeltaTime = Time.deltaTime;
        Beehaviour1.TeamAttraction = TeamAttraction;
        Beehaviour1.TeamRepulsion = TeamRepulsion;
        Beehaviour1.FlightJitter = FlightJitter;
        Beehaviour1.Damping = Damping;
        Beehaviour1.GrabDistance = GrabDistance;
        Beehaviour1.ChaseForce = ChaseForce;
        Beehaviour1.FieldSize = Field.size;
        Beehaviour1.rand = rand;
        rand.NextFloat();
        JobHandle BeeHaviour1Handle = Beehaviour1.Schedule(BeeTeam1UpdateQuery, BeeHaviour0Handle);  //this doesn't actually need to wait for BeeHaviour0Handle, but safety is confused about whether there might be some query overlap

        var cleanupJob = new CleanupJob();
        cleanupJob.Entities0 = team0Entities;
        cleanupJob.Entities1 = team1Entities;

        // Now that the job is set up, schedule it to be run. 
        return cleanupJob.Schedule(JobHandle.CombineDependencies(BeeHaviour0Handle, BeeHaviour1Handle));
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        BeeTeam0GatherQuery = GetEntityQuery(typeof(BeeTeam0), typeof(Translation));
        BeeTeam1GatherQuery = GetEntityQuery(typeof(BeeTeam1), typeof(Translation));
        BeeTeam0UpdateQuery = GetEntityQuery(typeof(BeeTeam0), ComponentType.Exclude<BeeTeam1>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>());
        BeeTeam1UpdateQuery = GetEntityQuery(typeof(BeeTeam1), ComponentType.Exclude<BeeTeam0>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Velocity>(), ComponentType.ReadWrite<FlightTarget>());
        rand = new Unity.Mathematics.Random(3);
    }
}