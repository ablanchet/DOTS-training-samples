using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ResourceGroundAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    void OnDrawGizmos()
    {
        var transform = gameObject.GetComponent<Transform>();
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = dstManager.GetComponentData<NonUniformScale>(entity);
        var translation = dstManager.GetComponentData<Translation>(entity);

        dstManager.AddComponentData(entity, new ResourceGroundComponent
        {
            Dimensions = scale.Value,
            Y = translation.Value.y
        });
    }
}

public struct ResourceGroundComponent : IComponentData
{
    public float Y;
    public float3 Dimensions;
}