/*
 * BatchRecorder.cs
 * Usa la API nativa de Unity Recorder para grabar cada carrera.
 *
 * SETUP:
 *   1. Agrega este script a cualquier GameObject de la escena.
 *   2. Arrastra el RaceOrchestrator al campo raceOrchestrator.
 *   3. Configura outputFolder con el boton explorador en el Inspector.
 *   4. Arrastra el BatchRecorder al campo batchRecorder del RaceOrchestrator.
 */

using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

public class BatchRecorder : MonoBehaviour
{
    [Header("Referencias")]
    public RaceOrchestrator raceOrchestrator;

    [Header("Configuracion de Grabacion")]
    [Tooltip("Usar el boton explorador en el Inspector para seleccionar la carpeta")]
    public string outputFolder = "";
    public int frameWidth = 4096;
    public int frameHeight = 2048;
    public int frameRate = 30;

    [Tooltip("Segundos de espera antes de iniciar la grabacion (para evitar grabar la apertura de puertas)")]
    public float DelayDeGrabacion = 1.0f;

    private RecorderController _recorderController;
    private RecorderControllerSettings _controllerSettings;
    private string _pendingFileName = "";

    // -------------------------------------------------------------------------
    void Awake()
    {
        if (raceOrchestrator == null)
            raceOrchestrator = FindObjectOfType<RaceOrchestrator>();

        if (raceOrchestrator == null)
        {
            Debug.LogError("[BatchRecorder] No se encontro RaceOrchestrator.");
            return;
        }

        raceOrchestrator.OnCarreraTerminada += OnCarreraTerminada;
        Debug.Log("[BatchRecorder] Inicializado con Unity Recorder.");
    }

    void OnDestroy()
    {
        if (raceOrchestrator != null)
            raceOrchestrator.OnCarreraTerminada -= OnCarreraTerminada;

        if (_recorderController != null && _recorderController.IsRecording())
            _recorderController.StopRecording();
    }

    // -------------------------------------------------------------------------
    public void IniciarGrabacion(string horseAName, string horseBName, string winType, int raceID)
    {
        _pendingFileName = string.Format("{0}_vs_{1}_{2}_race{3:000}",
            SanitizeName(horseAName),
            SanitizeName(horseBName),
            SanitizeName(winType),
            raceID);

        StartCoroutine(IniciarGrabacionConDelay(_pendingFileName));
    }

    private IEnumerator IniciarGrabacionConDelay(string fileName)
    {
        yield return new WaitForSeconds(DelayDeGrabacion);

        string folder = string.IsNullOrEmpty(outputFolder) ? "Captures/Batch" : outputFolder;
        string fullPath = Path.Combine(folder, fileName);

        _controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        _controllerSettings.SetRecordModeToManual();
        _controllerSettings.FrameRate = frameRate;

        var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorder.name = "BatchMovieRecorder";
        movieRecorder.Enabled = true;
        movieRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;

        movieRecorder.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = frameWidth,
            OutputHeight = frameHeight
        };

        movieRecorder.OutputFile = fullPath;

        _controllerSettings.AddRecorderSettings(movieRecorder);

        _recorderController = new RecorderController(_controllerSettings);
        _recorderController.PrepareRecording();
        _recorderController.StartRecording();

        Debug.LogFormat("[BatchRecorder] Grabacion iniciada: {0}", fileName);
    }

    // -------------------------------------------------------------------------
    private void OnCarreraTerminada()
    {
        StopAllCoroutines();

        if (_recorderController != null && _recorderController.IsRecording())
        {
            _recorderController.StopRecording();
            Debug.Log("[BatchRecorder] Grabacion detenida.");
        }

        if (raceOrchestrator != null)
            raceOrchestrator.ProcederConSiguienteCarrera();
    }

    // -------------------------------------------------------------------------
    private string SanitizeName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
    }
}