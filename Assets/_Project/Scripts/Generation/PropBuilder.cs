using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>
    /// Builds low-poly props from Unity primitives at runtime. No custom assets.
    /// </summary>
    public static class PropBuilder
    {
        private static Material _defaultMaterial;

        private static Material GetDefaultMaterial()
        {
            if (_defaultMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("URP/Lit")
                    ?? Shader.Find("Standard");
                _defaultMaterial = new Material(shader);
                _defaultMaterial.color = new Color(0.7f, 0.65f, 0.6f);
            }
            return _defaultMaterial;
        }

        private static void ApplyMaterial(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = GetDefaultMaterial();
            foreach (Transform child in go.transform)
                ApplyMaterial(child.gameObject);
        }

        public static GameObject Build(PropPrimitiveType type, Vector3 scale)
        {
            var root = new GameObject($"Prop_{type}");
            root.transform.localScale = scale;

            switch (type)
            {
                case PropPrimitiveType.Bench:
                    BuildBench(root);
                    break;
                case PropPrimitiveType.TrashCan:
                    BuildTrashCan(root);
                    break;
                case PropPrimitiveType.PottedPlant:
                    BuildPottedPlant(root);
                    break;
                case PropPrimitiveType.WetFloorSign:
                    BuildWetFloorSign(root);
                    break;
                case PropPrimitiveType.Kiosk:
                    BuildKiosk(root);
                    break;
                case PropPrimitiveType.BoxStack:
                    BuildBoxStack(root);
                    break;
                case PropPrimitiveType.Chair:
                    BuildChair(root);
                    break;
                case PropPrimitiveType.Cone:
                    BuildCone(root);
                    break;
                case PropPrimitiveType.SodaMachine:
                    BuildSodaMachine(root);
                    break;
                default:
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(root.transform);
                    cube.transform.localPosition = Vector3.zero;
                    cube.transform.localScale = Vector3.one;
                    break;
            }

            ApplyMaterial(root);
            return root;
        }

        private static void BuildBench(GameObject root)
        {
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.SetParent(root.transform);
            seat.transform.localPosition = new Vector3(0, 0.4f, 0);
            seat.transform.localScale = new Vector3(1.5f, 0.1f, 0.5f);
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Back";
            back.transform.SetParent(root.transform);
            back.transform.localPosition = new Vector3(0, 0.8f, -0.2f);
            back.transform.localScale = new Vector3(1.5f, 0.6f, 0.1f);
        }

        private static void BuildTrashCan(GameObject root)
        {
            var can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = "Can";
            can.transform.SetParent(root.transform);
            can.transform.localPosition = new Vector3(0, 0.5f, 0);
            can.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        }

        private static void BuildPottedPlant(GameObject root)
        {
            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "Pot";
            pot.transform.SetParent(root.transform);
            pot.transform.localPosition = new Vector3(0, 0.25f, 0);
            pot.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
            var plant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            plant.name = "Plant";
            plant.transform.SetParent(root.transform);
            plant.transform.localPosition = new Vector3(0, 0.7f, 0);
            plant.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        }

        private static void BuildWetFloorSign(GameObject root)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform);
            pole.transform.localPosition = new Vector3(0, 0.6f, 0);
            pole.transform.localScale = new Vector3(0.05f, 1.2f, 0.05f);
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign";
            sign.transform.SetParent(root.transform);
            sign.transform.localPosition = new Vector3(0, 1.2f, 0);
            sign.transform.localScale = new Vector3(0.5f, 0.4f, 0.05f);
        }

        private static void BuildKiosk(GameObject root)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            body.transform.localScale = new Vector3(0.8f, 1.6f, 0.4f);
            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(root.transform);
            screen.transform.localPosition = new Vector3(0, 0.8f, 0.25f);
            screen.transform.localScale = new Vector3(0.7f, 0.9f, 0.05f);
        }

        private static void BuildBoxStack(GameObject root)
        {
            var b1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b1.transform.SetParent(root.transform);
            b1.transform.localPosition = new Vector3(0, 0.2f, 0);
            b1.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var b2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b2.transform.SetParent(root.transform);
            b2.transform.localPosition = new Vector3(0.1f, 0.6f, -0.05f);
            b2.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var b3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b3.transform.SetParent(root.transform);
            b3.transform.localPosition = new Vector3(-0.05f, 1f, 0.05f);
            b3.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }

        private static void BuildChair(GameObject root)
        {
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.transform.SetParent(root.transform);
            seat.transform.localPosition = new Vector3(0, 0.35f, 0);
            seat.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(root.transform);
            back.transform.localPosition = new Vector3(0, 0.7f, -0.2f);
            back.transform.localScale = new Vector3(0.5f, 0.5f, 0.1f);
        }

        private static void BuildCone(GameObject root)
        {
            var cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = "Cone";
            cone.transform.SetParent(root.transform);
            cone.transform.localPosition = new Vector3(0, 0.5f, 0);
            cone.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        }

        private static void BuildSodaMachine(GameObject root)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.transform.localScale = new Vector3(0.6f, 2f, 0.5f);
            var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
            front.transform.SetParent(root.transform);
            front.transform.localPosition = new Vector3(0, 1f, 0.28f);
            front.transform.localScale = new Vector3(0.5f, 1.6f, 0.05f);
        }
    }
}
