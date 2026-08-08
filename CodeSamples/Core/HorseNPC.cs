using UnityEngine;
using System.Collections; // Necesario para las corrutinas (Parpadeo)

[RequireComponent(typeof(Animation))]
public class HorseNPC : MonoBehaviour
{
    [Header("ARRASTRE LAS ANIMACIONES AQUI (Drag & Drop)")]
    public AnimationClip IdleClip;
    public AnimationClip TrotClip;
    public AnimationClip RunClip;
    public AnimationClip FinishClip;

    [Header("Configuración Básica del Nombre")]
    [Tooltip("Si está activo, el caballo mostrará su nombre en texto 3D arriba de él.")]
    public bool MostrarNombre = true;
    [Tooltip("Qué tan arriba del caballo debe flotar el texto.")]
    public float AlturaNombre = 3.0f;
    [Tooltip("Tamaño de la fuente del texto.")]
    [Range(10, 150)] public int TamanoFuente = 50;
    [Tooltip("Color principal del texto interior.")]
    public Color ColorDelNombre = Color.white;

    [Header("Estilo TV Broadcast (Delineado y Fuente)")]
    [Tooltip("Arrastre aquí un archivo de Fuente (.ttf o .otf) para cambiar el estilo de la letra")]
    public Font FuentePersonalizada;
    [Tooltip("Activa un delineado/brillo alrededor del texto")]
    public bool UsarDelineadoBrillante = true;
    [Tooltip("Color del delineado (Use colores oscuros para contorno o colores vivos para brillo/glow)")]
    public Color ColorDelDelineado = Color.black;
    [Tooltip("Qué tan grueso o expandido es el delineado/brillo")]
    public float GrosorDelineado = 0.05f;

    [Header("Efectos Visuales")]
    [Tooltip("Hace que el nombre parpadee suavemente para llamar la atención")]
    public bool ActivarParpadeo = false;
    [Tooltip("Velocidad del efecto de parpadeo")]
    public float VelocidadParpadeo = 3.0f;

    private Animation _anim;

    // Variables internas para el texto flotante y efectos
    private GameObject _nameTagRoot;
    private TextMesh _nameTextMesh;
    private TextMesh[] _textosDelineado; // Array para guardar las 8 copias del brillo
    private Coroutine _blinkRoutine;
    private Color _colorBaseTexto;

    void Awake()
    {
        _anim = GetComponent<Animation>();
        _anim.playAutomatically = false;

        if (MostrarNombre)
        {
            CrearNombreFlotanteConDelineado();
        }
    }

    void OnEnable()
    {
        // Reactivar parpadeo si el objeto se apaga y se vuelve a prender
        if (MostrarNombre && ActivarParpadeo && _nameTextMesh != null)
        {
            if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
            _blinkRoutine = StartCoroutine(BlinkNameRoutine());
        }
    }

    void OnDisable()
    {
        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
    }

    private void CrearNombreFlotanteConDelineado()
    {
        // 1. Crear el objeto raíz que sostendrá todo
        _nameTagRoot = new GameObject("NameTagRoot_" + gameObject.name);
        _nameTagRoot.transform.SetParent(transform);
        _nameTagRoot.transform.localPosition = new Vector3(0, AlturaNombre, 0);

        // 2. Crear el objeto para el Texto 3D Principal
        GameObject textObj = new GameObject("NameText_Principal");
        textObj.transform.SetParent(_nameTagRoot.transform);
        textObj.transform.localPosition = Vector3.zero;

        _nameTextMesh = textObj.AddComponent<TextMesh>();
        _nameTextMesh.text = gameObject.name;

        if (FuentePersonalizada != null)
        {
            _nameTextMesh.font = FuentePersonalizada;
            textObj.GetComponent<Renderer>().material = FuentePersonalizada.material;
        }

        _nameTextMesh.characterSize = 0.05f;
        _nameTextMesh.fontSize = TamanoFuente;
        _nameTextMesh.anchor = TextAnchor.MiddleCenter;
        _nameTextMesh.alignment = TextAlignment.Center;
        _colorBaseTexto = ColorDelNombre;
        _nameTextMesh.color = _colorBaseTexto;
        textObj.GetComponent<Renderer>().sortingOrder = 11; // Capa superior

        // 3. Crear el Delineado Brillante (8 copias alrededor del texto)
        if (UsarDelineadoBrillante)
        {
            _textosDelineado = new TextMesh[8];

            // Direcciones: Arriba, Abajo, Izquierda, Derecha y las 4 diagonales
            Vector2[] offsets = new Vector2[]
            {
                new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
                new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject outlineObj = new GameObject("NameText_Outline_" + i);
                outlineObj.transform.SetParent(_nameTagRoot.transform);

                // Se desplazan ligeramente en X/Y según el grosor, y se empujan un poco hacia atrás en Z (0.01f)
                outlineObj.transform.localPosition = new Vector3(offsets[i].x * GrosorDelineado, offsets[i].y * GrosorDelineado, 0.01f);

                TextMesh outlineMesh = outlineObj.AddComponent<TextMesh>();
                outlineMesh.text = gameObject.name;

                if (FuentePersonalizada != null)
                {
                    outlineMesh.font = FuentePersonalizada;
                    outlineObj.GetComponent<Renderer>().material = FuentePersonalizada.material;
                }

                outlineMesh.characterSize = 0.05f;
                outlineMesh.fontSize = TamanoFuente;
                outlineMesh.anchor = TextAnchor.MiddleCenter;
                outlineMesh.alignment = TextAlignment.Center;
                outlineMesh.color = ColorDelDelineado;

                outlineObj.GetComponent<Renderer>().sortingOrder = 10; // Debajo del texto principal

                _textosDelineado[i] = outlineMesh;
            }
        }

        // 4. Iniciar parpadeo si está activo
        if (ActivarParpadeo)
        {
            _blinkRoutine = StartCoroutine(BlinkNameRoutine());
        }
    }

