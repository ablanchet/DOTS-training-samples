using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;

public class BeeAppearanceSystem : ComponentSystem
{
    public float RotationStiffness;
    public float SpeedStretch;

    public Vector4 TeamColor0;
    public Vector4 TeamColor1;

    NativeArray<Matrix4x4> Matrices;
    NativeArray<Vector4> Colors;

    List<Matrix4x4[]> managedBeeMatrices;
    List<Vector4[]> managedBeeColors;

    public Mesh beeMesh;
    public Material beeMaterial;
    MaterialPropertyBlock matProps;


    EntityQuery beesQuery;

    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    [BurstCompile]
    struct BeeFillRenderInfo : IJobForEachWithEntity<BeeSize, Velocity, Translation>
    {
        public float DeltaTime;
        public float RotationStiffness;
        public float SpeedStretch;
        public float4 TeamColor0;
        public float4 TeamColor1;

        public NativeArray<Matrix4x4> Matrices;
        public NativeArray<Vector4> colors;

        public void Execute(Entity e, int index, ref BeeSize beeSize, [ReadOnly]ref Velocity velocity, [ReadOnly]ref Translation translation)
        {
            float3 oldSmoothPos = beeSize.SmoothPosition;
            if (beeSize.Attacking == false)
            {
                beeSize.SmoothPosition = UnityEngine.Vector3.Lerp(beeSize.SmoothPosition, translation.Value, DeltaTime * RotationStiffness);
            }
            else
            {
                beeSize.SmoothPosition = translation.Value;
            }
            beeSize.SmoothDirection = beeSize.SmoothPosition - oldSmoothPos;

            float size = beeSize.Size;
            float3 scale = new float3(size, size, size);
            float velocityMagnitude = sqrt(velocity.v.x * velocity.v.x + velocity.v.y * velocity.v.y + velocity.v.z * velocity.v.z);
            float stretch = Mathf.Max(1f, velocityMagnitude * SpeedStretch);
            scale.z *= stretch;
            scale.x /= (stretch - 1f) / 5f + 1f;
            scale.y /= (stretch - 1f) / 5f + 1f;

            Quaternion rotation;
            if (beeSize.SmoothDirection.x != 0 || beeSize.SmoothDirection.y != 0 || beeSize.SmoothDirection.z != 0)
            {
                rotation = Quaternion.LookRotation(beeSize.SmoothDirection);
            }
            else
            {
                rotation = Quaternion.identity;
            }

            Matrices[index] = Matrix4x4.TRS(translation.Value, rotation, scale);
            colors[index] = ((beeSize.TeamColor == 0) ? TeamColor0 : TeamColor1) * (beeSize.Faded ? 0.7f : 1.0f);
        }
    }

    protected override void OnUpdate()
    {
        var job = new BeeFillRenderInfo();
        job.DeltaTime = Time.fixedDeltaTime;
        job.RotationStiffness = RotationStiffness;
        job.SpeedStretch = SpeedStretch;
        job.TeamColor0 = TeamColor0;
        job.TeamColor1 = TeamColor1;

        int beesCount = beesQuery.CalculateEntityCount();
        Matrices = new NativeArray<Matrix4x4>(beesCount, Allocator.TempJob);
        Colors = new NativeArray<Vector4>(beesCount, Allocator.TempJob);
        job.Matrices = Matrices;
        job.colors = Colors;

        JobHandle FillHandle = job.Schedule(beesQuery);
        FillHandle.Complete();

        int MaxSizePerBatch = 1023;
        int FullBatchCount = beesCount / MaxSizePerBatch;
        int PartialBatchSize = beesCount % MaxSizePerBatch;

        while (managedBeeMatrices.Count < FullBatchCount + 1)
            managedBeeMatrices.Add(new Matrix4x4[MaxSizePerBatch]);
        while (managedBeeColors.Count < FullBatchCount + 1)
            managedBeeColors.Add(new Vector4[MaxSizePerBatch]);

        for (int x = 0; x < FullBatchCount; ++x)
        {
            NativeArray<Matrix4x4>.Copy(Matrices, x * MaxSizePerBatch, managedBeeMatrices[x], 0, MaxSizePerBatch);
            NativeArray<Vector4>.Copy(Colors, x * MaxSizePerBatch, managedBeeColors[x], 0, MaxSizePerBatch);
            matProps.SetVectorArray("_Color", managedBeeColors[x]);
            Graphics.DrawMeshInstanced(beeMesh, 0, beeMaterial, managedBeeMatrices[x], MaxSizePerBatch, matProps);
        }
        if (PartialBatchSize > 0)
        {
            NativeArray<Matrix4x4>.Copy(Matrices, FullBatchCount * MaxSizePerBatch, managedBeeMatrices[FullBatchCount], 0, PartialBatchSize);
            NativeArray<Vector4>.Copy(Colors, FullBatchCount * MaxSizePerBatch, managedBeeColors[FullBatchCount], 0, PartialBatchSize);
            matProps.SetVectorArray("_Color", managedBeeColors[FullBatchCount]);
            Graphics.DrawMeshInstanced(beeMesh, 0, beeMaterial, managedBeeMatrices[FullBatchCount], PartialBatchSize, matProps);
        }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        beesQuery = GetEntityQuery(
            ComponentType.ReadWrite<BeeSize>(),
            ComponentType.ReadOnly<Velocity>(),
            ComponentType.ReadOnly<Translation>()
            );
        matProps = new MaterialPropertyBlock();
        managedBeeMatrices = new List<Matrix4x4[]>();
        managedBeeColors = new List<Vector4[]>();
    }
}