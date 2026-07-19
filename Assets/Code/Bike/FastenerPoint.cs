using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>Marks one bolt/nut on an installed part, for the wrench to target. Spawned by BikeAssembly.</summary>
    public class FastenerPoint : MonoBehaviour
    {
        public string partDefId;
        public string fastenerSlotId;
        public BikeAssembly owner;
    }
}
