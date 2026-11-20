using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs in-memory via `unity command script run` outside Assets/ to avoid triggering asset imports or domain reloads.
public static class ProjectProbe
{
    public static object Inspect()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var roots = activeScene.IsValid() ? activeScene.GetRootGameObjects() : new GameObject[0];

        return new
        {
            unityVersion = Application.unityVersion,
            isPlaying = EditorApplication.isPlaying,
            isCompiling = EditorApplication.isCompiling,
            sceneName = string.IsNullOrEmpty(activeScene.name) ? "(Untitled)" : activeScene.name,
            scenePath = activeScene.path,
            isDirty = activeScene.isDirty,
            rootCount = roots.Length,
            rootNames = roots.Select(go => go.name).ToArray()
        };
    }
}
