using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public class GridHelper
{
    public static float resourceSize;
    public static float resourceHeight;
    public static float resourceHeightScale;
    public static float2 gridDimensions;
    public static float2 gridBoundaries;
    public static float gridHeight;

    public static NativeArray<short> stackHeights;
    public static NativeArray<Entity> stackReferences;

    public static float3 SnapPointToGroundGrid(float3 point)
    {
        var xDistance = math.abs(point.x - gridBoundaries.x);
        var zDistance = math.abs(point.z - gridBoundaries.y);
        var horizontalResourceCount = math.trunc(xDistance / resourceSize);
        var verticalResourceCount = math.trunc(zDistance / resourceSize);
        var x = gridBoundaries.x - horizontalResourceCount * resourceSize + resourceSize / 2;
        var z = gridBoundaries.y - verticalResourceCount * resourceSize + resourceSize / 2;

        return new float3(x, point.y, z);
    }

    public static int GetIndexOf(float2 point)
    {
        var x = math.trunc((point.x - resourceSize / 2 + gridBoundaries.x) / resourceSize);
        var y = math.trunc((point.y - resourceSize / 2 + gridBoundaries.y) / resourceSize);

        return math.clamp((int) (y * gridDimensions.x + x), 0, (int) (gridDimensions.x * gridDimensions.y) - 1);
    }
}