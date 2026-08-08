using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HorseNPC))]
public class RaceEngine : MonoBehaviour
{
    [Header("Efectos Visuales")]
    [Tooltip("Transform hijo del caballo donde spawnea el polvo (ej: las patas traseras)")]
    public Transform DustSpawnPoint;
    [Tooltip("Material de polvo. Usar Dust_Mat_1 al Dust_Mat_6.")]
    public Material DustMaterial;

    [Header("Polvo - Ajustes")]
    public float DustEmission = 10f;
    public float DustLifetime = 5f;
    public float DustSpeed = 5f;
    public float DustSize = 100f;
    public float DustRadius = 2.34f;
    public float DustAngle = 25f;

    [Header("Audio")]
    public AudioSource SonidoGalope;

    [Header("Fisicas de Carrera (Antiguas)")]
    public bool CorrerAlInstante = true;
    public float Aceleracion = 5f;
    public float PorcentajeParaCorrer = 0.4f;

    [Header("Nueva Transicion Suave de Aceleracion")]
    public bool UsarTiemposDeTransicion = true;
    public float TiempoDeTrote = 1.5f;
    public float TiempoDeAceleracion = 2.5f;

    private HorseNPC _visuals;
    private List<Vector3> _pathPoints = new List<Vector3>();
    private int _currentIndex = 0;
    private float _speed = 0f;
    private float _currentSpeed = 0f;
    private bool _isRunning = false;
    private bool _isTrotting = false;

    private float _targetDuration;
    private float _timerCarrera = 0f;
    private float[] _waypointDistances;
    private float _currentDistance = 0f;
    private float _totalPathDistance = 0f;
    private float _vMax = 0f;
    private float _tAcel = 0f;

    private ParticleSystem _dustInstance;

    // -------------------------------------------------------------------------
    void Awake()
    {
        _visuals = GetComponent<HorseNPC>();
        if (SonidoGalope != null) SonidoGalope.Stop();
    }

    // -------------------------------------------------------------------------
    public void ConfigureRace(Transform laneObj, float durationSeconds, Transform startPoint, Transform finishPoint)
    {
        _targetDuration = durationSeconds;
        _pathPoints.Clear();
        _timerCarrera = 0f;
        _currentDistance = 0f;

        if (_dustInstance != null) { Destroy(_dustInstance.gameObject); _dustInstance = null; }

        int startIndex = 0;
        int finishIndex = laneObj.childCount - 1;
        if (startPoint != null) startIndex = GetClosestWaypointIndex(laneObj, startPoint.position);
        if (finishPoint != null) finishIndex = GetClosestWaypointIndex(laneObj, finishPoint.position);

        int currentIdx = startIndex;
        while (true)
        {
            _pathPoints.Add(laneObj.GetChild(currentIdx).position);
            if (currentIdx == finishIndex) break;
            currentIdx++;
            if (currentIdx >= laneObj.childCount) currentIdx = 0;
            if (_pathPoints.Count > laneObj.childCount + 1) break;
        }

        _waypointDistances = new float[_pathPoints.Count];
        _waypointDistances[0] = 0f;
        _totalPathDistance = 0f;
        for (int i = 0; i < _pathPoints.Count - 1; i++)
        {
            float dist = Vector3.Distance(_pathPoints[i], _pathPoints[i + 1]);
            _totalPathDistance += dist;
            _waypointDistances[i + 1] = _totalPathDistance;
        }

        if (UsarTiemposDeTransicion)
        {
            _tAcel = Mathf.Min(TiempoDeAceleracion, _targetDuration * 0.4f);
            _vMax = _totalPathDistance / (_targetDuration - 0.5f * _tAcel);
        }
        else
        {
            if (CorrerAlInstante) { _tAcel = 0f; _vMax = _totalPathDistance / _targetDuration; }
            else { _tAcel = Mathf.Min(2.0f, _targetDuration * 0.4f); _vMax = _totalPathDistance / (_targetDuration - 0.5f * _tAcel); }
        }

        _speed = _vMax;
        _currentSpeed = 0f;

        if (_pathPoints.Count > 0)
        {
            transform.position = _pathPoints[0];
            if (_pathPoints.Count > 1)
            {
                Vector3 lookDir = _pathPoints[1] - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
            }
            _currentIndex = 1;
        }

        _visuals.PlayIdle();
    }

