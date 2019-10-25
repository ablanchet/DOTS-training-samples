using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(VelocityBasedMovement)), UpdateAfter(typeof(BeeBehaviour))]
public class NewlyDeadBeeSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    EntityQuery pendingDeathQuery;
    NativeHashMap<Entity, bool> AlreadyDead;


    struct ProcessPendingDeathJob : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<PendingDeath> pendingDeaths;
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> entitiesToDestroy;
        public NativeHashMap<Entity, bool> AlreadyDead;


        public EntityCommandBuffer CommandBuffer;
        public ComponentDataFromEntity<Death> deathFromEntity;


        public void Execute()
        {
            //probably I could use a sort or a hashmap or something to do this more elegantly.  for now I'm brute forcing to remove duplicates
            //for (int x = 0; x < pendingDeaths.Length; ++x)
            //{
            //    for (int y= x+1; y < pendingDeaths.Length; ++y)
            //    {
            //        if (pendingDeaths[x].EntityThatWillDie == pendingDeaths[y].EntityThatWillDie)
            //        {
            //            pendingDeaths[x] = new PendingDeath();
            //            break;
            //        }
            //    }
            //}
            AlreadyDead.Clear();
            for (int x = 0; x < pendingDeaths.Length; ++x)
            {
                Entity entityThatWillDie = pendingDeaths[x].EntityThatWillDie;
                if (!AlreadyDead.TryAdd(entityThatWillDie, true))
                    continue;

                if (deathFromEntity.HasComponent(entityThatWillDie))
                    continue; //already dead

                CommandBuffer.AddComponent<Death>(entityThatWillDie, new Death() { FirstUpdateDone = false, DeathTimer = 1 });
            }

            for (int x = 0; x < entitiesToDestroy.Length; ++x)
            {
                CommandBuffer.DestroyEntity(entitiesToDestroy[x]);
            }
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int requiredSize = pendingDeathQuery.CalculateEntityCount();
        if (requiredSize > AlreadyDead.Capacity)
        {
            AlreadyDead.Dispose();
            AlreadyDead = new NativeHashMap<Entity, bool>(requiredSize + 1024, Allocator.Persistent);
        }

        JobHandle gatherPendingDeathHandle;
        NativeArray<PendingDeath> pendingDeaths = pendingDeathQuery.ToComponentDataArray<PendingDeath>(Allocator.TempJob, out gatherPendingDeathHandle);
        JobHandle gatherEntitiesToDestroyHandle;
        NativeArray<Entity> entitiesToDestroy = pendingDeathQuery.ToEntityArray(Allocator.TempJob, out gatherEntitiesToDestroyHandle);
        var pendingDeathJob = new ProcessPendingDeathJob();
        pendingDeathJob.pendingDeaths = pendingDeaths;
        pendingDeathJob.entitiesToDestroy = entitiesToDestroy;
        pendingDeathJob.deathFromEntity = GetComponentDataFromEntity<Death>();
        pendingDeathJob.AlreadyDead = AlreadyDead;
        pendingDeathJob.CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
        JobHandle pendingDeathJobHandle = pendingDeathJob.Schedule(JobHandle.CombineDependencies(inputDeps, gatherPendingDeathHandle, gatherEntitiesToDestroyHandle));
        m_EntityCommandBufferSystem.AddJobHandleForProducer(pendingDeathJobHandle);

        return pendingDeathJobHandle;
    }



    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        pendingDeathQuery = World.EntityManager.CreateEntityQuery(typeof(PendingDeath));
        AlreadyDead = new NativeHashMap<Entity, bool>(1024, Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        AlreadyDead.Dispose();
    }
}