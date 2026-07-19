using System.Collections.Generic;

namespace MyDutchBike.Parts
{
    /// <summary>Runtime state of one fastener. Plain data — ids/floats/bools only. See docs/PART_SYSTEM_DESIGN.md §3.</summary>
    [System.Serializable]
    public class FastenerState
    {
        public string fastenerSlotId;
        public bool present = true;
        public float tightness;
    }

    /// <summary>Runtime state of one installed/loose part. Plain data — no scene references.</summary>
    [System.Serializable]
    public class PartState
    {
        public string partDefId;
        public bool installed;
        public string onSocketId = "";
        public float condition = 1f;
        public List<FastenerState> fasteners = new List<FastenerState>();

        public bool IsSecured()
        {
            if (fasteners.Count == 0)
                return true;
            foreach (var f in fasteners)
            {
                if (!f.present || f.tightness < 0.9f)
                    return false;
            }
            return true;
        }
    }

    /// <summary>The entire save payload for one buildable object (e.g. one bike). See docs/PART_SYSTEM_DESIGN.md §3, §6.</summary>
    [System.Serializable]
    public class AssemblyState
    {
        public string objectId;
        public List<PartState> parts = new List<PartState>();

        public PartState Find(string partDefId) => parts.Find(p => p.partDefId == partDefId);
    }
}
