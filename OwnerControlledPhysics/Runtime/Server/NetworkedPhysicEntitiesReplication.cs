using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace OwnerControlledPhysics.Runtime
{
    public struct MassFromPhysicsBody : IComponentData
    {
        public PhysicsMass Value;
    }
    
    [UpdateInGroup(typeof( GhostSimulationSystemGroup ))]
    [UpdateAfter(typeof( GhostUpdateSystem ))]
    [UpdateBefore(typeof( GhostPredictionSystemGroup ))]
    internal class NetworkedPhysicEntitiesReplication : SystemBase
    {
        private const float KInterpolationDelay = 0.25f;
        
        private ServerSimulationSystemGroup m_serverSimulationSystemGroup;
        private ClientSimulationSystemGroup m_clientSimulationSystemGroup;
        
        protected override void OnCreate( )
        {
            m_serverSimulationSystemGroup = World.GetExistingSystem<ServerSimulationSystemGroup>( );
            m_clientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>( );
        }

        protected override void OnUpdate( )
        {
            var tickRate = default( ClientServerTickRate );
            tickRate.ResolveDefaults( );

            if ( HasSingleton<ClientServerTickRate>( ) ) 
                tickRate = GetSingleton<ClientServerTickRate>( );

            var interpolationTime = 0f;
            
            if ( m_clientSimulationSystemGroup != null )
            {
                var interpolationTick = m_clientSimulationSystemGroup.InterpolationTick;
                var interpolationTickFraction = m_clientSimulationSystemGroup.InterpolationTickFraction;

                interpolationTime = interpolationTick / ( float ) tickRate.SimulationTickRate + interpolationTickFraction / tickRate.SimulationTickRate - KInterpolationDelay;
            }

            else
            {
                interpolationTime = m_serverSimulationSystemGroup.ServerTick / ( float ) tickRate.SimulationTickRate;
            }


            Entities.WithNone<HasAuthorityOverPhysicsBody>().ForEach( ( ref Translation translation, ref Rotation rotation, ref PhysicsBodyVelocity velocity, in DynamicBuffer<PhysicsSnapshotBufferElement> interpolationSnapshots, in MassFromPhysicsBody physicsBody ) =>
            {
                if ( interpolationSnapshots.Length == 0 ) 
                {
                    //No state in history.
                    return;
                } 
                
                var idxOfSnapshotToInterpolateFrom = -1;
                var idxOfSnapshotToInterpolateTo = -1;
                
                for ( int i = 0; i < interpolationSnapshots.Length; ++i )
                {
                    if ( interpolationSnapshots[i].Time > interpolationTime )
                    {
                        idxOfSnapshotToInterpolateTo = i;
                    }

                    else
                    {
                        idxOfSnapshotToInterpolateFrom = i;
                        break;
                    }
                }
                
                if ( idxOfSnapshotToInterpolateFrom != -1 && idxOfSnapshotToInterpolateTo == -1  )  
                {

                    //No snapshots to interpolate to. Extrapolate from latest tick.
                    
                    var fromSnapshot = interpolationSnapshots[idxOfSnapshotToInterpolateFrom];

                    var timeSinceSnapshot = interpolationTime - fromSnapshot.Time;

                    var extrapolatedRigidTransform = GraphicalSmoothingUtility.Extrapolate( fromSnapshot.Value.GetRigidTransform( ), fromSnapshot.Value.Velocity, physicsBody.Value, timeSinceSnapshot );

                    translation.Value = extrapolatedRigidTransform.pos;
                    rotation.Value = extrapolatedRigidTransform.rot;
                    velocity.Value = fromSnapshot.Value.Velocity;
                }
                
                else if( idxOfSnapshotToInterpolateFrom != -1 && idxOfSnapshotToInterpolateTo != -1 )
                {
                    //Got snapshots to interpolate between.

                    var fromSnapshot = interpolationSnapshots[idxOfSnapshotToInterpolateFrom];
                    var targetSnapshot = interpolationSnapshots[idxOfSnapshotToInterpolateTo];

                    var fractionToTarget = math.unlerp( fromSnapshot.Time, targetSnapshot.Time, interpolationTime );

                    var fromRigidbodyTransform = new RigidTransform
                    {
                        pos = fromSnapshot.Value.Position,
                        rot = fromSnapshot.Value.Rotation,
                    };

                    var toRigidbodyTransform = new RigidTransform
                    {
                        pos = targetSnapshot.Value.Position,
                        rot = targetSnapshot.Value.Rotation
                    };
                    
                    var current = GraphicalSmoothingUtility.Interpolate( fromRigidbodyTransform, toRigidbodyTransform, fractionToTarget );

                    fromSnapshot.Value.Position = current.pos;
                    fromSnapshot.Value.Rotation = current.rot;
                    
                    translation.Value = fromSnapshot.Value.Position;
                    rotation.Value = fromSnapshot.Value.Rotation;
                    velocity.Value = fromSnapshot.Value.Velocity;
                }

            } ).Schedule( );
        }
    }
}