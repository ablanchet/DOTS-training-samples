using Unity.Entities;

struct ResourceData: IComponentData {
    public bool held;
    public bool dying;
    public Entity holder;
}