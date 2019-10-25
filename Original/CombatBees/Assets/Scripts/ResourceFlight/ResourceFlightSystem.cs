using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;

[UpdateAfter(typeof(BeeSpawner))]
public class ResourceFlightSystem : JobComponentSystem
{
    EntityQuery FollowEntityQuery;
    NativeHashMap<Entity, float3> followPositions;


    struct GatherFollowPositionsJob : IJobForEach<FollowEntity>
    {
        public NativeHashMap<Entity, float3>.ParallelWriter followPositions;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> translationFromEntity;
        public void Execute([ReadOnly] ref FollowEntity follow)
        {
            if (follow.target == Entity.Null)
                return;

            followPositions.TryAdd(follow.target, translationFromEntity[follow.target].Value);
        }
    }


    struct ResourceFlightJob : IJobForEach<FollowEntity, Translation, NonUniformScale, ResourceFallingComponent, ResourceData>
    {
        [ReadOnly]
        public ComponentDataFromEntity<Death> deathFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<BeeSize> beeSizeFromEntity;
        [ReadOnly]
        public NativeHashMap<Entity, float3> followPositions;

        public float deltaTime;
    
        public void Execute(ref FollowEntity follow, ref Translation translation, [ReadOnly]ref NonUniformScale scale, ref ResourceFallingComponent fallComponent, ref ResourceData resourceData)
        {
            if (follow.target != Entity.Null)
            {
                if (deathFromEntity[follow.target].Dying)
                {
                    follow = new FollowEntity();
                    fallComponent.IsFalling = true;
                    resourceData.held = false;
                }
                else
                {
                    var followPosition  = followPositions[follow.target];
                    var targetSize = beeSizeFromEntity[follow.target].Size;
                    var targetPos = followPosition - new float3(0, 1, 0) * (scale.Value.y + targetSize) * .5f;
                    var resourcePos = math.lerp(translation.Value, targetPos, 15 * deltaTime);
                    translation.Value = targetPos;
                }
            }
        }
    }



    //struct ResourceFlightJob : IJobForEach<FollowEntity, Translation, NonUniformScale, ResourceFallingComponent>
    //{
    //    [ReadOnly]
    //    public ComponentDataFromEntity<Death> deathFromEntity;
    //    [ReadOnly]
    //    public ComponentDataFromEntity<Translation> translationFromEntity;
    //    [ReadOnly]
    //    public ComponentDataFromEntity<BeeSize> beeSizeFromEntity;
    //    public float deltaTime;
    //
    //    public void Execute(ref FollowEntity follow, ref Translation translation, [ReadOnly]ref NonUniformScale scale, ref ResourceFallingComponent fallComponent)
    //    {
    //        if (follow.target != Entity.Null)
    //        {
    //            if (deathFromEntity[follow.target].Dying)
    //            {
    //                follow = new FollowEntity();
    //                fallComponent.IsFalling = true;
    //            }
    //            else
    //            {
    //                var targetTranslation = translationFromEntity[follow.target];
    //                var targetSize = beeSizeFromEntity[follow.target].Size;
    //                var targetPos = targetTranslation.Value - new float3(0, 1, 0) * (scale.Value.y + targetSize) * .5f;
    //                var resourcePos = math.lerp(translation.Value, targetPos, 15 * deltaTime);
    //                translation.Value = targetPos;
    //            }
    //        }
    //    }
    //}


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int followEntityCount = FollowEntityQuery.CalculateEntityCount();
        if (followEntityCount > followPositions.Capacity)
        {
            followPositions.Dispose();
            followPositions = new NativeHashMap<Entity, float3>(followEntityCount + 1024, Allocator.TempJob);
        }
        followPositions.Clear();

        var gatherFollowPositionsJob = new GatherFollowPositionsJob();
        gatherFollowPositionsJob.followPositions = followPositions.AsParallelWriter();
        gatherFollowPositionsJob.translationFromEntity= GetComponentDataFromEntity<Translation>();
        JobHandle gatherHandle = gatherFollowPositionsJob.Schedule(this, inputDeps);

        var job = new ResourceFlightJob();
        job.beeSizeFromEntity = GetComponentDataFromEntity<BeeSize>();
        job.deathFromEntity= GetComponentDataFromEntity<Death>();
        job.followPositions = followPositions;
        job.deltaTime = Time.deltaTime;
        return job.Schedule(this, gatherHandle);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        FollowEntityQuery = World.EntityManager.CreateEntityQuery(typeof(FollowEntity));
        followPositions = new NativeHashMap<Entity, float3>(1024, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        followPositions.Dispose();
    }
}