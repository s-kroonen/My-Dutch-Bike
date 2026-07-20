using UnityEngine;

namespace MyDutchBike.Parts
{
    /// <summary>
    /// Per-archetype handling data (ADR-0005): a city frame and a race frame are the same
    /// PartDefinition/socket/fastener system, but ride differently. Attach to a frame's
    /// PartDefinition asset alongside it (same GameObject, since PartDefinition is a ScriptableObject
    /// this lives as a sibling asset referenced by id, or is added via PartDefinitionMechanicsLink).
    /// </summary>
    [CreateAssetMenu(menuName = "My Dutch Bike/Frame Mechanics", fileName = "FrameMechanics_")]
    public class FrameMechanics : ScriptableObject
    {
        [Tooltip("Must match a PartDefinition.id for a Frame-category part.")]
        public string frameId;

        public float topSpeed = 6f;
        public float acceleration = 3f;
        public float turnRate = 90f;
        [Tooltip("Higher = more stable/slower to tip; lower = twitchier.")]
        public float stability = 1f;
        [Tooltip("Number of rear sprockets/cogs. City bikes use an internal hub gear = 1; race/mountain bikes use a derailleur cassette = up to 7. Drives the rear cog stack the chain routes onto (ADR-0005); actual shifting is a later mechanic.")]
        public int rearGearCount = 1;
    }
}