    // -------------------------------------------------------------------------
    public void StartRunning()
    {
        _isRunning = true;
        _timerCarrera = 0f;

        if (UsarTiemposDeTransicion)
        {
            _currentSpeed = 0f; _isTrotting = true; _visuals.PlayTrot(0.5f);
        }
        else
        {
            if (CorrerAlInstante) { _currentSpeed = _speed; _isTrotting = false; _visuals.PlayRun(_currentSpeed * 0.035f); }
            else { _currentSpeed = 0f; _isTrotting = true; _visuals.PlayTrot(0.5f); }
        }

        CrearPolvo();
        if (SonidoGalope != null) SonidoGalope.Play();
    }

    // -------------------------------------------------------------------------
    void Update()
    {
        if (!_isRunning || _pathPoints.Count == 0) return;

        _timerCarrera += Time.deltaTime;

        if (UsarTiemposDeTransicion)
        {
            if (_timerCarrera <= _tAcel)
            {
                float x = _timerCarrera / _tAcel;
                _currentDistance = (Mathf.Pow(x, 3) - 0.5f * Mathf.Pow(x, 4)) * _tAcel * _vMax;
                _currentSpeed = Mathf.SmoothStep(0f, _vMax, x);
            }
            else
            {
                _currentDistance = 0.5f * _tAcel * _vMax + (_timerCarrera - _tAcel) * _vMax;
                _currentSpeed = _vMax;
            }
        }
        else
        {
            if (CorrerAlInstante) { _currentDistance = _timerCarrera * _vMax; _currentSpeed = _vMax; }
            else
            {
                if (_timerCarrera <= _tAcel) { float x = _timerCarrera / _tAcel; _currentDistance = 0.5f * _vMax * x * _timerCarrera; _currentSpeed = _vMax * x; }
                else { _currentDistance = 0.5f * _tAcel * _vMax + (_timerCarrera - _tAcel) * _vMax; _currentSpeed = _vMax; }
            }
        }

        if (_currentDistance >= _totalPathDistance) _currentDistance = _totalPathDistance;

        if (UsarTiemposDeTransicion)
        {
            if (_timerCarrera < TiempoDeTrote)
            {
                if (!_isTrotting) { _isTrotting = true; _visuals.PlayTrot(_currentSpeed * 0.035f); }
                else _visuals.UpdateTrotSpeed(Mathf.Max(0.8f, _currentSpeed * 0.035f));
            }
            else
            {
                if (_isTrotting) { _isTrotting = false; _visuals.PlayRun(_currentSpeed * 0.035f); }
                else _visuals.UpdateRunSpeed(Mathf.Max(1.0f, _currentSpeed * 0.035f));
            }
        }
        else
        {
            float speedRatio = _vMax > 0f ? (_currentSpeed / _vMax) : 0f;
            if (_isTrotting)
            {
                if (speedRatio >= PorcentajeParaCorrer && !CorrerAlInstante) { _isTrotting = false; _visuals.PlayRun(_currentSpeed * 0.035f); }
                else _visuals.UpdateTrotSpeed(Mathf.Max(0.8f, _currentSpeed * 0.035f));
            }
            else _visuals.UpdateRunSpeed(Mathf.Max(1.0f, _currentSpeed * 0.035f));
        }

        Vector3 posTarget = GetPositionAtDistance(_currentDistance);
        posTarget.y = transform.position.y;
        transform.position = posTarget;

        float lookAheadDist = Mathf.Min(_currentDistance + 2.0f, _totalPathDistance);
        Vector3 lookTarget = GetPositionAtDistance(lookAheadDist);
        lookTarget.y = transform.position.y;
        Vector3 direction = lookTarget - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), Time.deltaTime * 10f);

        if (_currentDistance >= _totalPathDistance && _isRunning)
        {
            _isRunning = false;
            _visuals.PlayFinish();
            if (_dustInstance != null)
            {
                _dustInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(_dustInstance.gameObject, _dustInstance.main.startLifetime.constant + 1f);
                _dustInstance = null;
            }
            if (SonidoGalope != null) SonidoGalope.Stop();
        }
    }

    // -------------------------------------------------------------------------
    /// <summary>
    /// Crea el sistema de particulas replicando exactamente la fisica del prefab Smoke:
    /// - simulationSpace: Local
    /// - moveWithTransform: false
    /// - startLifetime: 5, startSpeed: 5, startSize: 100
    /// - gravityModifier: 0, maxParticles: 3500
    /// - emission: 10, shape cone angle 25 radius 2.34
    /// - ColorModule, SizeModule, RotationModule: desactivados
    /// - Color blanco con alpha 0.698
    /// </summary>
    private void CrearPolvo()
    {
        Transform origen = DustSpawnPoint != null ? DustSpawnPoint : transform;

        GameObject go = new GameObject("Dust_Main");
        go.transform.SetParent(origen);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        _dustInstance = go.AddComponent<ParticleSystem>();

        // Main module — copiado exacto del prefab
        var main = _dustInstance.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;  // LOCAL como el prefab
        main.startLifetime = new ParticleSystem.MinMaxCurve(DustLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(DustSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(DustSize);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0f);
        main.maxParticles = 3500;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.698f),
            new Color(1f, 1f, 1f, 0.698f)
        );

        // Emission
        var em = _dustInstance.emission;
        em.rateOverTime = DustEmission;

        // Shape — cone, angle 25, radius 2.34, sin rotacion extra
        var sh = _dustInstance.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = DustAngle;
        sh.radius = DustRadius;
        sh.rotation = new Vector3(0f, 180f, 0f); // apunta hacia atras

        // ColorOverLifetime — cafe a blanco con fade suave
        var col = _dustInstance.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.55f, 0.38f, 0.18f), 0f),
                new GradientColorKey(new Color(0.80f, 0.68f, 0.50f), 0.4f),
                new GradientColorKey(new Color(0.95f, 0.92f, 0.85f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,    0.00f),
                new GradientAlphaKey(0.85f, 0.10f),
                new GradientAlphaKey(0.70f, 0.50f),
                new GradientAlphaKey(0f,    1.00f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // SizeOverLifetime — DESACTIVADO como en el prefab
        var sol = _dustInstance.sizeOverLifetime;
        sol.enabled = false;

        // RotationOverLifetime — DESACTIVADO como en el prefab
        var rol = _dustInstance.rotationOverLifetime;
        rol.enabled = false;

        // Renderer
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 10;
        if (DustMaterial != null)
            rend.material = DustMaterial;
        else
            rend.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));

        _dustInstance.Play(true);
    }

    // -------------------------------------------------------------------------
    private int GetClosestWaypointIndex(Transform lane, Vector3 position)
    {
        int closestIndex = 0; float minDistance = Mathf.Infinity;
        for (int i = 0; i < lane.childCount; i++)
        {
            float dist = Vector3.Distance(lane.GetChild(i).position, position);
            if (dist < minDistance) { minDistance = dist; closestIndex = i; }
        }
        return closestIndex;
    }

    private Vector3 GetPositionAtDistance(float d)
    {
        if (d <= 0f) return _pathPoints[0];
        if (d >= _totalPathDistance) return _pathPoints[_pathPoints.Count - 1];
        for (int i = 0; i < _waypointDistances.Length - 1; i++)
        {
            if (d >= _waypointDistances[i] && d <= _waypointDistances[i + 1])
            {
                float segLen = _waypointDistances[i + 1] - _waypointDistances[i];
                float t = segLen > 0f ? (d - _waypointDistances[i]) / segLen : 0f;
                return Vector3.Lerp(_pathPoints[i], _pathPoints[i + 1], t);
            }
        }
        return _pathPoints[_pathPoints.Count - 1];
    }
}