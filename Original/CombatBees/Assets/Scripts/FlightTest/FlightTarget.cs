using Unity.Entities;

struct FlightTarget: IComponentData {
    public Entity entity;
    public int type;
}
