using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace OwnerControlledPhysics.Runtime
{
    /// <summary>
    /// Data used to interpolate vehicles on the server.
    /// </summary>
    public struct PhysicsSnapshot : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public PhysicsVelocity Velocity;
        public RigidTransform GetRigidTransform( ) => new RigidTransform( Rotation, Position );
    }
}