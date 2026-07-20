using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>Marks one bolt/nut (or chain-routing point) on an installed part, for the wrench to
    /// target. Spawned by BikeAssembly.</summary>
    public class FastenerPoint : MonoBehaviour
    {
        public string partDefId;
        public string fastenerSlotId;
        public BikeAssembly owner;
        public Renderer indicator;

        [Tooltip("Prompt label shown instead of the id (e.g. 'chain (front)'). Blank = use the id.")]
        public string displayLabel = "";
        [Tooltip("Chain-routing point rather than a literal bolt — UI says 'route' not 'tighten'.")]
        public bool isChainRoute = false;

        public string Label => string.IsNullOrEmpty(displayLabel) ? fastenerSlotId : displayLabel;
    }
}
