using UnityEngine;

namespace MyDutchBike.Parts
{
    /// <summary>An attach point a part provides, in the part's local space. See docs/PART_SYSTEM_DESIGN.md §2.</summary>
    [System.Serializable]
    public class SocketDefinition
    {
        public string id;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public PartCategory acceptedCategory;
    }
}
