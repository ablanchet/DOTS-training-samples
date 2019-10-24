using Unity.Entities;
using UnityEngine;

public class ResourceAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ResourceFallingTag());
        dstManager.AddComponentData(entity, new ResourceData());
    }
}

struct ResourceData : IComponentData
{
    public bool held;
    public bool dying;
    public Entity holder;
}

public struct ResourceFallingTag : IComponentData
{
}

public struct TargetCell : IComponentData
{
    public int cellIdx;
}