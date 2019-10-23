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
            var config = GetSingleton<ResourceSpawnerConfiguration>();
            var resourceSize = config.resourceScale.x;
            m_SpawnTimer += Time.deltaTime;
            while (m_SpawnTimer > 1f / config.spawnRate)
            {
                m_SpawnTimer -= 1f / config.spawnRate;
                var pos = MouseRaycaster.worldMousePosition;

                var spannedX = (math.round(pos.x / resourceSize) * resourceSize) + math.sign(pos.x) * -1 * (resourceSize / 2);
                var spannedZ = (math.round(pos.z / resourceSize) * resourceSize) + math.sign(pos.z) * -1 * (resourceSize / 2);

                var instance = PostUpdateCommands.Instantiate(config.resourcePrefab);
                PostUpdateCommands.SetComponent(instance, new Translation { Value = new float3(spannedX, pos.y, spannedZ) });
                PostUpdateCommands.AddComponent(instance, new ResourceFallingTag());
                PostUpdateCommands.AddComponent(instance, new ResourceData { held = false });
            }
        }
    }
}