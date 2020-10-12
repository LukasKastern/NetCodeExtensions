using Unity.Entities;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateInGroup(typeof( SimulationSystemGroup ))]
    [UpdateAfter(typeof( SmoothRigidBodiesGraphicalMotion ))]
    public class SamplePhysicsSnapshotFromPhysicBodies : SystemBase
    {
        protected override void OnUpdate( )
        {
            Entities.ForEach( ( ref PhysicsSnapshot snapshot, in PhysicsBodyReference reference ) =>
            {
                if ( !HasComponent<PhysicsVelocity>( reference.Value ) ) 
                    return;

                var ltw = GetComponent<LocalToWorld>( reference.Value );
                var velocity = GetComponent<PhysicsVelocity>( reference.Value );

                snapshot = new PhysicsSnapshot( )
                {
                    Position = ltw.Position,
                    Rotation = ltw.Rotation,
                    Velocity = velocity
                };
                
            } ).Schedule( );
        }
    }
}