using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[UpdateAfter(typeof(SpawnerResourceSystem))]
public class FindTargetSystem : ComponentSystem
{
    private EntityQuery resourceQuery;
    private EntityQuery Team0Query;
    private EntityQuery Team1Query;
    public float Aggression;

    protected override void OnCreate()
    {
        resourceQuery = GetEntityQuery(
            ComponentType.ReadOnly<ResourceData>()
        );
        Team0Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam0>());
        Team1Query = GetEntityQuery(ComponentType.ReadOnly<BeeTeam1>());
    }
    protected override void OnUpdate()
    {
        // Entities.ForEach processes each set of ComponentData on the main thread. This is not the recommended
        // method for best performance. However, we start with it here to demonstrate the clearer separation
        // between ComponentSystem Update (logic) and ComponentData (data).
        // There is no update logic on the individual ComponentData.
        var resourcesData = GetComponentDataFromEntity<ResourceData>();
        var beeStateFromEntity = GetComponentDataFromEntity<BeeState>();

        using (NativeArray<Entity> resourcesList = resourceQuery.ToEntityArray(Allocator.TempJob))
        using (NativeArray<Entity> team0List = Team0Query.ToEntityArray(Allocator.TempJob))
        using (NativeArray<Entity> team1List = Team1Query.ToEntityArray(Allocator.TempJob))
        {

            Entities.ForEach((Entity e, ref FlightTarget target) =>
                {
                    if (target.entity == Entity.Null)
                    {
                        if (UnityEngine.Random.Range(0.0f, 1.0f) < Aggression)
                        {
                            //Aggro - find a victim
                            NativeArray<Entity> enemyList = (EntityManager.HasComponent<BeeTeam0>(e)) ? team1List : team0List;
                            Entity targetCandidate = enemyList[UnityEngine.Random.Range(0, enemyList.Length)];
                            //need to avoid targeting dead beed
                            PostUpdateCommands.SetComponent(e, new FlightTarget { entity = targetCandidate, isResource = false });
                        }
                        else
                        {
                            // Get random resource
                            var resourceEntity = resourcesList[UnityEngine.Random.Range(0, resourcesList.Length)];
                            var resData = resourcesData[resourceEntity];
                            if (!resData.held)
                            {
                                PostUpdateCommands.SetComponent(e, new FlightTarget { entity = resourceEntity, isResource = true });
                            }
                        }
                    }
                    else
                    {
                        // If resoruce, check if its still free
                        if (target.isResource)
                        {
                            var resData = resourcesData[target.entity];

                            if (resData.held)
                            {
                                PostUpdateCommands.SetComponent(e, new FlightTarget());
                            }
                        }
                        else
                        {
                            if (beeStateFromEntity[target.entity].Dead)
                            {
                                target.entity = Entity.Null;
                            }
                        }
                    }
                });
        }
    }
}