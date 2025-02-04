using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

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
        Team0Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam0>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadOnly<Death>());
        Team1Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam1>(), ComponentType.ReadWrite<FlightTarget>(), ComponentType.ReadOnly<Death>());
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

    [BurstCompile]
    struct TargetUpdateJob : IJobForEachWithEntity<FlightTarget, Death>
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

        public void Execute(Entity entity, int index, ref FlightTarget flightTarget, [ReadOnly]ref Death death)
        {
            if (!death.Dying) {
                if (flightTarget.entity == Entity.Null || !flightTarget.isResource)
                {
                    flightTarget.holding = false;
                }

                if (flightTarget.entity == Entity.Null)
                {
                    if ((rand.NextFloat() < Aggression || ResourceList.Length == 0) && EnemyList.Length > 0)
                    {
                        flightTarget.entity = EnemyList[rand.NextInt(EnemyList.Length)];
                        flightTarget.isResource = false;
                    }
                    else
                    {
                        if (ResourceList.Length > 0)
                        {
                            Entity possibleTarget = ResourceList[rand.NextInt(ResourceList.Length)];
                            if (resourcesDataFromEntity.Exists(possibleTarget)) {
                                ResourceData resdat = resourcesDataFromEntity[possibleTarget];
                                if (!resdat.held && !resdat.dying)
                                {
                                    flightTarget.entity = possibleTarget;
                                    flightTarget.isResource = true;
                                } else if (resdat.held && resdat.holder != entity) {
                                    // Check if its an enemy
                                    for (int i = 0; i < EnemyList.Length; i++) {
                                        if (EnemyList[i] == resdat.holder) {
                                            flightTarget.entity = resdat.holder;
                                            flightTarget.isResource = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (flightTarget.entity != Entity.Null)
                {
                    // If its a resource and doesn't exist. Clear
                    if (flightTarget.isResource && !resourcesDataFromEntity.Exists(flightTarget.entity)) {
                        flightTarget.entity = Entity.Null;
                        flightTarget.isResource = false;
                        flightTarget.holding = false;
                        return;
                    }

                    if (flightTarget.isResource && resourcesDataFromEntity.Exists(flightTarget.entity))
                    {
                        if (!flightTarget.holding && resourcesDataFromEntity[flightTarget.entity].held)
                        {
                            flightTarget.entity = Entity.Null;
                            flightTarget.isResource = false;
                            flightTarget.holding = false;
                        }
                    }
                    else
                    {
                        if (deathFromEntity[flightTarget.entity].Dying) {
                            flightTarget.entity = Entity.Null;
                            flightTarget.isResource = false;
                            flightTarget.holding = false;
                        }
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

        TargetUpdateJob targetUpdateJob0 = new TargetUpdateJob();
        targetUpdateJob0.rand = rand;
        rand.NextFloat();
        targetUpdateJob0.EnemyList = Team1Entities;
        targetUpdateJob0.ResourceList = ResourceEntities;
        targetUpdateJob0.deathFromEntity = GetComponentDataFromEntity<Death>();
        targetUpdateJob0.resourcesDataFromEntity = GetComponentDataFromEntity<ResourceData>();
        targetUpdateJob0.Aggression = Aggression;
        JobHandle TargetUpdate0Handle = targetUpdateJob0.Schedule(Team0Query, JobHandle.CombineDependencies(inputDeps, resourceGatherHandle, team1GatherHandle));

        TargetUpdateJob targetUpdateJob1 = targetUpdateJob0;
        targetUpdateJob1.EnemyList = Team0Entities;
        JobHandle TargetUpdate1Handle = targetUpdateJob1.Schedule(Team1Query, JobHandle.CombineDependencies(TargetUpdate0Handle, team0GatherHandle));

        CleanupJob cleanupJob = new CleanupJob();
        cleanupJob.array0 = ResourceEntities;
        cleanupJob.array1 = Team0Entities;
        cleanupJob.array2 = Team1Entities;

        return cleanupJob.Schedule(TargetUpdate1Handle);
    }
}