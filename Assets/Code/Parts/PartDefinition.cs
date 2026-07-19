using UnityEngine;

namespace MyDutchBike.Parts
{
    /// <summary>
    /// Authored, immutable part data: identity + mesh + sockets it provides + fasteners that secure it.
    /// A bike (or any buildable object) is just a set of these. See docs/PART_SYSTEM_DESIGN.md §2.
    /// </summary>
    [CreateAssetMenu(menuName = "My Dutch Bike/Part Definition", fileName = "Part_")]
    public class PartDefinition : ScriptableObject
    {
        [Tooltip("Stable unique id, e.g. bike.frame.city")]
        public string id;
        public string displayName;
        public GameObject prefab;
        public float mass = 1f;

        [Header("Fitting")]
        public PartCategory category;
        [Tooltip("The socket category this part plugs into. None = root part (e.g. a frame), installed directly on a stand.")]
        public PartCategory installsOn;
        [Tooltip("Other parts that must already be installed before this one can be installed.")]
        public PartDefinition[] dependencies;

        [Header("Provides")]
        public SocketDefinition[] sockets;
        public FastenerSlot[] fasteners;
    }
}
