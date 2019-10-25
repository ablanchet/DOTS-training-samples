using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BeeManager : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public Mesh beeMesh;
    public GameObject beePrefab0;
    public GameObject beePrefab1;
    public GameObject resourcePrefab;
    public Material beeMaterial;
    public Color[] teamColors;
    public float minBeeSize;
    public float maxBeeSize;
    public float speedStretch;
    public float rotationStiffness;
    [Space(10)] [Range(0f, 1f)] public float aggression;
    public float flightJitter;
    public float teamAttraction;
    public float teamRepulsion;
    [Range(0f, 1f)] public float damping;
    public float chaseForce;
    public float carryForce;
    public float grabDistance;
    public float attackDistance;
    public float attackForce;
    public float hitDistance;
    public float maxSpawnSpeed;
    [Space(10)] public int startBeeCount;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        //spawn bees
        var spawner = dstManager.World.GetExistingSystem<BeeSpawner>();
        spawner.SetPrototypes(conversionSystem.GetPrimaryEntity(beePrefab0), conversionSystem.GetPrimaryEntity(beePrefab1));
        spawner.maxBeeSize = maxBeeSize;
        spawner.minBeeSize = minBeeSize;
        spawner.maxSpawnSpeed = maxSpawnSpeed;

        var teamSizes = new [] { startBeeCount / 2, startBeeCount / 2 };

        var spawnRequest = dstManager.CreateArchetype(typeof(Translation), typeof(BeeSpawnRequest));

        for (sbyte teamindex = 0; teamindex < teamSizes.Length; ++teamindex)
        {
            var pos = Vector3.right * (-Field.size.x * .4f + Field.size.x * .8f * teamindex);
            using (var spawnEntities = new NativeArray<Entity>(teamSizes[teamindex], Allocator.Temp))
            {
                dstManager.CreateEntity(spawnRequest, spawnEntities);
                for (var x = 0; x < spawnEntities.Length; ++x)
                {
                    dstManager.SetComponentData(spawnEntities[x], new BeeSpawnRequest() { Team = teamindex });
                    dstManager.SetComponentData(spawnEntities[x], new Translation() { Value = pos });
                }
            }
        }

        //set up bee flight parameters
        BeeBehaviour behaviour = dstManager.World.GetExistingSystem<BeeBehaviour>();
        behaviour.TeamAttraction = teamAttraction;
        behaviour.TeamRepulsion = teamRepulsion;
        behaviour.FlightJitter = flightJitter;
        behaviour.Damping = damping;
        behaviour.ChaseForce = chaseForce;
        behaviour.GrabDistance = grabDistance;
        behaviour.AttackForce = attackForce;
        behaviour.CarryForce = carryForce;
        behaviour.AttackDistance = attackDistance;
        behaviour.ResourceSize = resourcePrefab.transform.localScale.x;

        //set up target finding
//        FindTargetSystem findTargetSystem = dstManager.World.GetExistingSystem<FindTargetSystem>();
//        findTargetSystem.Aggression = aggression;

        //appearance of bees
        var beeAppearanceSystem = dstManager.World.GetExistingSystem<BeeAppearanceSystem>();
        beeAppearanceSystem.RotationStiffness = rotationStiffness;
        beeAppearanceSystem.SpeedStretch = speedStretch;
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(beePrefab0);
        referencedPrefabs.Add(beePrefab1);
        referencedPrefabs.Add(resourcePrefab);
    }
}