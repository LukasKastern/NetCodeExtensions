using Unity.Entities;

namespace OwnerControlledPhysics.Runtime
{
    [InternalBufferCapacity(16)]
    public struct PhysicsSnapshotBufferElement : IBufferElementData
    {
        public PhysicsSnapshot Value;
        public float Time;
    }
}