using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TomaTV
{
    public string NombreToma = "Nueva Toma";

    public enum PresetEstilo
    {
        Personalizado,
        TrackingCercano,
        TrackingLejano,
        TomaFrontalCurva,
        PhotoFinishMeta,
        TrackingSuelo,
        PersecucionTrasera,
        TomaEstaticaLejana,
        TramoAleatorio,
        BroadcastInteligente
    }

    [Header("PRESET RÁPIDO")]
    public PresetEstilo EstiloDeToma = PresetEstilo.Personalizado;

    [Header("Gatillo Automático (Porcentaje Exacto de Pista)")]
    public bool UsarPorcentaje = true;
    [Range(0, 100)] public float PorcentajeDeCarrera = 0f;

    [Header("Tipo de Cámara (Física)")]
    public bool EsCamaraFijaEnElLugar = false;
    public bool NoSeguirConLaMirada = false;

    [Header("Configuración del Carrito de TV")]
    public float DistanciaLateral = 25f;
    public float AlturaCamara = 6f;
    public float Adelanto = 3f;

    [Header("Lente (Zoom)")]
    [Range(10, 100)] public float FieldOfView = 25f;

    [Header("Modos Dinámicos (Aleatorio y Broadcast)")]
    public bool EsTramoAleatorio = false;
    public bool EsTramoBroadcast = false;
    public float TiempoMinCorte = 2f;
    public float TiempoMaxCorte = 5f;

    [HideInInspector] public float ActivarAMetrosRecorridos = 0f;
    [HideInInspector] public float ActivarADistancia = 1000f;
}

public class CinematicCamera : MonoBehaviour
{
    [Header("Objetivo de Transmisión (Failsafe)")]
    public Transform TargetHorse;

    [HideInInspector] public TipoAngulo AnguloActual = TipoAngulo.TrackingLateral;
    public enum TipoAngulo { TrackingLateral, TomaFrontal, MetaFija }
    [HideInInspector] public bool UsarCortesMatematicos = true;
    [HideInInspector] public float DistanciaCorteFrontal = 60f;
    [HideInInspector] public float DistanciaCortePhotoFinish = 20f;
    [HideInInspector] public float Frontal_DistanciaLateral = 5f;
    [HideInInspector] public float Frontal_Altura = 4f;
    [HideInInspector] public float Frontal_Adelanto = 35f;
    [HideInInspector] public float Frontal_FOV = 20f;
    [HideInInspector] public Transform PuntoMetaFija;
    [HideInInspector] public float Meta_FOV = 35f;

    [Header("Automatización Matemática (Pista y Waypoints)")]
    public Transform TrackPadre;
    public Transform PuntoInicio;
    public Transform PuntoMetaFinal;

    [Header("Suavizado de Cámara (Variables Antiguas Ocultas)")]
    [HideInInspector] public float PesoDelRiel = 2.5f;
    [HideInInspector] public float PesoDelPaneo = 1.5f;
    [HideInInspector] public float InerciaDeSeparacion = 0.8f;

    [Header("Encuadre por Rotación (Variables Antiguas Ocultas)")]
    [HideInInspector] public bool EncuadrePorRotacion = true;
    [HideInInspector] public float AnclajeAlLider = 0.85f;
    [HideInInspector] public float FuerzaDeMiradaAtras = 0.6f;

    [Header("Auto-Encuadre Antiguo (Variables Antiguas Ocultas)")]
    [HideInInspector] public bool AutoEncuadre = false;
    [HideInInspector] public float DistanciaExtraPorSeparacion = 0.8f;

    [HideInInspector] public bool EnfocarAlLider = true;
    [HideInInspector] public float IntensidadDeEnfoque = 0.6f;
    [HideInInspector] public float VelocidadTransicionEnfoque = 2f;

    [Header("Photo Finish Real")]
    [Tooltip("Coloque un objeto vacío en la meta. La cámara copiará su POSICIÓN Y ROTACIÓN exactas.")]
    public Transform PosteMetaEstatico;

