using Syncing.GhostLookup;
using Unity.Entities;
using Unity.Jobs;

namespace OwnerControlledPhysics.Runtime
{
    [UpdateAfter( typeof( ValidatePhysicEntitySnapshots ) )]
    [UpdateInGroup(typeof( SimulationSystemGroup ))]
    internal class StorePhysicEntitySnapshots : SystemBase
    {
        private GhostLookupSystem m_ghostLookupSystem;
        private EndSimulationEntityCommandBufferSystem m_barrier;

        private ValidatePhysicEntitySnapshots m_validatePhysicEntitySnapshots;
        
        protected override void OnCreate( )
        {
            m_validatePhysicEntitySnapshots = World.GetExistingSystem<ValidatePhysicEntitySnapshots>( );
            m_barrier = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>( );
            m_ghostLookupSystem = World.GetExistingSystem<GhostLookupSystem>( );
        }

        protected override void OnUpdate( )
        {
            this.Dependency = JobHandle.CombineDependencies( m_validatePhysicEntitySnapshots.GetOutputDependency( ), Dependency );
            
            var snapshotsToProcess = m_validatePhysicEntitySnapshots.GetSnapshots( );
            
            var ghostLookup = m_ghostLookupSystem.GhostLookup;

            var interpolationBuffersFromEntity = GetBufferFromEntity<PhysicsSnapshotBufferElement>( );

            Job.WithCode( ( ) =>
            {
                while ( snapshotsToProcess.Length > 0 )
                {
                    var snapshotToProcess = snapshotsToProcess[snapshotsToProcess.Length - 1];
                    snapshotsToProcess.RemoveAt( snapshotsToProcess.Length - 1 );

                    if ( !ghostLookup.TryGetValue( snapshotToProcess.GhostHash, out var ghostEnt ) || !interpolationBuffersFromEntity.HasComponent(ghostEnt) ) 
                        continue;

                    if ( GetComponent<PhysicsSnapshotSyncData>( ghostEnt ).LastTimeWeSendData > snapshotToProcess.SampledForServerTime )
                        continue; 

                    var interpolationBuffer = interpolationBuffersFromEntity[ghostEnt];
                    
                    { //Insert snapshot. Snapshots are ordered by time. 

                        int snapshotBeforeTheOneThatWeProcess = 0;

                        for ( int i = 0; i < interpolationBuffer.Length; ++i )
                        {
                            if ( interpolationBuffer[i].Time > snapshotToProcess.SampledForServerTime )
                                snapshotBeforeTheOneThatWeProcess = i + 1;
                            else
                                break;
                        }

                        if ( snapshotBeforeTheOneThatWeProcess == interpolationBuffer.Capacity )
                            continue; //Snapshot to old to be queued. 
                        
                        
                        if ( interpolationBuffer.Length >= interpolationBuffer.Capacity )
                            interpolationBuffer.RemoveAt( interpolationBuffer.Length - 1 ); //Make space for new element

                        interpolationBuffer.Insert( snapshotBeforeTheOneThatWeProcess, new PhysicsSnapshotBufferElement( )
                        {
                            Time = snapshotToProcess.SampledForServerTime,
                            Value = snapshotToProcess.Snapshot
                        } );
                    }
                }
            } ).Schedule( );

            m_barrier.AddJobHandleForProducer( Dependency );
        }

    }
}