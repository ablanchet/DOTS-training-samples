using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

    
public class ParticleDrawSystem : ComponentSystem
{
    public Mesh particleMesh;
    public Material particleMaterial;
    public float SpeedStretch;

    NativeArray<Matrix4x4> Matrices;
    NativeArray<Vector4> Colors;

    List<Matrix4x4[]> managedMatrices;
    List<Vector4[]> managedColors;

    MaterialPropertyBlock matProps;
    EntityQuery particleQuery;



    public ParticleSpawner Spawner;
    //spawn limits
    const int instancesPerBatch = 1023;
    const int maxParticleCount = 10 * instancesPerBatch;
    private EntityQuery ParticleQuery;


    [BurstCompile]
    struct ParticleFillRenderInfo : IJobForEachWithEntity<ParticleComponent, Velocity, Translation>
    {
        public float SpeedStretch;
        public NativeArray<Matrix4x4> Matrices;
        public NativeArray<Vector4> Colors;

        public void Execute(Entity e, int index, ref ParticleComponent particle, [ReadOnly]ref Velocity velocity, [ReadOnly]ref Translation translation)
        {
            Quaternion rotation = Quaternion.identity;
            Vector3 scale = particle.size * particle.life;
            if (particle.type == ParticleType.Blood)
            {
                rotation = Quaternion.LookRotation(velocity.v);
                scale.y *= 1f + Vector3.Magnitude(velocity.v) * SpeedStretch;
            }
            Matrices[index] = Matrix4x4.TRS(translation.Value, rotation, scale);
            Colors[index] = particle.color;
        }
    }

    protected override void OnUpdate()
    {
        //disable the spawner if full
        Spawner.Disabled = (ParticleQuery.CalculateEntityCount() >= maxParticleCount);


        var job = new ParticleFillRenderInfo();
        job.SpeedStretch = SpeedStretch;

        int particleCount = particleQuery.CalculateEntityCount();
        Matrices = new NativeArray<Matrix4x4>(particleCount, Allocator.TempJob);
        Colors = new NativeArray<Vector4>(particleCount, Allocator.TempJob);
        job.Matrices = Matrices;
        job.Colors = Colors;

        JobHandle FillHandle = job.Schedule(particleQuery);
        FillHandle.Complete();

        int MaxSizePerBatch = 1023;
        int FullBatchCount = particleCount / MaxSizePerBatch;
        int PartialBatchSize = particleCount % MaxSizePerBatch;

        while (managedMatrices.Count < FullBatchCount + 1)
            managedMatrices.Add(new Matrix4x4[MaxSizePerBatch]);
        while (managedColors.Count < FullBatchCount + 1)
            managedColors.Add(new Vector4[MaxSizePerBatch]);

        for (int x = 0; x < FullBatchCount; ++x)
        {
            NativeArray<Matrix4x4>.Copy(Matrices, x * MaxSizePerBatch, managedMatrices[x], 0, MaxSizePerBatch);
            NativeArray<Vector4>.Copy(Colors, x * MaxSizePerBatch, managedColors[x], 0, MaxSizePerBatch);
            matProps.SetVectorArray("_Color", managedColors[x]);
            Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, managedMatrices[x], MaxSizePerBatch, matProps);
        }
        if (PartialBatchSize > 0)
        {
            NativeArray<Matrix4x4>.Copy(Matrices, FullBatchCount * MaxSizePerBatch, managedMatrices[FullBatchCount], 0, PartialBatchSize);
            NativeArray<Vector4>.Copy(Colors, FullBatchCount * MaxSizePerBatch, managedColors[FullBatchCount], 0, PartialBatchSize);
            matProps.SetVectorArray("_Color", managedColors[FullBatchCount]);
            Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, managedMatrices[FullBatchCount], PartialBatchSize, matProps);
        }

        Matrices.Dispose();
        Colors.Dispose();
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        particleQuery = GetEntityQuery(
            ComponentType.ReadWrite<ParticleComponent>(),
            ComponentType.ReadOnly<Velocity>(),
            ComponentType.ReadOnly<Translation>()
            );
        matProps = new MaterialPropertyBlock();
        managedMatrices = new List<Matrix4x4[]>();
        managedColors = new List<Vector4[]>();
        Spawner = new ParticleSpawner(World.Active.EntityManager);
        ParticleQuery = GetEntityQuery(typeof(ParticleComponent));
    }
}