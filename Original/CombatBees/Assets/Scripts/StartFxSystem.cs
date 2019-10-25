// using System.Collections.Generic;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Transforms;
// using static Unity.Mathematics.math;


// public struct BloodInfo
// {
//     public float3 Position;
//     public float3 Velocity;
//     public float VelocityJitter;
//     public int count;
// }

// public class StartFxSystem : ComponentSystem
// {
//     public List<NativeQueue<BloodInfo>> BloodFxQueue;
//     public List<NativeQueue<float3>> SpawnFxQueue;
//     private int FrameIndex;
    
//     protected override void OnUpdate()
//     {
//         NativeQueue<float3> currentSpawn = SpawnFxQueue[FrameIndex];
//         NativeQueue<BloodInfo> currentBlood = BloodFxQueue[FrameIndex];

//         FrameIndex = (FrameIndex == 0) ? 1 : 0;

//         float3 position;
//         while (currentSpawn.TryDequeue(out position))
//         {
//             ParticleManager.SpawnParticle(position, ParticleType.SpawnFlash, UnityEngine.Vector3.zero, 6f, 5);
//         }

//         BloodInfo bi;
//         while (currentBlood.TryDequeue(out bi))
//         {
//             ParticleManager.SpawnParticle(bi.Position, ParticleType.Blood, bi.Velocity, bi.VelocityJitter, bi.count);
//         }
//     }

//     protected override void OnCreate()
//     {
//         base.OnCreate();
//         GetBloodQueue();
//         GetSpawnQueue();
//     }

//     public NativeQueue<BloodInfo> GetBloodQueue()
//     {
//         if (BloodFxQueue == null)
//         {
//             BloodFxQueue = new List<NativeQueue<BloodInfo>>(2);
//             BloodFxQueue.Add(new NativeQueue<BloodInfo>(Allocator.Persistent));
//             BloodFxQueue.Add(new NativeQueue<BloodInfo>(Allocator.Persistent));
//         }
//         return BloodFxQueue[FrameIndex];
//     }

//     public NativeQueue<float3> GetSpawnQueue()
//     {
//         if (SpawnFxQueue == null)
//         {
//             SpawnFxQueue = new List<NativeQueue<float3>>(2);
//             SpawnFxQueue.Add(new NativeQueue<float3>(Allocator.Persistent));
//             SpawnFxQueue.Add(new NativeQueue<float3>(Allocator.Persistent));
//         }
//         return SpawnFxQueue[FrameIndex];
//     }

//     protected override void OnDestroy()
//     {
//         base.OnDestroy();
//         if (BloodFxQueue != null)
//         {
//             foreach (var queue in BloodFxQueue)
//                 queue.Dispose();
//         }
//         if (SpawnFxQueue != null)
//         {
//             foreach (var queue in SpawnFxQueue)
//                 queue.Dispose();
//         }
//     }
// }