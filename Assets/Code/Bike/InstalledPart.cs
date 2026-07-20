using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>Sits on the world instance of a part that IS installed on the bike (the counterpart to
    /// Interaction.LoosePart). Lets the player's raycast identify which part it's looking at so it can
    /// be removed once its fasteners are loose. Spawned by BikeAssembly.TryInstallPart.</summary>
    public class InstalledPart : MonoBehaviour
    {
        public string partDefId;
        public BikeAssembly owner;
    }
}
