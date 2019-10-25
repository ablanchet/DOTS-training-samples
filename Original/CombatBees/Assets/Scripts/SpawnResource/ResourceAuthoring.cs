using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public class ResourceAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ResourceFallingComponent() { IsFalling = true }) ;
        dstManager.AddComponentData(entity, new ResourceData { 
            held = false,
            dying = false,
            holder = Entity.Null,
            velocity = new float3(0,0,0) 
        });
        dstManager.AddComponent<FollowEntity>(entity);
    }
}

struct ResourceData : IComponentData
{
    public bool held;
    public bool dying;
    public Entity holder;
    public float3 velocity;
}

public struct ResourceFallingComponent : IComponentData
{
    public bool IsFalling;
}

public struct TargetCell : IComponentData
{
    public int cellIdx;
}