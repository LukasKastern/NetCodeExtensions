using Unity.NetCode;

namespace OwnerControlledPhysics.Runtime
{
    public struct PhysicsSnapshotRpc : IRpcCommand
    {
        public PhysicsSnapshot Snapshot;
        public float SampledForServerTime;
        public int GhostHash;
    }
}