using UnityEditor;
using UnityEngine;
using WorldGenerator.Core;

namespace WorldGenerator.Editor
{
    [CustomEditor(typeof(TestNoiseGenerator))]
    public class TestNoiseGeneratorEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI(); // Отображаем стандартные поля
        
            TestNoiseGenerator generator = (TestNoiseGenerator)target;
        
            // Кнопка генерации
            if (GUILayout.Button("Generate Terrain")) {
                generator.Generate();
            }
        }
    }

    [CustomEditor(typeof(NoiseGenerator))]
    public class NoiseGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            NoiseGenerator generator = (NoiseGenerator)target;

            if (GUILayout.Button("Generate Terrain"))
            {
                generator.RegenerateTerrain();
            }
        }
    }
}
