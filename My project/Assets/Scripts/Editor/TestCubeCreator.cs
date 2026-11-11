using UnityEngine;
using UnityEditor;

namespace ProjectRac.Editor
{
    [InitializeOnLoad]
    public static class TestCubeCreator
    {
        static TestCubeCreator()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            if (GameObject.Find("TestArena") == null)
            {
                TestArenaBuilder.BuildArena();
            }
        }
    }
}
