using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CH_Gaze)), CanEditMultipleObjects]

public class CH_GazeEditor : Editor
{
    private CH_Gaze myCHGaze;

    private SerializedProperty m_agentMode;
    private SerializedProperty m_interestFieldPrefab;

    private void OnEnable()
    {
        m_interestFieldPrefab = serializedObject.FindProperty("interestFieldPrefab");
        m_agentMode = serializedObject.FindProperty("agentMode");
    }

    public override void OnInspectorGUI()
    {
        myCHGaze = (CH_Gaze)target;

        serializedObject.Update();

        base.OnInspectorGUI();

        if (myCHGaze.gazeBehavior == CH_Gaze.GazeBehavior.PROBABILISTIC)
        {
            // Displaying an object field to add the interest field prefab if the gaze behavior of the character is set to probabilistic.
            string interestFieldPrefabTooltip = "Add the Gaze Interest Field Prefab(Assets/DIINA Toolkit/Prefabs/).";
            EditorGUILayout.PropertyField(m_interestFieldPrefab, new GUIContent("Interest Field Prefab", interestFieldPrefabTooltip));
            //myVHTGaze.interestFieldPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Interest Field Prefab", interestFieldPrefabTooltip), myVHTGaze.interestFieldPrefab, typeof(GameObject), false)

            string agentModeTooltip = "Enable the agent mode to override upper body rotations when looking at a target (IK pass must be enabled in the animator settings).";
            EditorGUILayout.PropertyField(m_agentMode, new GUIContent("Agent Mode", agentModeTooltip));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
