using Unity.Mathematics;

public class GridHelper
{
    public static float3 SnapPointToGroundGrid(float2 groundBoundaries, float cellSize, float3 point)
    {
        var xDistance = math.abs(point.x - groundBoundaries.x);
        var zDistance = math.abs(point.z - groundBoundaries.y);
        var horizontalResourceCount = math.trunc(xDistance / cellSize);
        var verticalResourceCount = math.trunc(zDistance / cellSize);
        var x = groundBoundaries.x - horizontalResourceCount * cellSize;
        var z = groundBoundaries.y - verticalResourceCount * cellSize;

        return new float3(x, point.y, z);
    }
}