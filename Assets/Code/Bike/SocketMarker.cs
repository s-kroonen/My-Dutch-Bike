using MyDutchBike.Parts;
using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>Marks a world position where a part can be installed. Spawned by BikeAssembly for each socket a part provides.</summary>
    public class SocketMarker : MonoBehaviour
    {
        public string socketId;
        public PartCategory acceptedCategory;
        public bool occupied;
        public BikeAssembly owner;
        public Renderer indicator;
    }
}
