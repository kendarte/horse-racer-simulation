using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public enum RaceResultType
{
    HA_Long_Win,      // 0
    HB_Long_Win,      // 1
    HA_Short_Win,     // 2
    HB_Short_Win,     // 3
    HA_Photo_Finish,  // 4
    HB_Photo_Finish,  // 5
    Tie_Dead_Heat     // 6
}

public class RaceOrchestrator : MonoBehaviour
{
    [Header("1. CABALLOS")]
    public RaceEngine[] AllHorses;

    [Header("2. PISTA (Objeto TRACK_GENERATOR)")]
    public Transform TrackPadre;

    [Header("3. CAMARA")]
    public CinematicCamera MainCamera;

    [Header("4. UI Y EFECTOS")]
    public Text CountdownText;
    public AudioSource AmbientAudio;

    [Header("5. CONTROL DE CARRERA (SISTEMA AUTO RUN)")]
    public float TiempoDeEsperaInicial = 2.0f;
    public bool ActivarAutoRun = false;
    [Range(1, 315)] public int StartRaceID = 1;
    [Range(1, 315)] public int EndRaceID = 315;
    public float BaseTime = 30.0f;

    [Header("6. PUNTOS DE INICIO Y FIN")]
    public Transform StartPoint;
    public Transform FinishPoint;

    [Header("7. PUERTAS DE SALIDA")]
    public Animation PuertaCarril1;
    public Animation PuertaCarril2;
    public AnimationClip ClipAbrirPuerta;
    public float VelocidadAperturaPuertas = 1.0f;
    public float TiempoEsperaPuertas = 5.0f;
    public AudioSource SonidoAbrirPuertas;

    [Header("8. TEXTO DE VICTORIA")]
    public Font FuenteVictoria;
    public int TamanoTextoVictoria = 80;
    public Color ColorTextoVictoria = Color.yellow;
    public float TiempoAparicionVictoria = 5.0f;

    [Header("9. BATCH RECORDER (Opcional)")]
    public BatchRecorder batchRecorder;

    public System.Action OnCarreraTerminada;
    public int CurrentRaceID { get { return _currentRaceID; } }

    private Canvas _victoriaCanvas;
    private Text _victoriaText;
    private Text _victoriaSombra;

    private int _currentRaceID = 1;
    private string _horseAName = "";
    private string _horseBName = "";
    private string _winType = "";
    private int _setupID = 1;

    private const string PREF_CURRENT_ID = "AutoRun_CurrentID";
    private const string PREF_IS_AUTORUN = "AutoRun_Active";
    private const string PREF_END_ID = "AutoRun_EndID";

    // -------------------------------------------------------------------------
    void Start()
    {
        if (PlayerPrefs.GetInt(PREF_IS_AUTORUN, 0) == 1)
        {
            _currentRaceID = PlayerPrefs.GetInt(PREF_CURRENT_ID, StartRaceID);
            EndRaceID = PlayerPrefs.GetInt(PREF_END_ID, EndRaceID);
            Debug.Log("[RO] Start - RELOAD. CurrentID=" + _currentRaceID + " EndID=" + EndRaceID);
        }
        else if (ActivarAutoRun)
        {
            _currentRaceID = StartRaceID;
            PlayerPrefs.SetInt(PREF_IS_AUTORUN, 1);
            PlayerPrefs.SetInt(PREF_CURRENT_ID, StartRaceID);
            PlayerPrefs.SetInt(PREF_END_ID, EndRaceID);
            PlayerPrefs.Save();
            Debug.Log("[RO] Start - PRIMERA VEZ. CurrentID=" + _currentRaceID + " EndID=" + EndRaceID);
        }
        else
        {
            _currentRaceID = StartRaceID;
            Debug.Log("[RO] Start - SIN AUTORUN.");
        }

        CrearCanvasVictoria();
        SetupRace(_currentRaceID);
    }

