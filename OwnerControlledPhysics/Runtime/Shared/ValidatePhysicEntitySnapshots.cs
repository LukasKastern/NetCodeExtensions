using Syncing.GhostLookup;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateBefore(typeof( GhostSimulationSystemGroup ))]
    [UpdateInGroup(typeof( SimulationSystemGroup ))]
    internal class ValidatePhysicEntitySnapshots : SystemBase
    {
        private NativeList<PhysicsSnapshotRpc> m_validSnapshotsThatHaventBeenProcessedYet;
        private GhostLookupSystem m_ghostLookupSystem;

        private bool m_isServer;

        private EndSimulationEntityCommandBufferSystem m_barrier;

        public NativeList<PhysicsSnapshotRpc> GetSnapshots( ) => m_validSnapshotsThatHaventBeenProcessedYet;
        
        protected override void OnCreate( )
        {
            m_barrier = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>( );
            m_isServer = World.GetExistingSystem<ServerSimulationSystemGroup>( ) != null;
            m_ghostLookupSystem = World.GetExistingSystem<GhostLookupSystem>( );
      
            m_validSnapshotsThatHaventBeenProcessedYet = new NativeList<PhysicsSnapshotRpc>( 32, Allocator.Persistent );
        }

        protected override void OnDestroy( )
        {
            m_validSnapshotsThatHaventBeenProcessedYet.Dispose( );
        }

        protected override void OnUpdate( )
        {
            var ghostLookup = m_ghostLookupSystem.GhostLookup;
            var snapshots = m_validSnapshotsThatHaventBeenProcessedYet;
            var commandBuffer = m_barrier.CreateCommandBuffer( );
            
            if ( m_isServer )
            {
                Entities.ForEach( ( Entity entity, ref PhysicsSnapshotRpc rpc, in ReceiveRpcCommandRequestComponent requestComponent ) =>
                {
                    var isRpcValid = false;

                    if ( ghostLookup.TryGetValue( rpc.GhostHash, out var ghostEnt ) )
                    {
                        var rpcCommandRequestComponent = GetComponent<ReceiveRpcCommandRequestComponent>( entity );

                        var isLocalRpc = requestComponent.SourceConnection == Entity.Null;
                        
                        if ( isLocalRpc ) 
                        {
                            isRpcValid = true;
                        }
                        
                        else if ( HasComponent<SimulationBodyOwner>( ghostEnt ) )
                        {
                            var owner = GetComponent<SimulationBodyOwner>( ghostEnt ).Value;

                            isRpcValid = owner == GetComponent<NetworkIdComponent>( rpcCommandRequestComponent.SourceConnection ).Value;

                            var snapshotAck = GetComponent<NetworkSnapshotAckComponent>( rpcCommandRequestComponent.SourceConnection );
                            
                            rpc.SampledForServerTime += snapshotAck.EstimatedRTT / 1000f / 2f;  
                        }
                    }

                    if ( !isRpcValid )
                    {
                        commandBuffer.DestroyEntity( entity ); //Invalid rpc. Ignore
                        return;
                    }

                    //RPC Is valid. Store and forward it to all clients
                    snapshots.Add( rpc );

                    commandBuffer.RemoveComponent<ReceiveRpcCommandRequestComponent>( entity );
                    commandBuffer.AddComponent<SendRpcCommandRequestComponent>( entity );

                } ).Schedule( );
            }

            else
            { 
                Entities.WithAll<ReceiveRpcCommandRequestComponent>( ).ForEach( ( Entity ent, in PhysicsSnapshotRpc snapshotRpc ) =>
                {
                    if ( ghostLookup.TryGetValue( snapshotRpc.GhostHash, out var ghostEnt ) )
                    {
                        if ( !HasComponent<HasAuthorityOverPhysicsBody>( ghostEnt ) )  
                            snapshots.Add( snapshotRpc ); //We only store snapshots if we don't have authority over the physics body

                    }
                    

                    commandBuffer.DestroyEntity( ent );
                } ).Schedule( );
            }

            m_barrier.AddJobHandleForProducer( Dependency );
        }

        public JobHandle GetOutputDependency( ) => Dependency;

        public void AddInputDependency( JobHandle dependency )
        {
            Dependency = JobHandle.CombineDependencies( Dependency, dependency );
        }
    }
}