using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateInGroup(typeof( SimulationSystemGroup ))]
    [UpdateAfter(typeof( GhostUpdateSystem ))]
    [UpdateBefore(typeof( TransformSystemGroup ))]
    public class UpdatePhysicsBodyData : SystemBase
    {
        protected override void OnUpdate( )
        {
            Entities.WithNone<HasAuthorityOverPhysicsBody>( ).WithAll<Translation, Rotation>().ForEach( ( Entity ent, in PhysicsBodyReference bodyReference ) =>
            {
                SetComponent( bodyReference.Value, GetComponent<Translation>( ent ) );
                SetComponent( bodyReference.Value, GetComponent<Rotation>( ent ) );
            } ).Schedule( );
        }
    }
}