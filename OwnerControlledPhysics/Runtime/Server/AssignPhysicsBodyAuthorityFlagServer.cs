using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateInGroup(typeof( ServerInitializationSystemGroup ))]
    public class AssignPhysicsBodyAuthorityFlagServer : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem m_barrier;

        private EntityQuery m_bodiesWithoutOwnerComponent;

        private readonly EntityQueryDesc m_bodiesServerControlledWithoutAuthorityFlagAssigned = new EntityQueryDesc( )
        {
            All = new ComponentType[] {typeof( SimulationBodyPrefabReference )},
            None = new ComponentType[] {typeof( SimulationBodyOwner ), typeof( HasAuthorityOverPhysicsBody )}
        };
        
        protected override void OnCreate( )
        {
            m_barrier = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( );
            m_bodiesWithoutOwnerComponent = GetEntityQuery( m_bodiesServerControlledWithoutAuthorityFlagAssigned );
        }

        protected override void OnUpdate( )
        {
            var commandBuffer = m_barrier.CreateCommandBuffer( );

            commandBuffer.AddComponent<HasAuthorityOverPhysicsBody>( m_bodiesWithoutOwnerComponent );

            var networkIds = GetEntityQuery( typeof( NetworkIdComponent ) );
            
            var validOwnerIds = new NativeHashSet<int>( networkIds.CalculateEntityCount( ), Allocator.TempJob );

            Entities.ForEach( ( in NetworkIdComponent networkIdComponent ) =>
            {
                validOwnerIds.Add( networkIdComponent.Value );
            } ).Schedule( );
            
            Entities.ForEach( ( Entity ent, in SimulationBodyOwner bodyOwner ) =>
            {
                var doesServerCurrentlyControlBody = HasComponent<HasAuthorityOverPhysicsBody>( ent );

                var doesClientControlEntity = validOwnerIds.Contains( bodyOwner.Value );
                
                if ( doesClientControlEntity && doesServerCurrentlyControlBody )
                    commandBuffer.RemoveComponent<HasAuthorityOverPhysicsBody>( ent );
                if ( !doesClientControlEntity && !doesServerCurrentlyControlBody )
                    commandBuffer.AddComponent<HasAuthorityOverPhysicsBody>( ent );
            } ).Schedule( );
            
            m_barrier.AddJobHandleForProducer( Dependency );
            validOwnerIds.Dispose( Dependency );
        }
    }
}