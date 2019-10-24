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
            var ground = GetSingleton<ResourceGround>();
            var config = GetSingleton<ResourceSpawnerConfiguration>();

            var resourceSize = config.resourceScale.x;
            m_SpawnTimer += Time.deltaTime;
            while (m_SpawnTimer > 1f / config.spawnRate)
            {
                m_SpawnTimer -= 1f / config.spawnRate;
                var pos = GridHelper.SnapPointToGroundGrid(ground.xzMaxBoundaries, resourceSize, MouseRaycaster.worldMousePosition);

                var instance = PostUpdateCommands.Instantiate(config.resourcePrefab);
                PostUpdateCommands.SetComponent(instance, new Translation { Value = pos });
            }
        }
    }
}