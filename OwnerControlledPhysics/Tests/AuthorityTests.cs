using NUnit.Framework;
using OwnerControlledPhysics.Runtime;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace NetcodeExtensions.OwnerControlledPhysics
{
    public class AuthorityTests 
    {
        [Test]
        public void AssertServerAuthorityDoesGetAssignedCorrectly()
        {
            using ( var serverWorld = new World( "Test World" ) )
            {
                var simulationBodyOwner = serverWorld.EntityManager.CreateEntity( );
            
                serverWorld.EntityManager.AddComponentData( simulationBodyOwner, new SimulationBodyPrefabReference( )
                {
                    Value = Entity.Null
                } );

                serverWorld.CreateSystem<EndInitializationEntityCommandBufferSystem>( );
                serverWorld.CreateSystem<AssignPhysicsBodyAuthorityFlagServer>();

                //No Owner reference set should have authority

                {
                    for ( int i = 0; i < 5; ++i )
                    {
                        serverWorld.GetExistingSystem<AssignPhysicsBodyAuthorityFlagServer>( ).Update( );
                        serverWorld.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );
                    }


                    var hasAuthority = serverWorld.EntityManager.HasComponent<HasAuthorityOverPhysicsBody>( simulationBodyOwner );
                    Assert.IsTrue( hasAuthority );
                }
            
            
                //Set owner reference to invalid owner

                {
                    serverWorld.EntityManager.AddComponentData( simulationBodyOwner, new SimulationBodyOwner( )
                    {
                        Value = -1
                    } );
                
                    for ( int i = 0; i < 5; ++i )
                    {
                        serverWorld.GetExistingSystem<AssignPhysicsBodyAuthorityFlagServer>( ).Update( );
                        serverWorld.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );
                    }



                    var hasAuthority = serverWorld.EntityManager.HasComponent<HasAuthorityOverPhysicsBody>( simulationBodyOwner );
                    Assert.IsTrue( hasAuthority );
                }


                //Set owner reference to valid owner
                {
                    var owner = serverWorld.EntityManager.CreateEntity( );
                    serverWorld.EntityManager.AddComponentData( owner, new NetworkIdComponent( )
                    {
                        Value = 1
                    } );
                    
                
                    serverWorld.EntityManager.AddComponentData( simulationBodyOwner, new SimulationBodyOwner( )
                    {
                        Value = 1 
                    } );
                
                    for ( int i = 0; i < 5; ++i )
                    {
                        serverWorld.GetExistingSystem<AssignPhysicsBodyAuthorityFlagServer>( ).Update( );
                        serverWorld.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );
                    }



                    var hasAuthority = serverWorld.EntityManager.HasComponent<HasAuthorityOverPhysicsBody>( simulationBodyOwner );
                    Assert.IsFalse( hasAuthority );
                }
            
            }
        }

        [Test]
        public void AssertClientAuthorityDoesGetAssignedCorrectly( )
        {
            using ( var clientWorld = new World( "Test World" ) )
            {
                var simulationBodyOwner = clientWorld.EntityManager.CreateEntity( );
                var clientConnectionEnt = clientWorld.EntityManager.CreateEntity( );
                
                clientWorld.EntityManager.AddComponentData( clientConnectionEnt, new NetworkIdComponent( )
                {
                    Value = 1
                } );
            
                clientWorld.EntityManager.AddComponentData( simulationBodyOwner, new SimulationBodyPrefabReference( )
                {
                    Value = Entity.Null
                } );

                clientWorld.EntityManager.AddBuffer<PhysicsSnapshotBufferElement>( simulationBodyOwner );

                clientWorld.CreateSystem<EndInitializationEntityCommandBufferSystem>( );
                clientWorld.CreateSystem<AssignPhysicsBodyAuthorityFlagClient>( );

                //No Owner reference set client shouldn't have authority

                {
                    for ( int i = 0; i < 5; ++i )
                    {
                        clientWorld.GetExistingSystem<AssignPhysicsBodyAuthorityFlagClient>( ).Update( );
                        clientWorld.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );
                    }


                    var hasAuthority = clientWorld.EntityManager.HasComponent<HasAuthorityOverPhysicsBody>( simulationBodyOwner );
                    Assert.IsFalse( hasAuthority );
                }


                //Owner reference set but not this client

                {
                    clientWorld.EntityManager.AddComponentData( simulationBodyOwner, new SimulationBodyOwner( )
                    {
                        Value = -1
                    } );

                    for ( int i = 0; i < 5; ++i )
                    {
                        clientWorld.GetExistingSystem<AssignPhysicsBodyAuthorityFlagClient>( ).Update( );
                        clientWorld.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );
                    }



                    var hasAuthority = clientWorld.EntityManager.HasComponent<HasAuthorityOverPhysicsBody>( simulationBodyOwner );
                    Assert.IsFalse( hasAuthority );
                }


                //Owner reference set to this client
                {
                
                    clientWorld.EntityManager.AddComponentData( simulationBodyOwner, new SimulationBodyOwner( )
                    {
                        Value = 1
                    } );

                    for ( int i = 0; i < 5; ++i )
                    {
                        clientWorld.GetExistingSystem<AssignPhysicsBodyAuthorityFlagClient>( ).Update( );
                        clientWorld.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );
                    }



                    var hasAuthority = clientWorld.EntityManager.HasComponent<HasAuthorityOverPhysicsBody>( simulationBodyOwner );
                    Assert.IsTrue( hasAuthority );
                }
            }
        }
    
        [Test]
        public void AssertPhysicsBodyDoesGetCleanedUp( )
        {
            using ( var world = new World( "Test World" ) )
            {
                var simulationBodyOwner = world.EntityManager.CreateEntity( typeof(Translation), typeof(Rotation), typeof(SimulationBodyPrefabReference) );

                var physicsBody = world.EntityManager.CreateEntity( typeof( Prefab ), typeof(Translation), typeof(Rotation) );
            
                world.EntityManager.AddComponentData( simulationBodyOwner, new SimulationBodyPrefabReference( )
                {
                    Value = physicsBody
                } );

                world.CreateSystem<EndInitializationEntityCommandBufferSystem>( );
                world.CreateSystem<SpawnPhysicBodies>( );

                world.GetExistingSystem<SpawnPhysicBodies>( ).Update( );
                world.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );

                Assert.IsTrue( world.EntityManager.HasComponent<PhysicsBodyReference>( simulationBodyOwner ) );

                var physicsBodyReference = world.EntityManager.GetComponentData<PhysicsBodyReference>( simulationBodyOwner ).Value;
                Assert.IsTrue( world.EntityManager.Exists( physicsBodyReference ) );

                world.EntityManager.DestroyEntity( simulationBodyOwner );

                world.GetExistingSystem<SpawnPhysicBodies>( ).Update( );
                world.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( ).Update( );

                Assert.IsFalse( world.EntityManager.HasComponent<SimulationBodyPrefabReference>( simulationBodyOwner ) );
                Assert.IsFalse( world.EntityManager.HasComponent<PhysicsBodyReference>( simulationBodyOwner ) );
                Assert.IsFalse( world.EntityManager.Exists( physicsBodyReference ) );
            }
        }
    }
}
