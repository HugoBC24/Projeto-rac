using System.Linq;
using UnityEngine;
using ProjectRac.Editor.Map;
using ProjectRac.Interaction;

public static class SetupMansionFixesAndProps
{
    public static object Apply()
    {
        bool fixedOk = MansionColliderFixer.FixMansionColliders();
        GameObject props = MansionPropsTestBuilder.BuildTestPropsAndUI();

        GameObject mansion = GameObject.Find(MansionMapBuilder.MansionRootName);
        var allColliders = mansion.GetComponentsInChildren<Collider>(true);
        int skewedColliders = allColliders.Count(c =>
        {
            var m = c.transform.localToWorldMatrix;
            Vector3 x = m.GetColumn(0);
            Vector3 y = m.GetColumn(1);
            Vector3 z = m.GetColumn(2);
            return Mathf.Abs(Vector3.Dot(x.normalized, y.normalized)) > 0.01f
                || Mathf.Abs(Vector3.Dot(y.normalized, z.normalized)) > 0.01f
                || Mathf.Abs(Vector3.Dot(x.normalized, z.normalized)) > 0.01f;
        });

        var rampCol = mansion.transform.Find("Staircase/Stair_RampCollider")?.GetComponent<Collider>();

        return new
        {
            fixedOk,
            mansionScale = mansion.transform.localScale.ToString("F3"),
            skewedColliders,
            rampBounds = rampCol != null ? rampCol.bounds.ToString() : "none",
            hasDrawer = props.GetComponentInChildren<DrawerController>() != null,
            hasCabinet = props.GetComponentInChildren<CabinetController>() != null,
            hasNote = props.GetComponentInChildren<InspectableNote>() != null
        };
    }
}
