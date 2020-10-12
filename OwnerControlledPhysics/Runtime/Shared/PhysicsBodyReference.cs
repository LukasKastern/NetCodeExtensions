using Unity.Entities;

namespace OwnerControlledPhysics.Runtime
{
    public struct PhysicsBodyReference : ISystemStateComponentData
    {
        public Entity Value;
    }
}