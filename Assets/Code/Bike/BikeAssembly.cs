using System.Collections.Generic;
using MyDutchBike.Interaction;
using MyDutchBike.Parts;
using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>
    /// Runtime manager for one buildable bike instance: spawns/removes part visuals, tracks
    /// AssemblyState (the save payload, see docs/PART_SYSTEM_DESIGN.md), and reports whether
    /// the bike is complete enough to ride. Pairs each installed PartDefinition with its
    /// plain-data PartState so state stays serializable even though this component isn't.
    /// </summary>
    public class BikeAssembly : MonoBehaviour
    {
        public string objectId = "bike.instance";
        public PartDefinition frameDefinition;
        [Tooltip("Every non-frame part that must be installed+secured for the bike to be considered complete.")]
        public PartDefinition[] requiredParts;
        [Tooltip("Handling data for this bike's frame archetype (ADR-0005).")]
        public FrameMechanics mechanics;
        [Tooltip("Local height the frame (and thus the whole bike) spawns at, so wheels clear the ground and sit in the repair stand.")]
        public float frameSpawnHeight = 0f;
        [Tooltip("Layer name to move every spawned part onto (so the player capsule doesn't collide with / climb the bike). Ignored if the layer doesn't exist.")]
        public string partLayerName = "Part";

        public AssemblyState State { get; private set; } = new AssemblyState();

        private readonly Dictionary<string, Transform> _socketTransforms = new Dictionary<string, Transform>();
        private readonly Dictionary<string, SocketMarker> _socketMarkers = new Dictionary<string, SocketMarker>();
        private readonly Dictionary<string, GameObject> _spawnedInstances = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, FastenerPoint> _fastenerPoints = new Dictionary<string, FastenerPoint>();
        private readonly Dictionary<string, PartDefinition> _installedDefs = new Dictionary<string, PartDefinition>();
        // FastenerKey(defId, slotId) -> the slot id on the same part that must be tight before this one can be.
        private readonly Dictionary<string, string> _fastenerPrereq = new Dictionary<string, string>();
        // socketId -> defId of the part that PROVIDES the socket (its parent in the assembly train).
        private readonly Dictionary<string, string> _socketProvider = new Dictionary<string, string>();
        // socketId -> defId of the part currently INSTALLED on it (its child), if any.
        private readonly Dictionary<string, string> _socketOccupant = new Dictionary<string, string>();

        private const float SecuredThreshold = 0.9f;

        private static readonly Color SocketOpenColor = new Color(0.2f, 1f, 0.5f);
        private static readonly Color SocketHighlightColor = new Color(1f, 0.9f, 0.15f);
        private static readonly Color SocketTargetColor = Color.white;
        private static readonly Color FastenerLooseColor = new Color(1f, 0.2f, 0.15f);
        private static readonly Color FastenerTightColor = new Color(0.2f, 1f, 0.3f);

        [Tooltip("Blink frequency (full pulses per second) for sockets highlighted while holding a matching part.")]
        public float socketBlinkSpeed = 2.5f;

        private bool _initialized;

        // Set each frame by PlayerInteractor while a part is held, so matching open sockets blink.
        private PartDefinition _highlightDef;
        private SocketMarker _targetedSocket;
        // Set each frame by PlayerInteractor so the fastener under the crosshair pulses.
        private FastenerPoint _targetedFastener;
        private FastenerPoint _lastHighlightedFastener;

        private void Awake() => Initialize();

        /// <summary>Spawns the frame and registers its sockets. Safe to call directly in edit mode
        /// (e.g. from tooling/tests), since Awake() only runs automatically in Play mode.</summary>
        public void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;

            State.objectId = objectId;
            SpawnFrame();
        }

        private void SpawnFrame()
        {
            if (frameDefinition == null)
            {
                Debug.LogError($"BikeAssembly '{name}' has no frameDefinition assigned.", this);
                return;
            }

            var instance = Instantiate(frameDefinition.prefab, transform);
            instance.name = frameDefinition.displayName;
            instance.transform.localPosition = new Vector3(0f, frameSpawnHeight, 0f);
            MoveToPartLayer(instance);
            _spawnedInstances[frameDefinition.id] = instance;
            _installedDefs[frameDefinition.id] = frameDefinition;

            var frameState = new PartState { partDefId = frameDefinition.id, installed = true, onSocketId = "" };
            State.parts.Add(frameState);

            RegisterSockets(frameDefinition, instance.transform);
        }

        private void RegisterSockets(PartDefinition def, Transform parent)
        {
            if (def.sockets == null)
                return;

            foreach (var socket in def.sockets)
            {
                var socketGo = new GameObject($"Socket_{socket.id}");
                socketGo.transform.SetParent(parent, false);
                socketGo.transform.localPosition = socket.localPosition;
                socketGo.transform.localEulerAngles = socket.localEulerAngles;

                var collider = socketGo.AddComponent<SphereCollider>();
                collider.radius = 0.08f;
                collider.isTrigger = true;

                var marker = socketGo.AddComponent<SocketMarker>();
                marker.socketId = socket.id;
                marker.acceptedCategory = socket.acceptedCategory;
                marker.owner = this;
                marker.indicator = DebugVisualUtility.CreateIndicator(socketGo.transform, 0.06f, SocketOpenColor);

                _socketTransforms[socket.id] = socketGo.transform;
                _socketMarkers[socket.id] = marker;
                _socketProvider[socket.id] = def.id;
            }
        }

        private void SpawnFasteners(PartDefinition def, Transform parent, PartState state)
        {
            if (def.fasteners == null)
                return;

            foreach (var slot in def.fasteners)
            {
                var pointGo = new GameObject($"Fastener_{slot.id}");
                pointGo.transform.SetParent(parent, false);
                pointGo.transform.localPosition = slot.localPosition;

                var collider = pointGo.AddComponent<SphereCollider>();
                collider.radius = 0.03f;
                collider.isTrigger = true;

                var point = pointGo.AddComponent<FastenerPoint>();
                point.partDefId = def.id;
                point.fastenerSlotId = slot.id;
                point.owner = this;
                point.displayLabel = slot.displayLabel;
                point.isChainRoute = slot.isChainRoute;
                point.indicator = DebugVisualUtility.CreateIndicator(pointGo.transform, 0.025f, FastenerLooseColor);

                _fastenerPoints[FastenerKey(def.id, slot.id)] = point;
                if (!string.IsNullOrEmpty(slot.prerequisiteFastenerId))
                    _fastenerPrereq[FastenerKey(def.id, slot.id)] = slot.prerequisiteFastenerId;
                state.fasteners.Add(new FastenerState { fastenerSlotId = slot.id, present = true, tightness = 0f });
            }
        }

        /// <summary>Checks dependencies + socket availability without installing anything.</summary>
        public bool CanInstall(PartDefinition def, string socketId, out string reason)
        {
            if (!_socketMarkers.TryGetValue(socketId, out var marker) || marker.occupied)
            {
                reason = "socket unavailable";
                return false;
            }

            if (marker.acceptedCategory != def.category)
            {
                reason = $"socket wants {marker.acceptedCategory}, part is {def.category}";
                return false;
            }

            // Attach gate: the part providing this socket must be fully torqued before a child goes on it
            // (so you can't hang a wheel off a fork whose headset isn't tight). The frame has no fasteners,
            // so its sockets are always open.
            if (_socketProvider.TryGetValue(socketId, out var providerId) && !IsPartSecured(providerId))
            {
                reason = $"tighten {DisplayName(providerId)} first";
                return false;
            }

            if (def.dependencies != null)
            {
                foreach (var dep in def.dependencies)
                {
                    // Dependencies must be installed AND fully torqued (e.g. the chain won't route until the
                    // rear wheel it reaches across to is seated tight).
                    var depState = State.Find(dep.id);
                    if (depState == null || !depState.installed)
                    {
                        reason = $"attach {dep.displayName} first";
                        return false;
                    }
                    if (!depState.IsSecured())
                    {
                        reason = $"tighten {dep.displayName} first";
                        return false;
                    }
                }
            }

            reason = "";
            return true;
        }

        private bool IsPartSecured(string partDefId)
        {
            var state = State.Find(partDefId);
            return state != null && state.IsSecured();
        }

        private string DisplayName(string partDefId)
            => _installedDefs.TryGetValue(partDefId, out var d) && d != null ? d.displayName : partDefId;

        public bool TryInstallPart(PartDefinition def, string socketId)
        {
            if (!CanInstall(def, socketId, out var reason))
            {
                Debug.Log($"Can't install {def.id} on {socketId}: {reason}");
                return false;
            }

            var socketTransform = _socketTransforms[socketId];
            var instance = Instantiate(def.prefab, socketTransform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.name = def.displayName;
            MoveToPartLayer(instance);
            var installedMarker = instance.AddComponent<InstalledPart>();
            installedMarker.partDefId = def.id;
            installedMarker.owner = this;
            _spawnedInstances[def.id] = instance;
            _installedDefs[def.id] = def;

            var occupiedMarker = _socketMarkers[socketId];
            occupiedMarker.occupied = true;
            _socketOccupant[socketId] = def.id;
            if (occupiedMarker.indicator != null)
                occupiedMarker.indicator.gameObject.SetActive(false);

            var state = new PartState { partDefId = def.id, installed = true, onSocketId = socketId };
            State.parts.Add(state);

            RegisterSockets(def, instance.transform);
            SpawnFasteners(def, instance.transform, state);

            // A chain routes across two other parts (crankset + rear wheel), so it needs to be wired up
            // to them once its own routing fasteners exist. Deterministic call, not a Start() race.
            var chainRoute = instance.GetComponent<ChainRoute>();
            if (chainRoute != null)
                chainRoute.Setup(this);

            return true;
        }

        /// <summary>A part can come off once every one of its fasteners is fully loose and nothing is
        /// installed on any socket it provides (you must strip parts off in reverse). The frame never comes off.</summary>
        public bool CanRemove(string partDefId, out string reason)
        {
            if (frameDefinition != null && partDefId == frameDefinition.id)
            {
                reason = "can't remove the frame";
                return false;
            }

            var state = State.Find(partDefId);
            if (state == null || !state.installed)
            {
                reason = "not installed";
                return false;
            }

            foreach (var fastener in state.fasteners)
            {
                if (fastener.tightness > 0.02f)
                {
                    reason = "loosen all fasteners first";
                    return false;
                }
            }

            if (_installedDefs.TryGetValue(partDefId, out var def) && def.sockets != null)
            {
                foreach (var socket in def.sockets)
                {
                    if (_socketMarkers.TryGetValue(socket.id, out var marker) && marker.occupied)
                    {
                        reason = "remove the part attached to it first";
                        return false;
                    }
                }
            }

            reason = "";
            return true;
        }

        /// <summary>Removes an installed part and hands it back as a fresh loose part (physics on), or null
        /// if it can't be removed. Frees its parent socket and forgets the sockets/fasteners it provided.</summary>
        public LoosePart TryRemovePart(string partDefId)
        {
            if (!CanRemove(partDefId, out _))
                return null;
            if (!_installedDefs.TryGetValue(partDefId, out var def))
                return null;

            var state = State.Find(partDefId);

            // Free the socket this part sat on (belongs to its parent) and show that indicator again.
            if (!string.IsNullOrEmpty(state.onSocketId) && _socketMarkers.TryGetValue(state.onSocketId, out var parentMarker))
            {
                parentMarker.occupied = false;
                _socketOccupant.Remove(state.onSocketId);
                if (parentMarker.indicator != null)
                    parentMarker.indicator.gameObject.SetActive(true);
            }

            // Forget the sockets and fasteners this part provided (their GameObjects die with the instance).
            if (def.sockets != null)
            {
                foreach (var socket in def.sockets)
                {
                    _socketMarkers.Remove(socket.id);
                    _socketTransforms.Remove(socket.id);
                    _socketProvider.Remove(socket.id);
                    _socketOccupant.Remove(socket.id);
                }
            }
            if (def.fasteners != null)
            {
                foreach (var slot in def.fasteners)
                    _fastenerPoints.Remove(FastenerKey(def.id, slot.id));
            }

            State.parts.Remove(state);
            _installedDefs.Remove(partDefId);

            Vector3 worldPos = transform.position;
            if (_spawnedInstances.TryGetValue(partDefId, out var instance) && instance != null)
            {
                worldPos = instance.transform.position;
                Destroy(instance);
            }
            _spawnedInstances.Remove(partDefId);

            return SpawnLoosePart(def, worldPos);
        }

        private LoosePart SpawnLoosePart(PartDefinition def, Vector3 worldPosition)
        {
            var go = Instantiate(def.prefab, worldPosition + Vector3.up * 0.1f, Quaternion.identity);
            go.name = def.displayName;
            MoveToPartLayer(go);

            var loose = go.AddComponent<LoosePart>();
            loose.definition = def;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.05f, def.mass);
            rb.angularDamping = 4f; // so a removed wheel/tire settles instead of rolling away

            return loose;
        }

        public bool SetFastenerTightness(string partDefId, string fastenerSlotId, float delta)
        {
            var partState = State.Find(partDefId);
            var fastener = partState?.fasteners.Find(f => f.fastenerSlotId == fastenerSlotId);
            if (fastener == null)
                return false;

            // Respect ordering (e.g. chain: front sprocket before rear cog, and rear must come off before front).
            if (!CanAdjustFastener(partDefId, fastenerSlotId, delta > 0f, out _))
                return false;

            fastener.tightness = Mathf.Clamp01(fastener.tightness + delta);

            if (_fastenerPoints.TryGetValue(FastenerKey(partDefId, fastenerSlotId), out var point) && point.indicator != null)
                DebugVisualUtility.SetColor(point.indicator, Color.Lerp(FastenerLooseColor, FastenerTightColor, fastener.tightness));

            return true;
        }

        /// <summary>Ordering rule for prerequisite fasteners (chain routing): you can't tighten a fastener
        /// until its prerequisite is tight, and you can't loosen a fastener while something that depends on
        /// it is still tight. Bolts (no prerequisite) always pass.</summary>
        public bool CanAdjustFastener(string partDefId, string fastenerSlotId, bool increasing, out string reason)
        {
            reason = "";

            // Layered fasteners ("train"): you can only turn a part's fasteners when it's the leaf — nothing
            // is still attached to it. This forces teardown to be the strict reverse of assembly.
            if (HasOccupiedChildSocket(partDefId, out string childName))
            {
                reason = $"remove {childName} first";
                return false;
            }

            if (increasing)
            {
                if (_fastenerPrereq.TryGetValue(FastenerKey(partDefId, fastenerSlotId), out var prereqId)
                    && !string.IsNullOrEmpty(prereqId)
                    && GetFastenerTightness(partDefId, prereqId) < SecuredThreshold)
                {
                    reason = "do the previous step first";
                    return false;
                }
                return true;
            }

            // Loosening: block if any fastener on this part depends on this one and is still tight.
            string prefix = partDefId + ":";
            foreach (var kv in _fastenerPrereq)
            {
                if (kv.Value != fastenerSlotId || !kv.Key.StartsWith(prefix))
                    continue;
                string dependentSlot = kv.Key.Substring(prefix.Length);
                if (GetFastenerTightness(partDefId, dependentSlot) > 0.02f)
                {
                    reason = "undo the later step first";
                    return false;
                }
            }
            return true;
        }

        /// <summary>True if any socket this part provides currently has a child installed on it (so the part
        /// is a parent in the train, not a leaf). <paramref name="childName"/> names one such child.</summary>
        public bool HasOccupiedChildSocket(string partDefId, out string childName)
        {
            childName = "";
            if (!_installedDefs.TryGetValue(partDefId, out var def) || def == null || def.sockets == null)
                return false;
            foreach (var socket in def.sockets)
            {
                if (_socketMarkers.TryGetValue(socket.id, out var m) && m.occupied)
                {
                    childName = _socketOccupant.TryGetValue(socket.id, out var occ) ? DisplayName(occ) : "the attached part";
                    return true;
                }
            }
            return false;
        }

        /// <summary>The live GameObject for an installed part (frame or otherwise), or null. Lets a part's
        /// own components (e.g. ChainRoute) find sibling parts it needs to reach, like the crankset/rear wheel.</summary>
        public GameObject GetInstalledInstance(string partDefId)
            => _spawnedInstances.TryGetValue(partDefId, out var go) ? go : null;

        /// <summary>Nearest removable-ish installed part to the player's aim, within a cone — so small or
        /// visually-buried parts (brakes, the chain) can be aimed at to remove, not just pixel-perfect rays.
        /// The frame is never returned (it doesn't come off).</summary>
        public InstalledPart FindAimedInstalledPart(Vector3 origin, Vector3 direction, float maxRange, float maxAngleDeg, out float angle)
        {
            angle = float.MaxValue;
            InstalledPart best = null;

            foreach (var kv in _spawnedInstances)
            {
                if (frameDefinition != null && kv.Key == frameDefinition.id)
                    continue;
                var go = kv.Value;
                if (go == null)
                    continue;
                var ip = go.GetComponent<InstalledPart>();
                if (ip == null)
                    continue;

                Vector3 toPart = go.transform.position - origin;
                float distance = toPart.magnitude;
                if (distance > maxRange)
                    continue;

                float a = Vector3.Angle(direction, toPart);
                if (a > maxAngleDeg || a >= angle)
                    continue;

                angle = a;
                best = ip;
            }

            return best;
        }

        private static string FastenerKey(string partDefId, string fastenerSlotId) => $"{partDefId}:{fastenerSlotId}";

        /// <summary>Tells this bike which held part (if any) to blink sockets for, and which socket the
        /// player is currently aiming at. Call every frame while holding; pass null def to clear.</summary>
        public void SetSocketHighlight(PartDefinition heldDef, SocketMarker targeted)
        {
            _highlightDef = heldDef;
            _targetedSocket = targeted;
        }

        public void ClearSocketHighlight() => SetSocketHighlight(null, null);

        private void Update()
        {
            if (!_initialized)
                return;

            // 0..1 triangle wave; one full up-down cycle per (1/socketBlinkSpeed) seconds.
            float pulse = Mathf.Abs(Mathf.Sin(Time.time * socketBlinkSpeed * Mathf.PI));

            foreach (var kv in _socketMarkers)
            {
                var marker = kv.Value;
                if (marker.occupied || marker.indicator == null || !marker.indicator.gameObject.activeSelf)
                    continue;

                bool installable = _highlightDef != null
                    && marker.acceptedCategory == _highlightDef.category
                    && CanInstall(_highlightDef, marker.socketId, out _);

                if (installable)
                {
                    var peak = marker == _targetedSocket ? SocketTargetColor : SocketHighlightColor;
                    DebugVisualUtility.SetColor(marker.indicator, Color.Lerp(SocketOpenColor, peak, pulse));
                }
                else
                {
                    DebugVisualUtility.SetColor(marker.indicator, SocketOpenColor);
                }
            }

            // Pulse the fastener the player is aiming at (over its red→green tightness colour) so it's
            // obvious which bolt [LMB]/[RMB] will turn; restore the previous one when the target changes.
            if (_lastHighlightedFastener != _targetedFastener)
            {
                RefreshFastenerColor(_lastHighlightedFastener);
                _lastHighlightedFastener = _targetedFastener;
            }
            if (_targetedFastener != null && _targetedFastener.indicator != null)
            {
                float tightness = GetFastenerTightness(_targetedFastener.partDefId, _targetedFastener.fastenerSlotId);
                Color baseColor = Color.Lerp(FastenerLooseColor, FastenerTightColor, tightness);
                DebugVisualUtility.SetColor(_targetedFastener.indicator, Color.Lerp(baseColor, Color.white, pulse));
            }
        }

        public void SetTargetedFastener(FastenerPoint fastener) => _targetedFastener = fastener;

        private void RefreshFastenerColor(FastenerPoint point)
        {
            if (point == null || point.indicator == null)
                return;
            float tightness = GetFastenerTightness(point.partDefId, point.fastenerSlotId);
            DebugVisualUtility.SetColor(point.indicator, Color.Lerp(FastenerLooseColor, FastenerTightColor, tightness));
        }

        /// <summary>Names the first required part that still needs attaching or tightening, or null when the
        /// bike is complete — so the UI can tell the player exactly what's left.</summary>
        public string FirstIncompleteRequirement()
        {
            if (requiredParts == null)
                return null;
            foreach (var part in requiredParts)
            {
                var state = State.Find(part.id);
                if (state == null || !state.installed)
                    return $"{part.displayName} (attach)";
                if (!state.IsSecured())
                {
                    bool isChain = part.fasteners != null && System.Array.Exists(part.fasteners, f => f.isChainRoute);
                    return $"{part.displayName} ({(isChain ? "route" : "tighten")})";
                }
            }
            return null;
        }

        /// <summary>Finds the open socket nearest the player's aim that accepts <paramref name="def"/>,
        /// within range and a forgiving cone (so small/embedded socket colliders don't have to be hit
        /// exactly). Returns null if none is aimed at. <paramref name="installable"/>/<paramref name="reason"/>
        /// report whether that socket can actually take the part right now (dependencies etc.).</summary>
        public SocketMarker FindAimedSocket(PartDefinition def, Vector3 origin, Vector3 direction,
            float maxRange, float maxAngleDeg, out bool installable, out string reason, out float angle)
        {
            installable = false;
            reason = "";
            angle = float.MaxValue;
            SocketMarker best = null;

            foreach (var kv in _socketMarkers)
            {
                var marker = kv.Value;
                if (marker.occupied || marker.acceptedCategory != def.category)
                    continue;

                Vector3 toSocket = marker.transform.position - origin;
                float distance = toSocket.magnitude;
                if (distance > maxRange)
                    continue;

                float a = Vector3.Angle(direction, toSocket);
                if (a > maxAngleDeg || a >= angle)
                    continue;

                angle = a;
                best = marker;
            }

            if (best != null)
                installable = CanInstall(def, best.socketId, out reason);
            return best;
        }

        /// <summary>Finds the fastener nearest the player's aim within range and a cone — same forgiving
        /// approach as sockets, since the 3cm fastener colliders are buried in the part mesh.</summary>
        public FastenerPoint FindAimedFastener(Vector3 origin, Vector3 direction, float maxRange, float maxAngleDeg, out float angle)
        {
            angle = float.MaxValue;
            FastenerPoint best = null;

            foreach (var kv in _fastenerPoints)
            {
                var point = kv.Value;
                if (point == null)
                    continue;

                Vector3 toPoint = point.transform.position - origin;
                float distance = toPoint.magnitude;
                if (distance > maxRange)
                    continue;

                float a = Vector3.Angle(direction, toPoint);
                if (a > maxAngleDeg || a >= angle)
                    continue;

                angle = a;
                best = point;
            }

            return best;
        }

        public float GetFastenerTightness(string partDefId, string fastenerSlotId)
        {
            var partState = State.Find(partDefId);
            var fastener = partState?.fasteners.Find(f => f.fastenerSlotId == fastenerSlotId);
            return fastener != null ? fastener.tightness : 0f;
        }

        private void MoveToPartLayer(GameObject go)
        {
            int layer = string.IsNullOrEmpty(partLayerName) ? -1 : LayerMask.NameToLayer(partLayerName);
            if (layer < 0)
                return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        public IEnumerable<string> OpenSocketIdsFor(PartCategory category)
        {
            foreach (var kv in _socketMarkers)
            {
                if (!kv.Value.occupied && kv.Value.acceptedCategory == category)
                    yield return kv.Key;
            }
        }

        public bool IsFullyAssembledAndSecured()
        {
            if (requiredParts == null)
                return true;

            foreach (var part in requiredParts)
            {
                var state = State.Find(part.id);
                if (state == null || !state.installed || !state.IsSecured())
                    return false;
            }
            return true;
        }
    }
}