    [Header("Sincronización de Velocidad")]
    public float DuracionDeCarrera = 30f;
    public bool AutoSincronizarVelocidad = true;

    [HideInInspector] public bool UsarModoAleatorio = false;
    [HideInInspector] public float TiempoMinEntreCortes = 3f;
    [HideInInspector] public float TiempoMaxEntreCortes = 6f;
    [HideInInspector] public bool ForzarMetaEnRandom = true;

    public enum PresetGiro
    {
        Personalizado,
        GiroEpico,
        EspaldaEstatica,
        CenitalRapido,
        PaneoLento
    }

    [Header(">>> EDITE EL GIRO INICIAL AQUÍ <<<")]
    public PresetGiro EstiloGiroInicial = PresetGiro.Personalizado;
    public bool UsarTomaInicial = true;
    public float TiempoTomaInicial = 4.0f;
    public float AnguloInicialGiro = 0f;
    public float VelocidadRotacionInicial = 40f;
    public float RadioGiroInicial = 8f;
    public float AlturaGiroInicial = 2.5f;
    [Range(10, 100)] public float FOVInicial = 35f;

    [Header("2. Cadena de Tomas (Secuencia Editable)")]
    public List<TomaTV> SecuenciaDeTomas = new List<TomaTV>();

    [Header("Movimiento del Camarógrafo")]
    public float VelocidadRiel = 10f;
    public float VelocidadGiro = 15f;

    // --- VARIABLES INTERNAS ---
    private Camera _cam;
    private bool _corteInstantaneo = false;
    private float _cronometroInicial = 0f;
    private float _anguloGiroActual = 0f;
    private bool _carreraIniciada = false;
    private TomaTV _tomaActual;

    private Vector3 _posicionAnteriorDelPeloton = Vector3.zero;
    private float _metrosAcumulados = 0f;

    private List<Vector3> _puntosDeLaPista = new List<Vector3>();
    private float _distanciaTotalPista = 1f;

    private Vector3 _offsetActual = Vector3.zero;
    private Vector3 _offsetVelocity = Vector3.zero;
    private Vector3 _lookVelocity = Vector3.zero;
    private Vector3 _direccionAdelanteSuavizada = Vector3.forward;

    private Vector3 _posicionFijaDeCamara = Vector3.zero;
    private Quaternion _rotacionFijaDeCamara = Quaternion.identity;

    private int _historial1 = -1;
    private int _historial2 = -1;
    private int _historial3 = -1;

    private float _timerAleatorioLocal = 0f;
    private TomaTV _subTomaAleatoria;

    private float _timerBroadcast = 0f;
    private TomaTV _subTomaBroadcast;

