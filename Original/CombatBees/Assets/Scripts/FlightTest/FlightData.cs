using Unity.Entities;

struct FlightData: IComponentData {
    public float speed;
    public float chaseForce;
    public float jitterSpeed;
    public float grabDistance;
}
