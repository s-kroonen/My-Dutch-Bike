using System.Text;
using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>Toggleable readout of every BikeAssembly's part/fastener state, for playtesting.</summary>
    public class BikeDebugOverlay : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.F1;
        private bool _visible;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            var assemblies = Object.FindObjectsByType<BikeAssembly>(FindObjectsSortMode.None);
            var sb = new StringBuilder();
            sb.AppendLine($"Bike Debug ({toggleKey} to close)");
            sb.AppendLine();

            foreach (var assembly in assemblies)
            {
                sb.AppendLine($"[{assembly.objectId}] complete: {assembly.IsFullyAssembledAndSecured()}");
                foreach (var part in assembly.State.parts)
                {
                    sb.Append($"  {part.partDefId}: {(part.installed ? "installed" : "loose")}");
                    if (part.fasteners.Count > 0)
                    {
                        sb.Append(" [");
                        for (int i = 0; i < part.fasteners.Count; i++)
                        {
                            var f = part.fasteners[i];
                            sb.Append($"{f.fastenerSlotId}={f.tightness:F2}");
                            if (i < part.fasteners.Count - 1)
                                sb.Append(", ");
                        }
                        sb.Append(']');
                    }
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            var text = sb.ToString();
            var style = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
            style.normal.textColor = Color.white;

            var size = style.CalcSize(new GUIContent(text));
            GUI.Box(new Rect(8, 8, size.x + 20, size.y + 16), GUIContent.none);
            GUI.Label(new Rect(18, 16, size.x + 10, size.y + 10), text, style);
        }
    }
}
