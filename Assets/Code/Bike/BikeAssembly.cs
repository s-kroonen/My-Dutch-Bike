using System.Collections.Generic;
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

        public AssemblyState State { get; private set; } = new AssemblyState();

        private readonly Dictionary<string, Transform> _socketTransforms = new Dictionary<string, Transform>();
        private readonly Dictionary<string, SocketMarker> _socketMarkers = new Dictionary<string, SocketMarker>();
        private readonly Dictionary<string, GameObject> _spawnedInstances = new Dictionary<string, GameObject>();

        private bool _initialized;

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
            _spawnedInstances[frameDefinition.id] = instance;

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

                _socketTransforms[socket.id] = socketGo.transform;
                _socketMarkers[socket.id] = marker;
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

            if (def.dependencies != null)
            {
                foreach (var dep in def.dependencies)
                {
                    var depState = State.Find(dep.id);
                    if (depState == null || !depState.installed || !depState.IsSecured())
                    {
                        reason = $"missing dependency: {dep.id}";
                        return false;
                    }
                }
            }

            reason = "";
            return true;
        }

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
            _spawnedInstances[def.id] = instance;

            _socketMarkers[socketId].occupied = true;

            var state = new PartState { partDefId = def.id, installed = true, onSocketId = socketId };
            State.parts.Add(state);

            RegisterSockets(def, instance.transform);
            SpawnFasteners(def, instance.transform, state);

            return true;
        }

        public bool SetFastenerTightness(string partDefId, string fastenerSlotId, float delta)
        {
            var partState = State.Find(partDefId);
            var fastener = partState?.fasteners.Find(f => f.fastenerSlotId == fastenerSlotId);
            if (fastener == null)
                return false;

            fastener.tightness = Mathf.Clamp01(fastener.tightness + delta);
            return true;
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
