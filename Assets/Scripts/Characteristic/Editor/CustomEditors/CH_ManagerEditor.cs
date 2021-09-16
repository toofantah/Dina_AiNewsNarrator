using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CH_Manager))]
public class CH_ManagerEditor : Editor
{
    private CH_Manager myCHManager;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        myCHManager = (CH_Manager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Add procedural controllers:", EditorStyles.boldLabel);

        DrawButton("Emotions", typeof(CH_Emotions));
        DrawButton("Gaze", typeof(CH_Gaze));
        DrawButton("Lip Sync", typeof(CH_LipSync));
    }

    // Function to add scripts to the character based on the button inputs.
    private void DrawButton(string buttonName, System.Type scriptTypeToAdd)
    {
        if (GUILayout.Button(buttonName))

            if (!myCHManager.gameObject.GetComponent(scriptTypeToAdd))
                myCHManager.gameObject.AddComponent(scriptTypeToAdd);

            else
                Debug.Log(scriptTypeToAdd.ToString() + " already added to the character.");
    }
}
