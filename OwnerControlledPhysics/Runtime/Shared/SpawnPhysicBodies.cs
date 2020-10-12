using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace OwnerControlledPhysics.Runtime
{
    public struct PhysicsBodyOwner : IComponentData
    {
        public Entity Value;
    }
    
    [UpdateInGroup(typeof( InitializationSystemGroup ))]
    public class SpawnPhysicBodies : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem m_initializationEntityCommandBufferSystem;
        private bool m_isServer;
        
        protected override void OnCreate( )
        {
            m_initializationEntityCommandBufferSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( );
            m_isServer = World.GetExistingSystem<ServerSimulationSystemGroup>( ) != null;
        }

        protected override void OnUpdate( )
        {
            var commandBuffer = m_initializationEntityCommandBufferSystem.CreateCommandBuffer( );
            var isServer = m_isServer;
            
            Entities.WithNone<PhysicsBodyReference>( ).ForEach( ( Entity ent,
                in SimulationBodyPrefabReference serverOwnedClientControlledPhysicsController, in Translation translation, in Rotation rotation ) =>
            {
                var physicsBodyReference = new PhysicsBodyReference( )
                {
                    Value = commandBuffer.Instantiate( serverOwnedClientControlledPhysicsController.Value )
                };
                
                commandBuffer.AddComponent( ent, physicsBodyReference );
                commandBuffer.AddComponent(physicsBodyReference.Value ,new PhysicsBodyOwner
                {
                    Value = ent
                });
                
                commandBuffer.SetComponent( physicsBodyReference.Value, translation );
                commandBuffer.SetComponent( physicsBodyReference.Value, rotation );
                
            } ).Schedule( );

            Entities.WithNone<SimulationBodyPrefabReference>( ).ForEach( ( Entity ent, in PhysicsBodyReference bodyReference ) =>
            {
                commandBuffer.DestroyEntity( bodyReference.Value );
                commandBuffer.RemoveComponent<PhysicsBodyReference>( ent );
            } ).Schedule( );
            
            m_initializationEntityCommandBufferSystem.AddJobHandleForProducer( Dependency );
        }
    }
}