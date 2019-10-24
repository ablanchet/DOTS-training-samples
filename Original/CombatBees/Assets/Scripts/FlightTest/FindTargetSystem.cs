using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

[UpdateAfter(typeof(AutoResourceSpawnerSystem))]
public class FindTargetSystem : JobComponentSystem
{
    EntityManager manager;
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
        Team0Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam0>(), ComponentType.Exclude<Death>(), ComponentType.ReadWrite<FlightTarget>());
        Team1Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam1>(), ComponentType.Exclude<Death>(), ComponentType.ReadWrite<FlightTarget>());
        rand = new Unity.Mathematics.Random(3);

        manager = World.Active.EntityManager;
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
        public ComponentDataFromEntity<TargetCell> resourcesTargetCellFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<ResourceHeight> resourcesHeightFromEntity;

        [ReadOnly]
        public ComponentDataFromEntity<Death> deathFromEntity;
        [ReadOnly]
        public NativeArray<Entity> ResourceList;
        [ReadOnly]
        public NativeArray<Entity> EnemyList;
        [ReadOnly]
        public NativeArray<short> resourceStackHeights;
        public float Aggression;
        public Unity.Mathematics.Random rand;

        public void Execute(Entity entity, int index, ref FlightTarget flightTarget)
        {
            if (flightTarget.entity == Entity.Null || !flightTarget.isResource)
            {
                flightTarget.holding = false;
            }

            if (flightTarget.entity == Entity.Null)
            {
                if (EnemyList.Length > 0 && rand.NextFloat() < Aggression)
                {
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
                        var resourceData = resourcesDataFromEntity[possibleTarget];
                        if (!resourceData.held && resourcesTargetCellFromEntity.HasComponent(possibleTarget))
                        {
                            var targetCell = resourcesTargetCellFromEntity[possibleTarget];
                            var resourceHeight = resourcesHeightFromEntity[possibleTarget];
                            if (resourceStackHeights[targetCell.cellIdx] == resourceHeight.value)
                            {
                                flightTarget.entity = possibleTarget;
                                flightTarget.isResource = true;
                            }
                        }
                    }
                }
            }

            if (flightTarget.entity != Entity.Null)
            {
                if (flightTarget.isResource && resourcesDataFromEntity.Exists(flightTarget.entity))
                {
                    if (!flightTarget.holding && resourcesDataFromEntity[flightTarget.entity].held)
                    {
                        flightTarget.entity = Entity.Null;
                    }
                }
                else
                {
                    if (deathFromEntity.HasComponent(flightTarget.entity))
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
        Team0Entities = Team0Query.ToEntityArray(Allocator.TempJob, out team0GatherHandle);
        JobHandle team1GatherHandle;
        Team1Entities = Team1Query.ToEntityArray(Allocator.TempJob, out team1GatherHandle);

        TargetUpdateJob targetUpdateJob0 = new TargetUpdateJob
        {
            rand = rand,
            EnemyList = Team1Entities,
            ResourceList = ResourceEntities,
            deathFromEntity = GetComponentDataFromEntity<Death>(),
            resourcesDataFromEntity = GetComponentDataFromEntity<ResourceData>(),
            resourcesHeightFromEntity = GetComponentDataFromEntity<ResourceHeight>(),
            resourcesTargetCellFromEntity = GetComponentDataFromEntity<TargetCell>(),
            resourceStackHeights = World.GetExistingSystem<ResourceStackingSystem>().StackHeights,
            Aggression = Aggression
        };
        JobHandle targetUpdate0Handle = targetUpdateJob0.Schedule(Team0Query, JobHandle.CombineDependencies(inputDeps, resourceGatherHandle, team1GatherHandle));
        rand.NextFloat();

        TargetUpdateJob targetUpdateJob1 = targetUpdateJob0;
        targetUpdateJob1.EnemyList = Team0Entities;
        JobHandle targetUpdate1Handle = targetUpdateJob1.Schedule(Team1Query, JobHandle.CombineDependencies(targetUpdate0Handle, team0GatherHandle));

        CleanupJob cleanupJob = new CleanupJob();
        cleanupJob.array0 = ResourceEntities;
        cleanupJob.array1 = Team0Entities;
        cleanupJob.array2 = Team1Entities;

        return cleanupJob.Schedule(targetUpdate1Handle);
    }
}