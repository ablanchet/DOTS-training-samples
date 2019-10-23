using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

[UpdateAfter(typeof(AutoResourceSpawnerSystem))]
public class FindTargetSystem : JobComponentSystem
{
    private EntityQuery resourceQuery;
    private EntityQuery Team0Query;
    private EntityQuery Team1Query;
    public float Aggression;
    public Unity.Mathematics.Random rand;

    protected override void OnCreate()
    {
        resourceQuery = GetEntityQuery(
            ComponentType.ReadOnly<ResourceData>()
        );
        Team0Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam0>(), ComponentType.Exclude<Death>());
        Team1Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam1>(), ComponentType.Exclude<Death>());
        rand = new Unity.Mathematics.Random(3);
    }

    struct CleanupJob : IJob
    {
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> array0;
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> array1;
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> array2;

        public void Execute()
        {
        }
    }

    struct TargetUpdateJob : IJobForEachWithEntity<FlightTarget>
    {
        [ReadOnly]
        public ComponentDataFromEntity<ResourceData> resourcesDataFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Death> deathFromEntity;
        [ReadOnly]
        public NativeArray<Entity> ResourceList;
        [ReadOnly]
        public NativeArray<Entity> EnemyList;
        public float Aggression;
        public Unity.Mathematics.Random rand;

        public void Execute(Entity entity, int index, ref FlightTarget flightTarget)
        {
            if (flightTarget.entity == Entity.Null)
            {
                if (rand.NextFloat() < Aggression)
                {
                    if (EnemyList.Length > 0)
                    {
                        flightTarget.entity = EnemyList[rand.NextInt(0, EnemyList.Length)];
                        flightTarget.isResource = false;
                    }
                }
                else
                {
                    if (ResourceList.Length > 0)
                    {
                        Entity possibleTarget = ResourceList[rand.NextInt(0, ResourceList.Length)];
                        if (!resourcesDataFromEntity[possibleTarget].held)
                        {
                            flightTarget.entity = possibleTarget;
                            flightTarget.isResource = true;
                        }
                    }
                }
            }

            if (flightTarget.entity != Entity.Null)
            {
                if (flightTarget.isResource)
                {
                    if (!resourcesDataFromEntity.Exists(flightTarget.entity) || resourcesDataFromEntity[flightTarget.entity].held)
                    {
                        flightTarget.entity = Entity.Null;
                    }
                }
                else
                {
                    if (!deathFromEntity.Exists(flightTarget.entity) || deathFromEntity.HasComponent(flightTarget.entity))
                    {
                        flightTarget.entity = Entity.Null;
                    }
                }

            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeArray<Entity> ResourceEntities;
        NativeArray<Entity> Team0Entities;
        NativeArray<Entity> Team1Entities;

        JobHandle resourceGatherHandle;
        ResourceEntities = resourceQuery.ToEntityArray(Allocator.TempJob, out resourceGatherHandle);
        JobHandle team0GatherHandle;
        Team0Entities = resourceQuery.ToEntityArray(Allocator.TempJob, out team0GatherHandle);
        JobHandle team1GatherHandle;
        Team1Entities = resourceQuery.ToEntityArray(Allocator.TempJob, out team1GatherHandle);

        TargetUpdateJob targetUpdateJob0 = new TargetUpdateJob();
        targetUpdateJob0.rand = rand;
        rand.NextFloat();
        targetUpdateJob0.EnemyList = Team1Entities;
        targetUpdateJob0.ResourceList = ResourceEntities;
        targetUpdateJob0.deathFromEntity = GetComponentDataFromEntity<Death>();
        targetUpdateJob0.resourcesDataFromEntity = GetComponentDataFromEntity<ResourceData>();
        targetUpdateJob0.Aggression = Aggression;
        JobHandle TargetUpdate0Handle = targetUpdateJob0.Schedule(this, JobHandle.CombineDependencies(inputDeps, resourceGatherHandle, team1GatherHandle));

        TargetUpdateJob targetUpdateJob1 = targetUpdateJob0;
        targetUpdateJob1.EnemyList = Team0Entities;
        JobHandle TargetUpdate1Handle = targetUpdateJob1.Schedule(this, JobHandle.CombineDependencies(TargetUpdate0Handle, team0GatherHandle));

        CleanupJob cleanupJob = new CleanupJob();
        cleanupJob.array0 = ResourceEntities;
        cleanupJob.array1 = Team0Entities;
        cleanupJob.array2 = Team1Entities;

        return cleanupJob.Schedule(TargetUpdate1Handle);
    }
}