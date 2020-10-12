using Unity.Entities;

namespace OwnerControlledPhysics.Runtime
{
    /// <summary>
    /// Flag to determine if we have authority over a physics body and send state updates.
    /// </summary>
    internal struct HasAuthorityOverPhysicsBody : IComponentData { }
}