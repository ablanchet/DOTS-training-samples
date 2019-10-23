using Unity.Entities;
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
            m_SpawnTimer += Time.deltaTime;
            while (m_SpawnTimer > 1f / config.spawnRate)
            {
                m_SpawnTimer -= 1f / config.spawnRate;
                var pos = MouseRaycaster.worldMousePosition;

                var instance = PostUpdateCommands.Instantiate(config.resourcePrefab);
                PostUpdateCommands.SetComponent(instance, new Translation { Value = pos });
                PostUpdateCommands.AddComponent(instance, new ResourceFallingTag());
            }
        }
    }
}