    // -------------------------------------------------------------------------
    void CrearCanvasVictoria()
    {
        GameObject canvasGO = new GameObject("Victoria_Canvas");
        _victoriaCanvas = canvasGO.AddComponent<Canvas>();
        _victoriaCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _victoriaCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        Font fuente = FuenteVictoria != null
            ? FuenteVictoria
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Sombra
        GameObject sombraGO = new GameObject("Victoria_Sombra");
        sombraGO.transform.SetParent(canvasGO.transform, false);
        _victoriaSombra = sombraGO.AddComponent<Text>();
        _victoriaSombra.font = fuente;
        _victoriaSombra.fontSize = TamanoTextoVictoria;
        _victoriaSombra.alignment = TextAnchor.MiddleCenter;
        _victoriaSombra.color = Color.black;
        RectTransform rs = sombraGO.GetComponent<RectTransform>();
        rs.anchorMin = Vector2.zero;
        rs.anchorMax = Vector2.one;
        rs.offsetMin = new Vector2(4, -4);
        rs.offsetMax = new Vector2(4, -4);

        // Texto principal
        GameObject textoGO = new GameObject("Victoria_Texto");
        textoGO.transform.SetParent(canvasGO.transform, false);
        _victoriaText = textoGO.AddComponent<Text>();
        _victoriaText.font = fuente;
        _victoriaText.fontSize = TamanoTextoVictoria;
        _victoriaText.alignment = TextAnchor.MiddleCenter;
        _victoriaText.color = ColorTextoVictoria;
        RectTransform rt = textoGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _victoriaCanvas.gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------
    public void SetupRace(int id)
    {
        if (TrackPadre == null || TrackPadre.childCount < 2) return;

        Transform Lane_1 = TrackPadre.GetChild(0);
        Transform Lane_2 = TrackPadre.GetChild(1);

        int adjustedID = Mathf.Clamp(id - 1, 0, 314);
        int pairIndex = adjustedID / 7;
        int outcomeIndex = adjustedID % 7;

        int indexA, indexB;
        GetHorsesForPair(pairIndex, out indexA, out indexB);

        for (int i = 0; i < AllHorses.Length; i++)
            if (AllHorses[i] != null) AllHorses[i].gameObject.SetActive(false);

        RaceEngine horseA = AllHorses[indexA];
        RaceEngine horseB = AllHorses[indexB];
        horseA.gameObject.SetActive(true);
        horseB.gameObject.SetActive(true);

        float timeA = BaseTime;
        float timeB = BaseTime;
        Transform winnerTransform = horseA.transform;
        string winnerName = "";

        _horseAName = horseA.gameObject.name;
        _horseBName = horseB.gameObject.name;
        _setupID = id;

        switch (outcomeIndex)
        {
            case 0: timeB += 2.0f; winnerName = _horseAName; _winType = "Long_Win"; break;
            case 1: timeA += 2.0f; winnerTransform = horseB.transform; winnerName = _horseBName; _winType = "Long_Win"; break;
            case 2: timeB += 0.5f; winnerName = _horseAName; _winType = "Short_Win"; break;
            case 3: timeA += 0.5f; winnerTransform = horseB.transform; winnerName = _horseBName; _winType = "Short_Win"; break;
            case 4: timeB += 0.05f; winnerName = _horseAName; _winType = "Photo_Finish"; break;
            case 5: timeA += 0.05f; winnerTransform = horseB.transform; winnerName = _horseBName; _winType = "Photo_Finish"; break;
            case 6: winnerName = _horseAName + " & " + _horseBName; _winType = "Dead_Heat"; break;
        }

        horseA.ConfigureRace(Lane_1, timeA, StartPoint, FinishPoint);
        horseB.ConfigureRace(Lane_2, timeB, StartPoint, FinishPoint);
        if (MainCamera != null) MainCamera.SetTarget(winnerTransform);

        Invoke("AbrirPuertas", TiempoEsperaPuertas);
        StartCoroutine(RaceCountdownRoutine(winnerName, _winType, Mathf.Max(timeA, timeB)));
    }

    // -------------------------------------------------------------------------
    IEnumerator RaceCountdownRoutine(string winner, string type, float dur)
    {
        if (AmbientAudio != null) { AmbientAudio.Stop(); AmbientAudio.time = 0f; AmbientAudio.Play(); }
        yield return new WaitForSeconds(TiempoDeEsperaInicial);
        if (CountdownText != null)
        {
            CountdownText.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--) { CountdownText.text = i.ToString(); yield return new WaitForSeconds(1f); }
            CountdownText.text = "GO!";
        }
        else yield return new WaitForSeconds(3f);

        StartMotors();

        if (batchRecorder != null)
            batchRecorder.IniciarGrabacion(_horseAName, _horseBName, _winType, _setupID);

        yield return new WaitForSeconds(1f);
        if (CountdownText != null) CountdownText.gameObject.SetActive(false);
        yield return new WaitForSeconds(dur - 1f);
        StartCoroutine(DisplayWinnerTextRoutine(winner, type));
    }