    private Vector3 _puntoDeMiraSuavizado = Vector3.zero;
    private float _maxProgresoHistorico = 0f;
    private float _distanciaSeparacionSuavizada = 0f;
    private Vector3 _centroCarreraSuavizado = Vector3.zero;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        if (SecuenciaDeTomas.Count == 0)
        {
            SecuenciaDeTomas.Add(new TomaTV { NombreToma = "Arranque", EstiloDeToma = TomaTV.PresetEstilo.TrackingCercano, UsarPorcentaje = true, PorcentajeDeCarrera = 0f });
            SecuenciaDeTomas.Add(new TomaTV { NombreToma = "Director TV", EstiloDeToma = TomaTV.PresetEstilo.BroadcastInteligente, UsarPorcentaje = true, PorcentajeDeCarrera = 15f });
            SecuenciaDeTomas.Add(new TomaTV { NombreToma = "Meta", EstiloDeToma = TomaTV.PresetEstilo.PhotoFinishMeta, UsarPorcentaje = true, PorcentajeDeCarrera = 90f });
        }
    }

    void OnValidate()
    {
        if (EstiloGiroInicial == PresetGiro.GiroEpico)
        {
            TiempoTomaInicial = 4f; AnguloInicialGiro = 0f; VelocidadRotacionInicial = 45f; RadioGiroInicial = 10f; AlturaGiroInicial = 3f; FOVInicial = 35f;
        }
        else if (EstiloGiroInicial == PresetGiro.EspaldaEstatica)
        {
            TiempoTomaInicial = 3f; AnguloInicialGiro = 180f; VelocidadRotacionInicial = 0f; RadioGiroInicial = 6f; AlturaGiroInicial = 2.5f; FOVInicial = 40f;
        }
        else if (EstiloGiroInicial == PresetGiro.CenitalRapido)
        {
            TiempoTomaInicial = 3f; AnguloInicialGiro = 90f; VelocidadRotacionInicial = 90f; RadioGiroInicial = 3f; AlturaGiroInicial = 12f; FOVInicial = 50f;
        }
        else if (EstiloGiroInicial == PresetGiro.PaneoLento)
        {
            TiempoTomaInicial = 5f; AnguloInicialGiro = -45f; VelocidadRotacionInicial = 15f; RadioGiroInicial = 12f; AlturaGiroInicial = 4f; FOVInicial = 30f;
        }

        if (SecuenciaDeTomas != null)
        {
            foreach (TomaTV toma in SecuenciaDeTomas)
            {
                if (toma.EstiloDeToma == TomaTV.PresetEstilo.TrackingCercano)
                {
                    toma.DistanciaLateral = 20f; toma.AlturaCamara = 5f; toma.Adelanto = 2f; toma.FieldOfView = 30f;
                    toma.EsCamaraFijaEnElLugar = false; toma.NoSeguirConLaMirada = false; toma.EsTramoAleatorio = false; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.TrackingLejano)
                {
                    toma.DistanciaLateral = 35f; toma.AlturaCamara = 12f; toma.Adelanto = 5f; toma.FieldOfView = 25f;
                    toma.EsCamaraFijaEnElLugar = false; toma.NoSeguirConLaMirada = false; toma.EsTramoAleatorio = false; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.TrackingSuelo)
                {
                    toma.DistanciaLateral = 15f; toma.AlturaCamara = 1.5f; toma.Adelanto = 0f; toma.FieldOfView = 35f;
                    toma.EsCamaraFijaEnElLugar = false; toma.NoSeguirConLaMirada = false; toma.EsTramoAleatorio = false; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.PersecucionTrasera)
                {
                    toma.DistanciaLateral = 0f; toma.AlturaCamara = 4f; toma.Adelanto = -18f; toma.FieldOfView = 30f;
                    toma.EsCamaraFijaEnElLugar = false; toma.NoSeguirConLaMirada = false; toma.EsTramoAleatorio = false; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.TomaFrontalCurva)
                {
                    toma.DistanciaLateral = 5f; toma.AlturaCamara = 4f; toma.Adelanto = 35f; toma.FieldOfView = 20f;
                    toma.EsCamaraFijaEnElLugar = true; toma.NoSeguirConLaMirada = false; toma.EsTramoAleatorio = false; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.TomaEstaticaLejana)
                {
                    toma.DistanciaLateral = 45f; toma.AlturaCamara = 15f; toma.Adelanto = 25f; toma.FieldOfView = 15f;
                    toma.EsCamaraFijaEnElLugar = true; toma.NoSeguirConLaMirada = false; toma.EsTramoAleatorio = false; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.PhotoFinishMeta)
                {
                    toma.DistanciaLateral = 8f; toma.AlturaCamara = 3f; toma.Adelanto = 30f; toma.FieldOfView = 20f;
                    toma.EsCamaraFijaEnElLugar = true; toma.NoSeguirConLaMirada = true; toma.EsTramoAleatorio = false; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.TramoAleatorio)
                {
                    toma.EsTramoAleatorio = true; toma.EsTramoBroadcast = false;
                }
                else if (toma.EstiloDeToma == TomaTV.PresetEstilo.BroadcastInteligente)
                {
                    toma.EsTramoBroadcast = true; toma.EsTramoAleatorio = false;
                }
            }
        }
    }

    private float CalcularMetrosRecorridos(Vector3 posicion)
    {
        if (_puntosDeLaPista.Count < 2) return 0f;
        float distanciaRecorrida = 0f;
        int closestIndex = 0;
        float minD = float.MaxValue;

        for (int i = 0; i < _puntosDeLaPista.Count; i++)
        {
            float d = Vector3.Distance(posicion, _puntosDeLaPista[i]);
            if (d < minD) { minD = d; closestIndex = i; }
        }

        for (int i = 0; i < closestIndex; i++)
        {
            distanciaRecorrida += Vector3.Distance(_puntosDeLaPista[i], _puntosDeLaPista[i + 1]);
        }
        return distanciaRecorrida;
    }

    private void ActualizarHistorial(int nuevaToma)
    {
        _historial3 = _historial2;
        _historial2 = _historial1;
        _historial1 = nuevaToma;
    }

    private TomaTV GenerarTomaAleatoriaTemporal()
    {
        TomaTV temp = new TomaTV();
        List<int> validas = new List<int> { 0, 1, 2, 3, 4, 5 };
        List<int> filtradas = new List<int>();

        foreach (int t in validas)
        {
            if (t != _historial1 && t != _historial2 && t != _historial3) filtradas.Add(t);
        }

        int rand = filtradas.Count > 0 ? filtradas[Random.Range(0, filtradas.Count)] : validas[Random.Range(0, validas.Count)];
        ActualizarHistorial(rand);

        switch (rand)
        {
            case 0: temp.DistanciaLateral = 20f; temp.AlturaCamara = 5f; temp.Adelanto = 2f; temp.FieldOfView = 30f; temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 1: temp.DistanciaLateral = 35f; temp.AlturaCamara = 12f; temp.Adelanto = 5f; temp.FieldOfView = 25f; temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 2: temp.DistanciaLateral = 5f; temp.AlturaCamara = 4f; temp.Adelanto = 35f; temp.FieldOfView = 20f; temp.EsCamaraFijaEnElLugar = true; temp.NoSeguirConLaMirada = false; break;
            case 3: temp.DistanciaLateral = 15f; temp.AlturaCamara = 1.5f; temp.Adelanto = 0f; temp.FieldOfView = 35f; temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 4: temp.DistanciaLateral = 0f; temp.AlturaCamara = 4f; temp.Adelanto = -18f; temp.FieldOfView = 30f; temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 5: temp.DistanciaLateral = 45f; temp.AlturaCamara = 15f; temp.Adelanto = 25f; temp.FieldOfView = 15f; temp.EsCamaraFijaEnElLugar = true; temp.NoSeguirConLaMirada = false; break;
        }
        return temp;
    }

    private TomaTV GenerarTomaBroadcastTemporal(float separacion)
    {
        TomaTV temp = new TomaTV();
        List<int> tomasValidas = new List<int>();

        if (separacion < 12f)
        {
            tomasValidas.AddRange(new int[] { 0, 1, 2, 3 });
        }
        else if (separacion < 25f)
        {
            tomasValidas.AddRange(new int[] { 2, 3, 5, 0 });
        }
        else
        {
            tomasValidas.AddRange(new int[] { 4, 5, 2 });
        }

        List<int> tomasFiltradas = new List<int>();
        foreach (int t in tomasValidas)
        {
            if (t != _historial1 && t != _historial2 && t != _historial3)
            {
                tomasFiltradas.Add(t);
            }
        }

        int rand = 0;
        if (tomasFiltradas.Count > 0) rand = tomasFiltradas[Random.Range(0, tomasFiltradas.Count)];
        else rand = tomasValidas[Random.Range(0, tomasValidas.Count)];

        ActualizarHistorial(rand);

        switch (rand)
        {
            case 0:
                temp.DistanciaLateral = 20f; temp.AlturaCamara = 5f; temp.Adelanto = 2f; temp.FieldOfView = 30f;
                temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 1:
                temp.DistanciaLateral = 15f; temp.AlturaCamara = 1.5f; temp.Adelanto = 0f; temp.FieldOfView = 35f;
                temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 2:
                temp.DistanciaLateral = 35f; temp.AlturaCamara = 12f; temp.Adelanto = 5f; temp.FieldOfView = 25f;
                temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 3:
                temp.DistanciaLateral = 0f; temp.AlturaCamara = 4f; temp.Adelanto = -18f; temp.FieldOfView = 30f;
                temp.EsCamaraFijaEnElLugar = false; temp.NoSeguirConLaMirada = false; break;
            case 4:
                temp.DistanciaLateral = 45f; temp.AlturaCamara = 15f; temp.Adelanto = 25f; temp.FieldOfView = 15f;
                temp.EsCamaraFijaEnElLugar = true; temp.NoSeguirConLaMirada = false; break;
        }
        return temp;
    }

    void LateUpdate()
    {
        // 1. SOLUCIÓN ABSOLUTA: LA CÁMARA SOLO SIGUE AL TARGET HORSE Y A NADIE MÁS.
        // Se eliminó la lógica de escanear corredores y buscar al líder.
        Vector3 basePos = Vector3.zero;
        Vector3 forwardRaw = Vector3.forward;

        if (TargetHorse != null)
        {
            basePos = TargetHorse.position;
            forwardRaw = TargetHorse.forward;
        }

        // Necesario para el modo Broadcast: Medimos la separación general de la carrera
        // (Pero esto NO se usa para apuntar la cámara, solo para saber si deben abrir la toma)
        RaceEngine[] corredores = FindObjectsOfType<RaceEngine>();
        float minProgreso = float.MaxValue;
        float maxProgreso = -1f;
        Vector3 posPrimero = Vector3.zero;
        Vector3 posUltimo = Vector3.zero;

        if (corredores != null && corredores.Length > 0)
        {
            foreach (RaceEngine caballo in corredores)
            {
                if (caballo.gameObject.activeInHierarchy)
                {
                    float progresoCaballo = CalcularMetrosRecorridos(caballo.transform.position);

                    if (progresoCaballo > maxProgreso)
                    {
                        maxProgreso = progresoCaballo;
                        posPrimero = caballo.transform.position;
                    }
                    if (progresoCaballo < minProgreso)
                    {
                        minProgreso = progresoCaballo;
                        posUltimo = caballo.transform.position;
                    }
                }
            }
        }

        _centroCarreraSuavizado = basePos;

        if (_posicionAnteriorDelPeloton != Vector3.zero)
        {
            Vector3 moveDir = basePos - _posicionAnteriorDelPeloton;
            moveDir.y = 0;
            if (moveDir.sqrMagnitude > 0.001f) forwardRaw = moveDir.normalized;
        }

        _direccionAdelanteSuavizada = Vector3.Lerp(_direccionAdelanteSuavizada, forwardRaw, Time.deltaTime * 6f).normalized;
        Vector3 direccionDerechaSuavizada = Vector3.Cross(Vector3.up, _direccionAdelanteSuavizada).normalized;

        if (UsarTomaInicial && _cronometroInicial < TiempoTomaInicial)
        {
            _cronometroInicial += Time.deltaTime;
            _anguloGiroActual += VelocidadRotacionInicial * Time.deltaTime;

            float radianes = _anguloGiroActual * Mathf.Deg2Rad;
            Vector3 offsetGiro = new Vector3(Mathf.Sin(radianes) * RadioGiroInicial, AlturaGiroInicial, Mathf.Cos(radianes) * RadioGiroInicial);
            Vector3 centroGiro = (PuntoInicio != null) ? PuntoInicio.position : basePos;

            transform.position = centroGiro + offsetGiro;
            transform.LookAt(centroGiro + (Vector3.up * 1.5f));
            if (_cam != null) _cam.fieldOfView = FOVInicial;

            _posicionAnteriorDelPeloton = basePos;
        }
        else
        {
            if (!_carreraIniciada)
            {
                _carreraIniciada = true;
                _corteInstantaneo = true;
            }

            if (_posicionAnteriorDelPeloton != Vector3.zero)
            {
                _metrosAcumulados += Vector3.Distance(_posicionAnteriorDelPeloton, basePos);
            }
            _posicionAnteriorDelPeloton = basePos;

            float progresoActual = 0f;
            if (_puntosDeLaPista.Count > 1)
            {
                float distanciaRecorridaCentro = CalcularMetrosRecorridos(basePos);
                progresoActual = (distanciaRecorridaCentro / _distanciaTotalPista) * 100f;
                progresoActual = Mathf.Clamp(progresoActual, 0f, 100f);
            }

            _maxProgresoHistorico = Mathf.Max(_maxProgresoHistorico, progresoActual);

            TomaTV mejorToma = SecuenciaDeTomas[0];
            float maxValorAlcanzado = -1f;

            foreach (TomaTV toma in SecuenciaDeTomas)
            {
                if (toma.UsarPorcentaje)
                {
                    if (_maxProgresoHistorico >= toma.PorcentajeDeCarrera && toma.PorcentajeDeCarrera > maxValorAlcanzado)
                    {
                        maxValorAlcanzado = toma.PorcentajeDeCarrera;
                        mejorToma = toma;
                    }
                }
                else
                {
                    if (_metrosAcumulados >= toma.ActivarAMetrosRecorridos && toma.ActivarAMetrosRecorridos > maxValorAlcanzado)
                    {
                        maxValorAlcanzado = toma.ActivarAMetrosRecorridos;
                        mejorToma = toma;
                    }
                }
            }

            if (_tomaActual != mejorToma)
            {
                _tomaActual = mejorToma;
                _corteInstantaneo = true;
            }

            TomaTV tomaActivaParaPosicion = _tomaActual;

            if (_tomaActual != null && _tomaActual.EsTramoBroadcast)
            {
                _timerBroadcast -= Time.deltaTime;
                float distanciaReal = Vector3.Distance(posPrimero, posUltimo);

                bool forzarCortePorSeparacion = false;
                if (_subTomaBroadcast != null)
                {
                    if (distanciaReal > 25f && _historial1 < 2) forzarCortePorSeparacion = true;
                }

                if (_timerBroadcast <= 0f || _subTomaBroadcast == null || forzarCortePorSeparacion)
                {
                    _timerBroadcast = Random.Range(3.5f, 6.5f);
                    _subTomaBroadcast = GenerarTomaBroadcastTemporal(distanciaReal);
                    _corteInstantaneo = true;
                }
                tomaActivaParaPosicion = _subTomaBroadcast;
            }
            else if (_tomaActual != null && _tomaActual.EsTramoAleatorio)
            {
                _timerAleatorioLocal -= Time.deltaTime;
                if (_timerAleatorioLocal <= 0f || _subTomaAleatoria == null)
                {
                    _timerAleatorioLocal = Random.Range(_tomaActual.TiempoMinCorte, _tomaActual.TiempoMaxCorte);
                    _subTomaAleatoria = GenerarTomaAleatoriaTemporal();
                    _corteInstantaneo = true;
                }
                tomaActivaParaPosicion = _subTomaAleatoria;
            }

            Vector3 offsetDeseado = (direccionDerechaSuavizada * tomaActivaParaPosicion.DistanciaLateral)
                                  + (Vector3.up * tomaActivaParaPosicion.AlturaCamara)
                                  + (_direccionAdelanteSuavizada * tomaActivaParaPosicion.Adelanto);

            Vector3 posObjetivoMundo = basePos + offsetDeseado;

            if (_corteInstantaneo)
            {
                if (tomaActivaParaPosicion.EsCamaraFijaEnElLugar)
                {
                    if (tomaActivaParaPosicion.EstiloDeToma == TomaTV.PresetEstilo.PhotoFinishMeta && PosteMetaEstatico != null)
                    {
                        _posicionFijaDeCamara = PosteMetaEstatico.position;
                    }
                    else
                    {
                        _posicionFijaDeCamara = basePos + offsetDeseado;
                    }
                }

                transform.position = tomaActivaParaPosicion.EsCamaraFijaEnElLugar ? _posicionFijaDeCamara : basePos + offsetDeseado;
            }
            else
            {
                if (tomaActivaParaPosicion.EsCamaraFijaEnElLugar)
                {
                    transform.position = _posicionFijaDeCamara;
                }
                else
                {
                    // FIX ABSOLUTO: Anclaje perfecto sin suavizado al caballo designado (TargetHorse)
                    transform.position = basePos + offsetDeseado;
                }
            }

            if (_cam != null) _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, tomaActivaParaPosicion.FieldOfView, Time.deltaTime * 5f);

            // MIRADA: Apunta ÚNICAMENTE al TargetHorse
            Vector3 lookTarget = basePos + (Vector3.up * 1.5f);

            if (tomaActivaParaPosicion.NoSeguirConLaMirada)
            {
                if (tomaActivaParaPosicion.EsCamaraFijaEnElLugar && tomaActivaParaPosicion.EstiloDeToma == TomaTV.PresetEstilo.PhotoFinishMeta && PosteMetaEstatico != null)
                {
                    transform.rotation = PosteMetaEstatico.rotation;
                }
                else
                {
                    if (_corteInstantaneo) _rotacionFijaDeCamara = Quaternion.LookRotation(-direccionDerechaSuavizada);
                    transform.rotation = _rotacionFijaDeCamara;
                }
            }
            else
            {
                Vector3 direccionCamara = lookTarget - transform.position;
                if (direccionCamara.sqrMagnitude > 0.001f)
                {
                    Vector3 vectorArriba = Vector3.up;

                    transform.rotation = Quaternion.LookRotation(direccionCamara, vectorArriba);
                }
            }

            _corteInstantaneo = false;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        TargetHorse = newTarget;
        _cronometroInicial = 0f;
        _anguloGiroActual = AnguloInicialGiro;
        _carreraIniciada = false;
        _tomaActual = null;
        _metrosAcumulados = 0f;
        _posicionAnteriorDelPeloton = Vector3.zero;

        _timerAleatorioLocal = 0f;
        _timerBroadcast = 0f;

        _historial1 = -1;
        _historial2 = -1;
        _historial3 = -1;

        _maxProgresoHistorico = 0f;
        _distanciaSeparacionSuavizada = 0f;

        _offsetVelocity = Vector3.zero;
        _lookVelocity = Vector3.zero;

        _puntosDeLaPista.Clear();
        _distanciaTotalPista = 0f;

        if (TrackPadre != null && TrackPadre.childCount > 0 && PuntoInicio != null && PuntoMetaFinal != null)
        {
            Transform carril = TrackPadre.GetChild(0);

            int startIndex = GetClosestWaypointIndex(carril, PuntoInicio.position);
            int finishIndex = GetClosestWaypointIndex(carril, PuntoMetaFinal.position);

            int currentIdx = startIndex;
            while (true)
            {
                _puntosDeLaPista.Add(carril.GetChild(currentIdx).position);
                if (currentIdx == finishIndex) break;
                currentIdx++;
                if (currentIdx >= carril.childCount) currentIdx = 0;
                if (_puntosDeLaPista.Count > carril.childCount + 1) break;
            }

            for (int i = 0; i < _puntosDeLaPista.Count - 1; i++)
                _distanciaTotalPista += Vector3.Distance(_puntosDeLaPista[i], _puntosDeLaPista[i + 1]);
        }

        if (_distanciaTotalPista <= 0f) _distanciaTotalPista = 1f;

        if (!UsarTomaInicial)
        {
            _corteInstantaneo = true;
            _carreraIniciada = true;
        }
    }

    private int GetClosestWaypointIndex(Transform lane, Vector3 position)
    {
        int closestIndex = 0;
        float minDistance = Mathf.Infinity;

        for (int i = 0; i < lane.childCount; i++)
        {
            float dist = Vector3.Distance(lane.GetChild(i).position, position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    public void CambiarAngulo(TipoAngulo nuevoAngulo)
    {
        AnguloActual = nuevoAngulo;
        _corteInstantaneo = true;
    }
}
