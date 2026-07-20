using UnityEngine;

namespace MyDutchBike.Parts
{
    public enum FastenerType
    {
        Bolt,
        Nut,
        Screw,
    }

    /// <summary>A bolt/nut/screw slot on a part, in the part's local space. See docs/PART_SYSTEM_DESIGN.md §2.
    /// Also used for the chain's two "routing" points (front sprocket, rear cog) — a fastening-style
    /// hold-to-progress action that isn't literally a bolt (hence <see cref="displayLabel"/>).</summary>
    [System.Serializable]
    public class FastenerSlot
    {
        public string id;
        public FastenerType type = FastenerType.Bolt;
        public Vector3 localPosition;
        [Range(0f, 1f)] public float tightnessThreshold = 0.9f;
        public bool toolRequired = true;

        [Tooltip("Optional: another fastener id on the same part that must be tight before this one can be tightened (and that can't be loosened until this one is loose). Used to force chain-front-then-rear ordering.")]
        public string prerequisiteFastenerId = "";
        [Tooltip("Optional prompt label shown instead of the id, e.g. 'chain (front)'. Blank = use the id.")]
        public string displayLabel = "";
        [Tooltip("If true, this is a chain-routing point (front/rear sprocket), not a literal bolt — the UI says 'route' instead of 'tighten'.")]
        public bool isChainRoute = false;
    }
}
