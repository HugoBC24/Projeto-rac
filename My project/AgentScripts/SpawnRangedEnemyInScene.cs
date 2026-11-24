using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectRac.Editor;
using ProjectRac.Enemy;

public static class SpawnRangedEnemyInScene
{
    public static object Spawn()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var arena = GameObject.Find("TestArena");
        if (arena == null)
        {
            return new { ok = false, reason = "TestArena not found in active scene." };
        }

        var pool = EnemyBuilder.EnsureEnemyProjectilePool(arena.transform);
        var ranged = EnemyBuilder.SpawnRangedEnemy(arena.transform, new Vector3(4f, 0f, 21f), pool);
        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        var ai = ranged.GetComponent<EnemyRangedAI>();
        var hp = ranged.GetComponent<EnemyHealth>();

        return new
        {
            ok = true,
            scene = activeScene.path,
            rangedName = ranged.name,
            rangedPos = ranged.transform.position.ToString("F2"),
            maxHealth = hp.MaxHealth,
            poolName = pool.name,
            hasMesh = ranged.transform.Find("RangedEnemyMesh") != null,
            hasFirePoint = ranged.transform.Find("RangedEnemyMesh/FirePoint") != null
        };
    }
}
