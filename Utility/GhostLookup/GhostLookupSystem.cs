using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Syncing.GhostLookup
{
    /// <summary>
    /// Ghost lookup inspired by Enzis post here: https://forum.unity.com/threads/dots-netcode-0-3-0-released.955890/page-2#post-6266304
    /// </summary>
    [UpdateInGroup(typeof( InitializationSystemGroup ))]
    public class GhostLookupSystem : SystemBase
    {
        public NativeHashMap<int, Entity> GhostLookup;
        public NativeHashMap<Entity, int> HashLookup;

        private EndInitializationEntityCommandBufferSystem m_barrier;

        struct AddedToLookupTables : ISystemStateComponentData { }
        
        protected override void OnCreate()
        {
            m_barrier = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>( );

            GhostLookup = new NativeHashMap<int, Entity>( 512, Allocator.Persistent );
            HashLookup = new NativeHashMap<Entity, int>( 512, Allocator.Persistent );
        }

        protected override void OnDestroy()
        {
            GhostLookup.Dispose();
            HashLookup.Dispose( );
        }

        protected override void OnUpdate()
        {
            var lookup = GhostLookup;
            var hashLookup = HashLookup;
            
            var commandBuffer = m_barrier.CreateCommandBuffer( );
            
            Entities.WithNone<AddedToLookupTables>( ).ForEach( ( Entity e, in GhostComponent ghost ) =>
            {
                var hash = 13;
                hash = hash * 7 + ghost.ghostId.GetHashCode( );
                hash = hash * 7 + ghost.ghostType.GetHashCode( );
                hash = hash * 7 + ghost.spawnTick.GetHashCode( );

                if ( hash == 4459 ) return;

                lookup.Add( hash, e );
                hashLookup.Add( e, hash );

                commandBuffer.AddComponent<AddedToLookupTables>( e );

            } ).Schedule( );

            Entities.WithNone<GhostComponent>( ).WithAll<AddedToLookupTables>().ForEach( ( Entity ent ) =>
            {
                var hash = hashLookup[ent];
                
                lookup.Remove( hash );
                hashLookup.Remove( ent );

                commandBuffer.RemoveComponent<AddedToLookupTables>( ent );

            } ).Schedule( );
            
            m_barrier.AddJobHandleForProducer( Dependency );
        }
    }

}