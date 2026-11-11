using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectRac.Editor
{
    [InitializeOnLoad]
    public static class TestCubeCreator
    {
        static TestCubeCreator()
        {
            EditorApplication.delayCall += EnsureTestCubeInScene;
        }

        [MenuItem("Tools/Insert Test Cube")]
        public static void EnsureTestCubeInScene()
        {
            var existingCube = GameObject.Find("TestCube");
            if (existingCube != null)
            {
                Debug.Log("[Antigravity] TestCube already exists in scene at: " + existingCube.transform.position);
                return;
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestCube";
            cube.transform.position = new Vector3(0, 1.0f, 0);
            cube.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Antigravity] Successfully created TestCube in active scene: " + activeScene.name);
        }
    }
}
