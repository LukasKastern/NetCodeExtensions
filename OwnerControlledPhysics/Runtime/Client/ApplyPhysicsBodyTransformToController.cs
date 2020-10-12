using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateInGroup(typeof( ClientSimulationSystemGroup ))] //We only to this on the client since the server directly interpolates from the snapshots.
    public class ApplyPhysicsBodyTransformToController : SystemBase
    {
        protected override void OnUpdate( )
        {
            // ReSharper disable once RedundantAssignment
            Entities.WithAll<LocalToWorld, HasAuthorityOverPhysicsBody>().ForEach( ( Entity ent, in PhysicsBodyReference reference ) =>
            {
                var localToWorld = GetComponent<LocalToWorld>( reference.Value );
                SetComponent( ent, localToWorld );
                SetComponent( ent, new Translation( )
                {
                    Value = localToWorld.Position
                } );
                SetComponent( ent, new Rotation( )
                {
                    Value = localToWorld.Rotation
                } );
                
            } ).Schedule( );
        }
    }
}