using Unity.Entities;
using Unity.NetCode;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateInGroup(typeof( ClientInitializationSystemGroup ))]
    public class AssignPhysicsBodyAuthorityFlagClient : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem m_barrier;

        private EntityQuery m_bodiesWithoutOwnerComponent;
        
        private readonly EntityQueryDesc m_bodiesServerControlledWithAuthorityFlagAssigned = new EntityQueryDesc( )
        {
            All = new ComponentType[] {typeof( SimulationBodyPrefabReference ), typeof( HasAuthorityOverPhysicsBody )},
            None = new ComponentType[] {typeof( SimulationBodyOwner )}
        };
        
        protected override void OnCreate( )
        {
            m_barrier = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( );
            m_bodiesWithoutOwnerComponent = GetEntityQuery( m_bodiesServerControlledWithAuthorityFlagAssigned );

            RequireSingletonForUpdate<NetworkIdComponent>( );
        }

        protected override void OnUpdate( )
        {
            var commandBuffer = m_barrier.CreateCommandBuffer( );

            var networkId = GetSingleton<NetworkIdComponent>( ).Value;
            
            Entities.ForEach( ( Entity ent, ref DynamicBuffer <PhysicsSnapshotBufferElement> snapshots, in SimulationBodyOwner bodyOwner ) =>
            {
                var doesClientHaveAuthorityFlagAssignedToEntity = HasComponent<HasAuthorityOverPhysicsBody>( ent );
                var shouldHaveFlagAssigned = bodyOwner.Value == networkId;

                if ( shouldHaveFlagAssigned && !doesClientHaveAuthorityFlagAssignedToEntity )
                    commandBuffer.AddComponent<HasAuthorityOverPhysicsBody>( ent );
                if ( !shouldHaveFlagAssigned && doesClientHaveAuthorityFlagAssignedToEntity )
                {
                    commandBuffer.RemoveComponent<HasAuthorityOverPhysicsBody>( ent );
                    snapshots.Clear( );
                }
            } ).Schedule( );
            
            commandBuffer.RemoveComponent<HasAuthorityOverPhysicsBody>( m_bodiesWithoutOwnerComponent );
            
            m_barrier.AddJobHandleForProducer( Dependency );
        }
    }
}