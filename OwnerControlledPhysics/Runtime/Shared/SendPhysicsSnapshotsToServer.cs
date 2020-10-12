using Syncing.GhostLookup;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateInGroup(typeof( SimulationSystemGroup ))]
    [UpdateAfter(typeof( SamplePhysicsSnapshotFromPhysicBodies ))]
    internal class SendPhysicsSnapshotsToServer : SystemBase
    {
        private ClientSimulationSystemGroup m_clientSimulationSystemGroup;
        private ServerSimulationSystemGroup m_serverSimulationSystemGroup;
        private EndSimulationEntityCommandBufferSystem m_barrier;

        private ValidatePhysicEntitySnapshots m_validatePhysicEntitySnapshots;
        
        private GhostLookupSystem m_ghostLookupSystem;
        
        private bool m_isServer;
        
        private uint m_simulationTick;
        
        protected override void OnCreate( )
        {
            m_validatePhysicEntitySnapshots = World.GetExistingSystem<ValidatePhysicEntitySnapshots>( );
            m_clientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>( );
            m_serverSimulationSystemGroup = World.GetExistingSystem<ServerSimulationSystemGroup>( );
            m_barrier = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>( );
            m_ghostLookupSystem = World.GetExistingSystem<GhostLookupSystem>( );
            
            m_isServer = m_serverSimulationSystemGroup != null;
        }

        protected override void OnUpdate( )
        {
            ClientServerTickRate tickRate = default;

            tickRate.ResolveDefaults( );


            if ( HasSingleton<ClientServerTickRate>( ) ) tickRate = GetSingleton<ClientServerTickRate>( );
            
            //Interpolation delay should be simulation send interval in seconds + rtt in seconds + extra buffer  
            
            var isServer = m_isServer;
            var commandBuffer = m_barrier.CreateCommandBuffer( );
            
            var tick = m_isServer ? m_serverSimulationSystemGroup.ServerTick : m_clientSimulationSystemGroup.ServerTick;
            
            var serverTickInSeconds = tick / ( float ) tickRate.SimulationTickRate;

            if ( !m_isServer )
                serverTickInSeconds += m_clientSimulationSystemGroup.ServerTickFraction / tickRate.SimulationTickRate;

            var hashLookup = m_ghostLookupSystem.HashLookup;

            Dependency = JobHandle.CombineDependencies( Dependency, m_validatePhysicEntitySnapshots.GetOutputDependency( ) );

            m_validatePhysicEntitySnapshots.AddInputDependency( Dependency );

            var snapshotsToStoreLocally = m_validatePhysicEntitySnapshots.GetSnapshots( );
            
            Entities.WithAll<HasAuthorityOverPhysicsBody, ShouldSimulatePhysicsBody>().ForEach( ( Entity ent, ref PhysicsSnapshotSyncData sendData, in PhysicsSnapshot snapshot, in PhysicsBodyReference bodyReference, in GhostComponent ghostComponent ) =>
            {
                if ( serverTickInSeconds - sendData.LastTimeWeSendData < sendData.SendIntervalInSeconds )
                    return;

                if ( !hashLookup.TryGetValue( ent, out var hash ) ) 
                    return; //Not registered yet or not a valid ghost.

                sendData.LastTimeWeSendData = serverTickInSeconds;

                var rpcEnt = commandBuffer.CreateEntity( );
                
                var physicsSnapshotRpc = new PhysicsSnapshotRpc
                {
                    Snapshot = snapshot,
                    SampledForServerTime = serverTickInSeconds,
                    GhostHash = hash,
                };
                commandBuffer.AddComponent( rpcEnt, physicsSnapshotRpc );

                if ( !isServer )
                    commandBuffer.AddComponent<SendRpcCommandRequestComponent>( rpcEnt );
                else
                    commandBuffer.AddComponent<ReceiveRpcCommandRequestComponent>( rpcEnt );

                snapshotsToStoreLocally.Add( physicsSnapshotRpc );

            } ).Schedule( );

            m_barrier.AddJobHandleForProducer( Dependency );
        }
    }
}