using System.Collections.Generic;
using MyDutchBike.Parts;
using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>
    /// Drives the bike chain's visual routing. The chain is installed on the crankset like any part, then
    /// "routed" in two ordered stages (front chainring, then rear cog) via the fastener hold-to-progress
    /// mechanic — see the chain_front / chain_rear fasteners authored in BikeBomAuthoring. This component:
    ///  - repositions those two routing points onto the real front-sprocket / rear-cog anchors,
    ///  - builds the rear cog stack from the frame archetype's gear count (city hub = 1, race cassette = up to 7),
    ///  - and renders the chain as a line that wraps the front sprocket (stage 1), then spans back and wraps
    ///    the rear cog (stage 2), revealing in step with the two routing values.
    /// Chain-link failure / a fallen chain are a later mechanic; this is just the put-on.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ChainRoute : MonoBehaviour
    {
        [Header("Which parts this chain routes across")]
        public string cranksetDefId = "bike.crankset";
        public string rearWheelDefId = "bike.wheel_rear";
        public string frontSlotId = "chain_front";
        public string rearSlotId = "chain_rear";

        [Header("Geometry (bike-local: right = axle/drive side, up = Y, forward = Z)")]
        [Tooltip("Signed offset along the bike's right axis to the drive side the chain runs on.")]
        public float driveOffset = -0.09f;
        public float frontRadius = 0.10f;
        public float rearRadius = 0.05f;
        public float lineWidth = 0.03f;
        public int arcSegments = 14;

        [Header("Rear cog stack")]
        public float cogThickness = 0.012f;
        public float cogSpacing = 0.014f;
        public float cogMinRadius = 0.032f;

        private LineRenderer _line;
        private BikeAssembly _owner;
        private string _chainDefId;
        private Transform _crankset;
        private Transform _rearWheel;
        private FastenerPoint _frontPoint;
        private FastenerPoint _rearPoint;
        private bool _routing;

        private static readonly Color ChainColor = new Color(0.12f, 0.12f, 0.13f);

        /// <summary>Called by BikeAssembly right after the chain is installed and its routing fasteners exist.
        /// Wires the chain to the crankset/rear wheel and switches it from "loose coil" to "routing" mode.</summary>
        public void Setup(BikeAssembly owner)
        {
            _owner = owner;
            var installed = GetComponent<InstalledPart>();
            _chainDefId = installed != null ? installed.partDefId : "bike.chain";

            var cranksetGo = owner.GetInstalledInstance(cranksetDefId);
            var rearWheelGo = owner.GetInstalledInstance(rearWheelDefId);
            _crankset = cranksetGo != null ? cranksetGo.transform : null;
            _rearWheel = rearWheelGo != null ? rearWheelGo.transform : null;

            foreach (var point in GetComponentsInChildren<FastenerPoint>(true))
            {
                if (point.fastenerSlotId == frontSlotId) _frontPoint = point;
                else if (point.fastenerSlotId == rearSlotId) _rearPoint = point;
            }

            // Hide the loose "coil" pickup mesh; the routed line is the chain from here on.
            var coil = transform.Find("Coil");
            if (coil != null)
                coil.gameObject.SetActive(false);

            SetUpLine();
            BuildRearCogs();
            PositionRoutingPoints();
            _routing = true;
        }

        private void SetUpLine()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.widthMultiplier = lineWidth;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.textureMode = LineTextureMode.Stretch;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.material = UnlitMaterial(ChainColor);
            _line.positionCount = 0;
            _line.enabled = true;
        }

        /// <summary>Move the two routing points onto the sprockets so the player aims at the front chainring
        /// to route the front, then at the rear cog to route the rear.</summary>
        private void PositionRoutingPoints()
        {
            if (_frontPoint != null && FrontCenter(out var fc))
                _frontPoint.transform.position = fc;
            if (_rearPoint != null && RearCenter(out var rc))
                _rearPoint.transform.position = rc;
        }

        /// <summary>City = internal hub (1 cog), race = derailleur cassette (up to 7). Built on the rear wheel,
        /// tapering outward like a real cassette, so the drivetrain visibly reflects the archetype (ADR-0005).</summary>
        private void BuildRearCogs()
        {
            if (_rearWheel == null || _owner == null)
                return;
            if (_rearWheel.Find("ChainCogs") != null)
                return; // already built (e.g. chain re-installed)

            int gears = _owner.mechanics != null ? Mathf.Max(1, _owner.mechanics.rearGearCount) : 1;

            var root = new GameObject("ChainCogs").transform;
            root.SetParent(_rearWheel, false);
            root.gameObject.layer = _rearWheel.gameObject.layer;

            var mat = UnlitMaterial(new Color(0.55f, 0.56f, 0.6f));
            var bike = _owner.transform;
            Quaternion axleAlign = Quaternion.FromToRotation(Vector3.up, bike.right); // cylinder Y -> bike right (axle)

            for (int i = 0; i < gears; i++)
            {
                // Nearest cog to the frame centre is the biggest; tapers outward toward the drive side.
                float t = gears == 1 ? 0f : (float)i / (gears - 1);
                float radius = Mathf.Lerp(rearRadius, cogMinRadius, t);
                float x = driveOffset + Mathf.Sign(driveOffset == 0 ? 1f : driveOffset) * (i * cogSpacing);

                var cog = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cog.name = $"Cog_{i}";
                Object.Destroy(cog.GetComponent<Collider>());
                cog.transform.SetParent(root, false);
                cog.transform.localRotation = Quaternion.identity;
                cog.transform.rotation = axleAlign;
                cog.transform.position = _rearWheel.position + bike.right * x;
                cog.transform.localScale = new Vector3(radius * 2f, cogThickness, radius * 2f);
                cog.GetComponent<Renderer>().sharedMaterial = mat;
                cog.layer = root.gameObject.layer;
            }
        }

        private void Update()
        {
            if (!_routing || _line == null || _owner == null)
                return;

            float front = _owner.GetFastenerTightness(_chainDefId, frontSlotId);
            float rear = _owner.GetFastenerTightness(_chainDefId, rearSlotId);

            // Sprockets can move at runtime (later: spinning wheels), so rebuild the loop each frame.
            PositionRoutingPoints();
            RenderLoop(front, rear);
        }

        /// <summary>Builds the closed chain loop (front wrap → top span → rear wrap → bottom span) as an
        /// ordered world-space polyline, then reveals a prefix of it: the front wrap fills during stage 1,
        /// the spans + rear wrap during stage 2.</summary>
        private void RenderLoop(float front, float rear)
        {
            if (!FrontCenter(out var fc) || !RearCenter(out var rc))
            {
                _line.positionCount = 0;
                return;
            }

            Vector3 up = _owner.transform.up;
            Vector3 fwd = _owner.transform.forward;

            var points = new List<Vector3>();

            // 1) Front wrap: bottom -> (bulge +forward) -> top.
            for (int i = 0; i <= arcSegments; i++)
            {
                float a = Mathf.Lerp(-90f, 90f, (float)i / arcSegments) * Mathf.Deg2Rad;
                points.Add(fc + (fwd * Mathf.Cos(a) + up * Mathf.Sin(a)) * frontRadius);
            }
            int frontWrapEnd = points.Count - 1; // last index of the front wrap

            // 2) Top span -> rear top.
            points.Add(rc + up * rearRadius);
            // 3) Rear wrap: top -> (bulge -forward) -> bottom.
            for (int i = 1; i <= arcSegments; i++)
            {
                float a = Mathf.Lerp(90f, 270f, (float)i / arcSegments) * Mathf.Deg2Rad;
                points.Add(rc + (fwd * Mathf.Cos(a) + up * Mathf.Sin(a)) * rearRadius);
            }
            // 4) Bottom span -> back to front bottom (close the loop).
            points.Add(fc - up * frontRadius);

            // Cumulative arc lengths.
            var cum = new float[points.Count];
            for (int i = 1; i < points.Count; i++)
                cum[i] = cum[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            float total = cum[points.Count - 1];
            float frontWrapLen = cum[frontWrapEnd];

            // Reveal: stage 1 fills the front wrap, stage 2 fills everything after it.
            float revealed = front * frontWrapLen + rear * Mathf.Max(0f, total - frontWrapLen);
            if (revealed <= 1e-4f)
            {
                _line.positionCount = 0;
                return;
            }

            var shown = new List<Vector3> { points[0] };
            for (int i = 1; i < points.Count; i++)
            {
                if (cum[i] <= revealed)
                {
                    shown.Add(points[i]);
                }
                else
                {
                    float segLen = cum[i] - cum[i - 1];
                    float t = segLen > 1e-5f ? (revealed - cum[i - 1]) / segLen : 0f;
                    shown.Add(Vector3.Lerp(points[i - 1], points[i], t));
                    break;
                }
            }

            _line.positionCount = shown.Count;
            _line.SetPositions(shown.ToArray());
        }

        private bool FrontCenter(out Vector3 center)
        {
            if (_crankset == null) { center = default; return false; }
            center = _crankset.position + _owner.transform.right * driveOffset;
            return true;
        }

        private bool RearCenter(out Vector3 center)
        {
            if (_rearWheel == null) { center = default; return false; }
            center = _rearWheel.position + _owner.transform.right * driveOffset;
            return true;
        }

        private static Material UnlitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
        }
    }
}
