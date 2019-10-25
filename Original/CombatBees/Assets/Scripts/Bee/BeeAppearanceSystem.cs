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

    [BurstCompile]
    [ExcludeComponent(typeof(Death))]
    struct BeeScalingSystemJob : IJobForEach<BeeAppearance, Velocity, NonUniformScale, Rotation, Translation>
    {
        public float DeltaTime;
        public float RotationStiffness;
        public float SpeedStretch;

        public void Execute(ref BeeAppearance beeAppearance, ref Velocity velocity, ref NonUniformScale nonUniformScale, ref Rotation rotation, ref Translation translation)
        {
            float3 oldSmoothPos = beeAppearance.SmoothPosition;
            if (beeAppearance.Attacking == false)
            {
                beeAppearance.SmoothPosition = math.lerp(beeAppearance.SmoothPosition, translation.Value, DeltaTime * RotationStiffness);
            }
            else
            {
                beeAppearance.SmoothPosition = translation.Value;
            }
            beeAppearance.SmoothDirection = beeAppearance.SmoothPosition - oldSmoothPos;

            float size = beeAppearance.Size;
            float3 scale = new float3(size, size, size);
            float velocityMagnitude = math.sqrt(velocity.v.x * velocity.v.x + velocity.v.y * velocity.v.y + velocity.v.z * velocity.v.z);
            float stretch = math.max(1f, velocityMagnitude * SpeedStretch);
            scale.z *= stretch;
            scale.x /= (stretch - 1f) / 5f + 1f;
            scale.y /= (stretch - 1f) / 5f + 1f;
            nonUniformScale.Value = scale;

            if (beeAppearance.SmoothDirection.x != 0 || beeAppearance.SmoothDirection.y != 0 || beeAppearance.SmoothDirection.z != 0)
            {
                rotation.Value = Quaternion.LookRotation(beeAppearance.SmoothDirection);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new BeeScalingSystemJob
        {
            DeltaTime = Time.fixedDeltaTime,
            RotationStiffness = RotationStiffness,
            SpeedStretch = SpeedStretch
        };

        return job.Schedule(this, inputDependencies);
    }
}