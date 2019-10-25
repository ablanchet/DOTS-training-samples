using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(BeeBehaviour))]
public class VelocityBasedMovement : JobComponentSystem
{
    // This declares a new kind of job, which is a unit of work to do.
    // The job is declared as an IJobForEach<Translation, Rotation>,
    // meaning it will process all entities in the world that have both
    // Translation and Rotation components. Change it to process the component
    // types you want.
    //
    // The job is also tagged with the BurstCompile attribute, which means
    // that the Burst compiler will optimize it for the best performance.
    [BurstCompile]
    struct VelocityBasedMovementJob : IJobForEach<Translation, Velocity>
    {
        // Add fields here that your job needs to do its work.
        // For example,
        public float deltaTime;

        public void Execute(ref Translation translation, [ReadOnly] ref Velocity velocity)
        {
            translation.Value += velocity.v * deltaTime;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new VelocityBasedMovementJob();
        job.deltaTime = Time.fixedDeltaTime;
        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
        //     job.deltaTime = UnityEngine.Time.deltaTime;



        // Now that the job is set up, schedule it to be run.
        return job.Schedule(this, inputDependencies);
    }
}