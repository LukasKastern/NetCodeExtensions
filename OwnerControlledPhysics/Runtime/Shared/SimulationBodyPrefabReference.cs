using Unity.Entities;

namespace OwnerControlledPhysics.Runtime
{
    public struct SimulationBodyPrefabReference : IComponentData
    {
        public Entity Value;
    }
}
