using UnityEngine;

namespace MyDutchBike.Parts
{
    public enum FastenerType
    {
        Bolt,
        Nut,
        Screw,
    }

    /// <summary>A bolt/nut/screw slot on a part, in the part's local space. See docs/PART_SYSTEM_DESIGN.md §2.</summary>
    [System.Serializable]
    public class FastenerSlot
    {
        public string id;
        public FastenerType type = FastenerType.Bolt;
        public Vector3 localPosition;
        [Range(0f, 1f)] public float tightnessThreshold = 0.9f;
        public bool toolRequired = true;
    }
}
