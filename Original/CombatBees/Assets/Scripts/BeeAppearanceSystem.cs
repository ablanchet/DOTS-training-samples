using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BeeAppearanceSystem : JobComponentSystem
{
    public float RotationStiffness;
    public float SpeedStretch;

    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    [BurstCompile]
    [ExcludeComponent(typeof(Death))]
    struct BeeScalingSystemJob : IJobForEach<BeeSize, Velocity, NonUniformScale, Rotation, Translation>
    {
        public float DeltaTime;
        public float RotationStiffness;
        public float SpeedStretch;

        public void Execute(ref BeeSize beeSize, ref Velocity velocity, ref NonUniformScale nonUniformScale, ref Rotation rotation, ref Translation translation)
        {
            float3 oldSmoothPos = beeSize.SmoothPosition;
            if (beeSize.Attacking == false)
            {
                beeSize.SmoothPosition = math.lerp(beeSize.SmoothPosition, translation.Value, DeltaTime * RotationStiffness);
            }
            else
            {
                beeSize.SmoothPosition = translation.Value;
            }
            beeSize.SmoothDirection = beeSize.SmoothPosition - oldSmoothPos;

            float size = beeSize.Size;
            float3 scale = new float3(size, size, size);
            float velocityMagnitude = math.sqrt(velocity.v.x * velocity.v.x + velocity.v.y * velocity.v.y + velocity.v.z * velocity.v.z);
            float stretch = math.max(1f, velocityMagnitude * SpeedStretch);
            scale.z *= stretch;
            scale.x /= (stretch - 1f) / 5f + 1f;
            scale.y /= (stretch - 1f) / 5f + 1f;
            nonUniformScale.Value = scale;

            if (beeSize.SmoothDirection.x != 0 || beeSize.SmoothDirection.y != 0 || beeSize.SmoothDirection.z != 0)
            {
                rotation.Value = Quaternion.LookRotation(beeSize.SmoothDirection);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new BeeScalingSystemJob();
        job.DeltaTime = Time.fixedDeltaTime;
        job.RotationStiffness = RotationStiffness;
        job.SpeedStretch = SpeedStretch;

        return job.Schedule(this, inputDependencies);
    }
}