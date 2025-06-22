using UnityEditor;
using UnityEngine;

namespace PandemicWars.Editor
{
    public class BrokenChildPrefabChecker : EditorWindow
    {
        private string scanFolder = "Assets/Prefabs";

        [MenuItem("Tools/WFC/Find Broken Child Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<BrokenChildPrefabChecker>("Broken Prefab Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Scan Folder (inside Assets/):", EditorStyles.boldLabel);
            scanFolder = EditorGUILayout.TextField(scanFolder);

            if (GUILayout.Button("Scan Prefabs")) ScanPrefabs(scanFolder);
        }

        private void ScanPrefabs(string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            var brokenCount = 0;
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null) continue;

                foreach (Transform child in prefab.transform)
                {
                    var childObj = child.gameObject;

                    // Check for prefab connection
                    var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(childObj);
                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(childObj);

                    if (prefabAsset == null || prefabStatus == PrefabInstanceStatus.MissingAsset)
                    {
                        Debug.LogWarning(
                            $"❌ Broken child prefab in: {prefab.name} → Child: {child.name} | Path: {assetPath}",
                            prefab);
                        brokenCount++;
                    }
                    // You can add more checks here
                    // Debug.Log($"✅ OK: {child.name} in {prefab.name}");
                }
            }

            Debug.Log($"✔️ Scan complete. Broken child prefabs found: {brokenCount}");
        }
    }
}