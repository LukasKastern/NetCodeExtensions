using OwnerControlledPhysics.Runtime;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;

namespace NetcodeExtensions.OwnerControlledPhysics
{
    [ConverterVersion("Bob", 2)]
    [UpdateAfter( typeof( PhysicsBodyConversionSystem ) )]
    public sealed class OwnerControlledPhysicsBodyConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate( )
        {
            Entities.ForEach( ( OwnerControlledPhysicsBody ownerControlledPhysicsBody ) =>
            {
                var entity = GetPrimaryEntity( ownerControlledPhysicsBody );
                var physicsBodyPrefab = GetPrimaryEntity( ownerControlledPhysicsBody.physicsBody );
            
                DstEntityManager.AddComponent<PhysicsSnapshot>( entity );
                DstEntityManager.AddComponent<PhysicsSnapshotBufferElement>( entity );

                DstEntityManager.AddComponentData( entity, new MassFromPhysicsBody
                {
                    Value = DstEntityManager.GetComponentData<PhysicsMass>( physicsBodyPrefab )
                } );
            
                DstEntityManager.AddComponentData( entity, new PhysicsBodyVelocity
                {
                    Value = DstEntityManager.GetComponentData<PhysicsVelocity>( physicsBodyPrefab )
                } );
            
                DstEntityManager.AddComponentData( entity, new PhysicsSnapshotSyncData
                {
                    SendIntervalInSeconds = 1f / ownerControlledPhysicsBody.stateUpdatesToSendPerSecond
                } );
            
                DstEntityManager.AddComponentData( entity, new SimulationBodyPrefabReference
                {
                    Value = physicsBodyPrefab
                } );

                DstEntityManager.AddComponent<SimulationBodyOwner>( entity );
            } );
        }
    }
}