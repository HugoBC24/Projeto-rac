using UnityEngine;

namespace ProjectRac.Weapons
{
    public static class ShotgunModelGenerator
    {
        public static GameObject CreateDefaultShotgunViewmodel(Transform parent)
        {
            GameObject gun = new GameObject("DefaultShotgunModel");
            gun.transform.SetParent(parent, false);

            GameObject barrel1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel1.name = "Barrel_Left";
            barrel1.transform.SetParent(gun.transform, false);
            barrel1.transform.localPosition = new Vector3(-0.02f, 0f, 0.3f);
            barrel1.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel1.transform.localScale = new Vector3(0.035f, 0.25f, 0.035f);
            SafeDestroy(barrel1.GetComponent<Collider>());

            GameObject barrel2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel2.name = "Barrel_Right";
            barrel2.transform.SetParent(gun.transform, false);
            barrel2.transform.localPosition = new Vector3(0.02f, 0f, 0.3f);
            barrel2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel2.transform.localScale = new Vector3(0.035f, 0.25f, 0.035f);
            SafeDestroy(barrel2.GetComponent<Collider>());

            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock";
            stock.transform.SetParent(gun.transform, false);
            stock.transform.localPosition = new Vector3(0f, -0.04f, 0.05f);
            stock.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            stock.transform.localScale = new Vector3(0.06f, 0.09f, 0.25f);
            SafeDestroy(stock.GetComponent<Collider>());

            var grayBlock = new MaterialPropertyBlock();
            grayBlock.SetColor("_BaseColor", new Color(0.25f, 0.26f, 0.28f));
            barrel1.GetComponent<Renderer>()?.SetPropertyBlock(grayBlock);
            barrel2.GetComponent<Renderer>()?.SetPropertyBlock(grayBlock);

            var woodBlock = new MaterialPropertyBlock();
            woodBlock.SetColor("_BaseColor", new Color(0.48f, 0.28f, 0.16f));
            stock.GetComponent<Renderer>()?.SetPropertyBlock(woodBlock);

            return gun;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
