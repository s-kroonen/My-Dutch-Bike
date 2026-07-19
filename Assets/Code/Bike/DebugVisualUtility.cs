using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>
    /// Cheap unlit indicator spheres so sockets/fasteners are visible in Play mode instead of
    /// just showing up as invisible trigger colliders (which is all a player would otherwise see
    /// as gizmo wireframes in the editor, and nothing at all in a build).
    /// </summary>
    internal static class DebugVisualUtility
    {
        private static Mesh _sphereMesh;

        private static Mesh SphereMesh
        {
            get
            {
                if (_sphereMesh == null)
                {
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _sphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                    Object.Destroy(temp);
                }
                return _sphereMesh;
            }
        }

        public static Renderer CreateIndicator(Transform parent, float radius, Color color)
        {
            var go = new GameObject("Indicator");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * (radius * 2f);

            go.AddComponent<MeshFilter>().sharedMesh = SphereMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            SetColor(mat, color);
            renderer.material = mat;

            return renderer;
        }

        public static void SetColor(Renderer renderer, Color color) => SetColor(renderer.material, color);

        private static void SetColor(Material mat, Color color)
        {
            // URP shaders expose "_BaseColor", not the legacy "_Color" that Material.color assumes.
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
        }
    }
}
