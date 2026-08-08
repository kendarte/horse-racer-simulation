#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BatchRecorder))]
public class BatchRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BatchRecorder br = (BatchRecorder)target;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Referencias", EditorStyles.boldLabel);

        br.raceOrchestrator = (RaceOrchestrator)EditorGUILayout.ObjectField(
            "Race Orchestrator",
            br.raceOrchestrator,
            typeof(RaceOrchestrator),
            true);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Configuracion de Grabacion", EditorStyles.boldLabel);

        br.frameWidth = EditorGUILayout.IntField("Frame Width", br.frameWidth);
        br.frameHeight = EditorGUILayout.IntField("Frame Height", br.frameHeight);
        br.frameRate = EditorGUILayout.IntField("Frame Rate", br.frameRate);
        br.DelayDeGrabacion = EditorGUILayout.FloatField("Delay de Grabacion (s)", br.DelayDeGrabacion);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Carpeta de Salida", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        br.outputFolder = EditorGUILayout.TextField(br.outputFolder);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selected = EditorUtility.OpenFolderPanel(
                "Seleccionar carpeta de salida",
                string.IsNullOrEmpty(br.outputFolder) ? "" : br.outputFolder,
                "");

            if (!string.IsNullOrEmpty(selected))
            {
                br.outputFolder = selected + "/";
                EditorUtility.SetDirty(br);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(br.outputFolder))
            EditorGUILayout.HelpBox(br.outputFolder, MessageType.None);
        else
            EditorGUILayout.HelpBox("Sin carpeta seleccionada. Se usara Captures/Batch/", MessageType.Warning);

        if (GUI.changed)
            EditorUtility.SetDirty(br);
    }
}
#endif