using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateInGroup(typeof( InitializationSystemGroup ))]
    public class ChangePhysicsBodyAuthorityState : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem m_barrier;

        protected override void OnCreate( )
        {
            m_barrier = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( );
        }

        protected override void OnUpdate( )
        {
            var commandBuffer = m_barrier.CreateCommandBuffer( );
            
            Entities.WithAll<HasAuthorityOverPhysicsBody, ShouldSimulatePhysicsBody>( ).ForEach( ( in PhysicsBodyReference reference, in PhysicsBodyVelocity velocity ) =>
            {
                if ( HasComponent<PhysicsVelocity>( reference.Value ) ) 
                    return;

                commandBuffer.AddComponent( reference.Value, velocity.Value );

            } ).Schedule( );
            
            Entities.WithNone<HasAuthorityOverPhysicsBody, ShouldSimulatePhysicsBody>( ).ForEach( ( in PhysicsBodyReference reference ) =>
            {
                if ( !HasComponent<PhysicsVelocity>( reference.Value ) ) 
                    return;

                commandBuffer.RemoveComponent<PhysicsVelocity>( reference.Value );
            } ).Schedule( );

            m_barrier.AddJobHandleForProducer( Dependency );
        }
    }

    internal class DetermineIfWeShouldSimulateThePhysicBody : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem m_barrier;

        private const float KInterpolationDelay = 0.25f;
        
        private ServerSimulationSystemGroup m_serverSimulationSystemGroup;
        private ClientSimulationSystemGroup m_clientSimulationSystemGroup;
        
        protected override void OnCreate( )
        {
            m_serverSimulationSystemGroup = World.GetExistingSystem<ServerSimulationSystemGroup>( );
            m_clientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>( );
            m_barrier = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( );
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

            
            var commandBuffer = m_barrier.CreateCommandBuffer( );

            commandBuffer.RemoveComponent<ShouldSimulatePhysicsBody>( GetEntityQuery(
            typeof( ShouldSimulatePhysicsBody ), ComponentType.Exclude<HasAuthorityOverPhysicsBody>( ) ) );

            Entities.WithAll<HasAuthorityOverPhysicsBody>( ).WithNone<ShouldSimulatePhysicsBody>( ).ForEach( ( Entity ent, ref DynamicBuffer<PhysicsSnapshotBufferElement> snapshots ) =>
            {
                for ( int i = 0; i < snapshots.Length; ++i )
                {
                    if ( interpolationTime < snapshots[i].Time ) 
                    {
                        return; //Don't start simulating we are still interpolating snapshots.
                    }
                }

                commandBuffer.AddComponent<ShouldSimulatePhysicsBody>( ent );
                
            } ).Schedule( );

            m_barrier.AddJobHandleForProducer( Dependency );
        }
    }

    public struct ShouldSimulatePhysicsBody : IComponentData { }
    
    public struct PhysicsBodyVelocity : IComponentData
    {
        public PhysicsVelocity Value;
    }
}