using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace NetcodeExtensions.OwnerControlledPhysics
{
    public class OwnerControlledPhysicsBody : MonoBehaviour, IDeclareReferencedPrefabs
    {
        public GameObject physicsBody;
        
        [Range(1, 20)]
        public int stateUpdatesToSendPerSecond;
        
        public void DeclareReferencedPrefabs( List<GameObject> referencedPrefabs )
        {
            referencedPrefabs.Add( physicsBody );
        }
    }
}
