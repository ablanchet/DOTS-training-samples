using Unity.Entities;

struct ResourceData: IComponentData {
    public bool held;
    public Entity holder;
}