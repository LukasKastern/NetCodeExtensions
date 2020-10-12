using Unity.Entities;

namespace OwnerControlledPhysics.Runtime
{
    internal struct PhysicsSnapshotSyncData : IComponentData
    {
        public float LastTimeWeSendData;
        public float SendIntervalInSeconds;
    }
}