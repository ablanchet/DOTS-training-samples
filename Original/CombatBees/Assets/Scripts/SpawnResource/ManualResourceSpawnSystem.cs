using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ManualResourceSpawnSystem : ComponentSystem
{
    float m_SpawnTimer;

    protected override void OnUpdate()
    {
        if (MouseRaycaster.isMouseTouchingField && Input.GetKey(KeyCode.Mouse0))
        {
            var ground = GetSingletonEntity<ResourceGroundTag>();
            var groundTranslation = EntityManager.GetComponentData<Translation>(ground).Value;
            var groundScale = EntityManager.GetComponentData<NonUniformScale>(ground).Value;
            var groundBoundaries = new float2(groundTranslation.x + groundScale.x / 2, groundTranslation.z + groundScale.z / 2);

            var config = GetSingleton<ResourceSpawnerConfiguration>();
            var resourceSize = config.resourceScale.x;
            m_SpawnTimer += Time.deltaTime;
            while (m_SpawnTimer > 1f / config.spawnRate)
            {
                m_SpawnTimer -= 1f / config.spawnRate;
                var pos = AutoResourceSpawnerSystem.SnapPointToGroundGrid(groundBoundaries, resourceSize, MouseRaycaster.worldMousePosition);

                var instance = PostUpdateCommands.Instantiate(config.resourcePrefab);
                PostUpdateCommands.SetComponent(instance, new Translation { Value = pos });
                PostUpdateCommands.AddComponent(instance, new ResourceFallingTag());
                PostUpdateCommands.AddComponent(instance, new ResourceData { held = false });
            }
        }
    }
}