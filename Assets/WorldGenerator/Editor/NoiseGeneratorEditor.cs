using UnityEditor;
using UnityEngine;
using Worldgenerator.Noise;

namespace WorldGenerator.Editor
{
    [CustomEditor(typeof(TestNoiseGenerator))]
    public class NoiseGeneratorEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI(); // Отображаем стандартные поля
        
            TestNoiseGenerator generator = (TestNoiseGenerator)target;
        
            // Кнопка генерации
            if (GUILayout.Button("Generate Terrain")) {
                generator.Generate();
            }
        }
    }
}
