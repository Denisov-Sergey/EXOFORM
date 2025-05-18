using UnityEditor;
using UnityEngine;
using Worldgenerator.Noise;

namespace WorldGenerator.Editor
{
    [CustomEditor(typeof(NoiseGenerator))]
    public class NoiseGeneratorEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI(); // Отображаем стандартные поля
        
            NoiseGenerator generator = (NoiseGenerator)target;
        
            // Кнопка генерации
            if (GUILayout.Button("Generate Terrain")) {
                generator.Generate();
            }
        }
    }
}
