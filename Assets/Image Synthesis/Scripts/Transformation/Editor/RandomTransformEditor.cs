using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(RandomTransform))]
    public class RandomTransformEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            RandomTransform myScript = (RandomTransform)target;
            
            myScript.samplingType = (RandomTransform.SamplingType)EditorGUILayout.EnumPopup("Sampling Type", myScript.samplingType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Position Settings", EditorStyles.boldLabel);
            DrawTransformAxis(myScript.positionX, "X");
            DrawTransformAxis(myScript.positionY, "Y");
            DrawTransformAxis(myScript.positionZ, "Z");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
            DrawTransformAxis(myScript.rotationX, "X");
            DrawTransformAxis(myScript.rotationY, "Y");
            DrawTransformAxis(myScript.rotationZ, "Z");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scale Settings", EditorStyles.boldLabel);
            DrawTransformAxis(myScript.scaleX, "X");
            DrawTransformAxis(myScript.scaleY, "Y");
            DrawTransformAxis(myScript.scaleZ, "Z");

            EditorGUILayout.Space();
            if (GUILayout.Button("Sample Transform"))
            {
                // Get a random int
                int randomInt = Random.Range(0, 1000);
                myScript.SampleTransform(randomInt);
            }
        }

        void DrawTransformAxis(RandomTransform.TransformAxis axis, string label)
        {
            axis.isRandom = EditorGUILayout.BeginToggleGroup("Random " + label, axis.isRandom);
            axis.range = EditorGUILayout.Vector2Field("Range", axis.range);
            EditorGUILayout.EndToggleGroup();
        }
    }
}