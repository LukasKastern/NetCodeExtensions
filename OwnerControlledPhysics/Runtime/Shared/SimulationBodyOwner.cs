using Unity.Entities;
using Unity.NetCode;

namespace OwnerControlledPhysics.Runtime
{
    [GenerateAuthoringComponent]
    public struct SimulationBodyOwner : IComponentData
    {
        /// <summary>
        /// Network id of the client that controls this body. 
        /// </summary>
        [GhostField]
        public int Value;
    }
}