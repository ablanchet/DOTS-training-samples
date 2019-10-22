using Unity.Entities;

struct FlightTarget: IComponentData {
    public Entity entity;
    public bool isResource; //if false then target is enemy
}
