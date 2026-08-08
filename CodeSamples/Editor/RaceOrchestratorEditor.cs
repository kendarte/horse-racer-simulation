#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RaceOrchestrator))]
public class RaceOrchestratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RaceOrchestrator ro = (RaceOrchestrator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── CONTROLES ──────────────────────", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        bool autoRunGuardado = PlayerPrefs.GetInt("AutoRun_Active", 0) == 1;

        if (!Application.isPlaying)
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("▶  START AUTO RUN", GUILayout.Height(36)))
            {
                ro.IniciarAutoRun();
                EditorApplication.isPlaying = true;
            }
            GUI.backgroundColor = Color.white;

            if (autoRunGuardado)
            {
                EditorGUILayout.HelpBox(
                    "Hay un AutoRun guardado. Al entrar en Play Mode continuara desde la carrera " +
                    PlayerPrefs.GetInt("AutoRun_CurrentID", ro.StartRaceID) + ".",
                    MessageType.Warning);

                if (GUILayout.Button("Limpiar estado guardado"))
                    ro.DetenerAutoRun();
            }
        }
        else
        {
            if (GUILayout.Button("▶  Correr Carrera (StartRaceID)", GUILayout.Height(30)))
                ro.SetupRace(ro.StartRaceID);

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("■  STOP AUTO RUN", GUILayout.Height(36)))
            {
                ro.DetenerAutoRun();
                EditorApplication.isPlaying = false;
            }
            GUI.backgroundColor = Color.white;

            // Progreso leído directo del componente en memoria
            EditorGUILayout.Space(6);
            int current = ro.CurrentRaceID;
            int total = ro.EndRaceID - ro.StartRaceID + 1;
            int done = Mathf.Max(0, current - ro.StartRaceID);
            float progress = total > 0 ? (float)done / total : 0f;

            Rect rect = GUILayoutUtility.GetRect(18, 22, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, progress,
                string.Format("Carrera {0} de {1}  ({2:0}%)", current, ro.EndRaceID, progress * 100f));

            Repaint();
        }

        GUI.enabled = true;
    }
}
#endif
