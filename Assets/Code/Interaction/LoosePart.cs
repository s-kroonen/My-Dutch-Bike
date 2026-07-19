using MyDutchBike.Parts;
using UnityEngine;

namespace MyDutchBike.Interaction
{
    /// <summary>
    /// Sits on the world instance of a part that hasn't been installed yet — the thing you pick up.
    /// The actual Collider lives on a child ("Visual"), not this GameObject, so this can't use
    /// [RequireComponent(typeof(Collider))] — Collider is abstract and lives one level down.
    /// </summary>
    public class LoosePart : MonoBehaviour
    {
        public PartDefinition definition;
    }
}