    // --- CORRUTINA PARA EL EFECTO DE PARPADEO SUAVE ---
    IEnumerator BlinkNameRoutine()
    {
        while (true)
        {
            // Genera un valor de Alpha que sube y baja suavemente entre 0.3 y 1.0
            float alpha = Mathf.PingPong(Time.time * VelocidadParpadeo, 0.7f) + 0.3f;

            // Aplica el alpha al texto principal
            Color colorActual = _colorBaseTexto;
            colorActual.a = alpha;
            _nameTextMesh.color = colorActual;

            // Aplica el mismo parpadeo al delineado para que el efecto sea parejo
            if (UsarDelineadoBrillante && _textosDelineado != null)
            {
                Color colorOutlineActual = ColorDelDelineado;
                colorOutlineActual.a = alpha; // Mantiene la transparencia sincronizada

                for (int i = 0; i < _textosDelineado.Length; i++)
                {
                    if (_textosDelineado[i] != null)
                    {
                        _textosDelineado[i].color = colorOutlineActual;
                    }
                }
            }

            yield return null; // Esperar al siguiente frame
        }
    }

    void LateUpdate()
    {
        // Obliga a la raíz (que contiene el texto y su delineado) a mirar siempre a la cámara principal
        if (_nameTagRoot != null && Camera.main != null)
        {
            _nameTagRoot.transform.rotation = Camera.main.transform.rotation;
        }
    }

    // --- MÉTODOS DE ANIMACIÓN EXISTENTES (SIN CAMBIOS) ---
    public void PlayIdle()
    {
        if (!_anim || IdleClip == null) return;
        if (_anim[IdleClip.name] == null) _anim.AddClip(IdleClip, IdleClip.name);
        _anim[IdleClip.name].wrapMode = WrapMode.Loop;
        _anim.CrossFade(IdleClip.name, 0.2f);
    }

    public void PlayTrot(float speedMultiplier)
    {
        if (!_anim || TrotClip == null) return;
        if (_anim[TrotClip.name] == null) _anim.AddClip(TrotClip, TrotClip.name);
        _anim[TrotClip.name].speed = speedMultiplier;
        _anim[TrotClip.name].wrapMode = WrapMode.Loop;
        _anim.CrossFade(TrotClip.name, 0.2f);
    }

    public void UpdateTrotSpeed(float speedMultiplier)
    {
        if (_anim && TrotClip != null && _anim[TrotClip.name] != null)
        {
            _anim[TrotClip.name].speed = speedMultiplier;
        }
    }

    public void PlayRun(float speedMultiplier)
    {
        if (!_anim || RunClip == null) return;
        if (_anim[RunClip.name] == null) _anim.AddClip(RunClip, RunClip.name);
        _anim[RunClip.name].speed = speedMultiplier;
        _anim[RunClip.name].wrapMode = WrapMode.Loop;
        _anim.CrossFade(RunClip.name, 0.1f);
    }

    public void UpdateRunSpeed(float speedMultiplier)
    {
        if (_anim && RunClip != null && _anim[RunClip.name] != null)
        {
            _anim[RunClip.name].speed = speedMultiplier;
        }
    }

    public void PlayFinish()
    {
        if (!_anim || FinishClip == null) return;
        if (_anim[FinishClip.name] == null) _anim.AddClip(FinishClip, FinishClip.name);
        _anim.CrossFade(FinishClip.name, 0.5f);
    }
}