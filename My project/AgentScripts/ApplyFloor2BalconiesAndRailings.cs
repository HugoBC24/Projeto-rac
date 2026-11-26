using System.Linq;
using UnityEngine;
using ProjectRac.Editor.Map;
using ProjectRac.Interaction;

public static class ApplyFloor2BalconiesAndRailings
{
    public static object Apply()
    {
        bool ok = MansionMapBuilder.UpgradeFloor2InPlace();
        var mansion = GameObject.Find(MansionMapBuilder.MansionRootName);
        var doors = mansion.GetComponentsInChildren<DoorController>(true);
        var railingsRoot = mansion.transform.Find("Floor2/Railings");
        var allColliders = mansion.GetComponentsInChildren<Collider>(true);

        int skewed = allColliders.Count(c =>
        {
            var m = c.transform.localToWorldMatrix;
            Vector3 x = m.GetColumn(0);
            Vector3 y = m.GetColumn(1);
            Vector3 z = m.GetColumn(2);
            return Mathf.Abs(Vector3.Dot(x.normalized, y.normalized)) > 0.01f
                || Mathf.Abs(Vector3.Dot(y.normalized, z.normalized)) > 0.01f
                || Mathf.Abs(Vector3.Dot(x.normalized, z.normalized)) > 0.01f;
        });

        return new
        {
            ok,
            mansionScale = mansion.transform.localScale.ToString("F3"),
            totalDoors = doors.Length,
            doorNames = doors.Select(d => d.transform.parent.parent.name).ToArray(),
            railingChildCount = railingsRoot != null ? railingsRoot.childCount : 0,
            skewedColliders = skewed,
            hasRoof = mansion.transform.Find("Roof/Roof_Slab") != null
        };
    }
}