    // -------------------------------------------------------------------------
    IEnumerator DisplayWinnerTextRoutine(string winner, string type)
    {
        string txt = (type == "Dead_Heat") ? winner + " WIN!\nTIE" : winner + " WINS!";

        _victoriaText.text = txt;
        _victoriaSombra.text = txt;
        _victoriaCanvas.gameObject.SetActive(true);

        yield return new WaitForSeconds(TiempoAparicionVictoria);
        _victoriaCanvas.gameObject.SetActive(false);

        if (OnCarreraTerminada != null) OnCarreraTerminada.Invoke();

        if (batchRecorder == null)
            ProcederConSiguienteCarrera();
    }

    // -------------------------------------------------------------------------
    public void ProcederConSiguienteCarrera()
    {
        if (PlayerPrefs.GetInt(PREF_IS_AUTORUN, 0) != 1) return;

        if (_currentRaceID >= EndRaceID)
        {
            PlayerPrefs.DeleteKey(PREF_CURRENT_ID);
            PlayerPrefs.DeleteKey(PREF_IS_AUTORUN);
            PlayerPrefs.DeleteKey(PREF_END_ID);
            PlayerPrefs.Save();
            Debug.Log("[RO] AutoRun completo. Saliendo del Play Mode.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        _currentRaceID++;
        PlayerPrefs.SetInt(PREF_CURRENT_ID, _currentRaceID);
        PlayerPrefs.SetInt(PREF_IS_AUTORUN, 1);
        PlayerPrefs.SetInt(PREF_END_ID, EndRaceID);
        PlayerPrefs.Save();

        Debug.Log("[RO] Avanzando a carrera " + _currentRaceID + ". Recargando escena...");

        CancelInvoke();
        SceneManager.LoadScene(0);
    }

    // -------------------------------------------------------------------------
    void AbrirPuertas()
    {
        if (ClipAbrirPuerta != null) { if (PuertaCarril1 != null) PlayDoorAnimation(PuertaCarril1); if (PuertaCarril2 != null) PlayDoorAnimation(PuertaCarril2); }
        if (SonidoAbrirPuertas != null) { SonidoAbrirPuertas.time = 0f; SonidoAbrirPuertas.Play(); }
    }

    void PlayDoorAnimation(Animation anim)
    {
        if (anim[ClipAbrirPuerta.name] == null) anim.AddClip(ClipAbrirPuerta, ClipAbrirPuerta.name);
        anim[ClipAbrirPuerta.name].speed = VelocidadAperturaPuertas;
        anim.Play(ClipAbrirPuerta.name);
    }

    void StartMotors()
    {
        foreach (var h in AllHorses) if (h != null && h.gameObject.activeSelf) h.StartRunning();
    }

    void GetHorsesForPair(int pID, out int h1, out int h2)
    {
        int counter = 0; h1 = 0; h2 = 1;
        for (int i = 0; i < 10; i++) { for (int j = i + 1; j < 10; j++) { if (counter == pID) { h1 = i; h2 = j; return; } counter++; } }
    }

    public void IniciarAutoRun()
    {
        PlayerPrefs.SetInt(PREF_IS_AUTORUN, 1);
        PlayerPrefs.SetInt(PREF_CURRENT_ID, StartRaceID);
        PlayerPrefs.SetInt(PREF_END_ID, EndRaceID);
        PlayerPrefs.Save();
    }

    public void DetenerAutoRun()
    {
        PlayerPrefs.DeleteKey(PREF_CURRENT_ID);
        PlayerPrefs.DeleteKey(PREF_IS_AUTORUN);
        PlayerPrefs.DeleteKey(PREF_END_ID);
        PlayerPrefs.Save();
    }
}