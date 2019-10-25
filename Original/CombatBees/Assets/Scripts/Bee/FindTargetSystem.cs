using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Random = Unity.Mathematics.Random;

[UpdateAfter(typeof(AutoResourceSpawnerSystem))]
public class FindTargetSystem : JobComponentSystem
{
    public float aggression;

    Random m_Rand;
    EntityQuery m_Team0Query;
    EntityQuery m_Team1Query;

    protected override void OnCreate()
    {
        m_Team0Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam0>(), ComponentType.Exclude<Death>(), ComponentType.ReadWrite<FlightTarget>());
        m_Team1Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam1>(), ComponentType.Exclude<Death>(), ComponentType.ReadWrite<FlightTarget>());
        m_Rand = new Random(3);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var team0Entities = m_Team0Query.ToEntityArray(Allocator.TempJob, out var tqh0);
        var team1Entities = m_Team1Query.ToEntityArray(Allocator.TempJob,out var tqh1);

        var handle0 = new TargetUpdateJob
        {
            rand = m_Rand,
            enemyList = team1Entities,
            deathFromEntity = GetComponentDataFromEntity<Death>(),
            aggression = aggression
        }.Schedule(m_Team0Query, JobHandle.CombineDependencies(inputDeps, tqh1));

        var handle1 = new TargetUpdateJob
        {
            rand = m_Rand,
            enemyList = team0Entities,
            deathFromEntity = GetComponentDataFromEntity<Death>(),
            aggression = aggression
        }.Schedule(m_Team1Query, JobHandle.CombineDependencies(handle0, tqh0));

        return new CleanupJob
            {
                array0 = team0Entities,
                array1 = team1Entities
            }
            .Schedule(handle1);
    }

    [BurstCompile]
    struct TargetUpdateJob : IJobForEach_C<FlightTarget>
    {
        [ReadOnly] public ComponentDataFromEntity<Death> deathFromEntity;
        [ReadOnly] public NativeArray<Entity> enemyList;
        public float aggression;
        public Random rand;

        public void Execute(ref FlightTarget flightTarget)
        {
            if (flightTarget.entity == Entity.Null && enemyList.Length > 0 && rand.NextFloat() < aggression)
            {
                flightTarget.entity = enemyList[rand.NextInt(0, enemyList.Length)];
            }
            else if (flightTarget.entity != Entity.Null && deathFromEntity.HasComponent(flightTarget.entity))
            {
                flightTarget.entity = Entity.Null;
            }
        }
    }

    struct CleanupJob : IJob
    {
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> array0;
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Entity> array1;

        public void Execute() { }
    }
}