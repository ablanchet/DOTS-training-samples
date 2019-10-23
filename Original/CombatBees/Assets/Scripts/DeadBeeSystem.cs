using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(VelocityBasedMovement)), UpdateAfter(typeof(BeeBehaviour))]
public class DeadBeeSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
            
            
            }
}