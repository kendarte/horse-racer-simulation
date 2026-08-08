using UnityEngine;

[ExecuteInEditMode] // <--- ESTO PERMITE QUE FUNCIONE SIN DARLE PLAY
public class TrackGenerator : MonoBehaviour
{
    public enum TrackShape { Circle, Oval }

    [Header("1. FORMA DE LA PISTA")]
    public TrackShape Shape = TrackShape.Oval;

    [Header("2. DIMENSIONES")]
    public float StraightLength = 80f;
    public float CurveRadius = 30f;
    public int Resolution = 40;
    public float LaneWidth = 3.0f;

    [Header("3. CONTROL EN TIEMPO REAL")]
    [Tooltip("Si está activo, los Waypoints se mueven solos al cambiar los números")]
    public bool AutoActualizar = true;

    // --- VARIABLES INTERNAS PARA DETECTAR CAMBIOS ---
    private TrackShape _lastShape;
    private float _lastStraight;
    private float _lastRadius;
    private int _lastRes;
    private float _lastWidth;

    void Update()
    {
        // Si AutoActualizar está activo y estamos editando (no jugando)
        if (AutoActualizar && !Application.isPlaying)
        {
            if (ValoresCambiaron())
            {
                BakeTrack();
                GuardarValores();
            }
        }
    }

    bool ValoresCambiaron()
    {
        return Shape != _lastShape ||
               StraightLength != _lastStraight ||
               CurveRadius != _lastRadius ||
               Resolution != _lastRes ||
               LaneWidth != _lastWidth;
    }

    void GuardarValores()
    {
        _lastShape = Shape;
        _lastStraight = StraightLength;
        _lastRadius = CurveRadius;
        _lastRes = Resolution;
        _lastWidth = LaneWidth;
    }

    // --- BOTÓN MANUAL (Por si desactiva el automático) ---
    [ContextMenu("GENERAR LAS 2 LANES AHORA")]
    public void BakeTrack()
    {
        // 1. Borrar Waypoints viejos de forma segura en el Editor
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // 2. Crear las 2 lanes físicas
        CreateLane(1, CurveRadius);
        CreateLane(2, CurveRadius + LaneWidth);
    }

    // --- CREACIÓN FÍSICA EXACTA ---
    private void CreateLane(int laneNumber, float radius)
    {
        GameObject laneObj = new GameObject($"LANE_0{laneNumber}");
        laneObj.transform.parent = transform;
        laneObj.transform.localPosition = Vector3.zero;

        int wpIndex = 0;

        if (Shape == TrackShape.Circle)
        {
            for (int i = 0; i < Resolution; i++)
            {
                float rad = (360f * i / Resolution) * Mathf.Deg2Rad;
                float x = Mathf.Cos(rad) * radius;
                float z = Mathf.Sin(rad) * radius;
                CreateWaypoint(laneObj.transform, new Vector3(x, 0, z), wpIndex++);
            }
        }
        else if (Shape == TrackShape.Oval)
        {
            float halfStraight = StraightLength / 2f;
            int straightRes = 5;
            int halfRes = Resolution / 2;

            // Arriba
            for (int i = 0; i <= straightRes; i++)
            {
                float x = Mathf.Lerp(halfStraight, -halfStraight, (float)i / straightRes);
                CreateWaypoint(laneObj.transform, new Vector3(x, 0, radius), wpIndex++);
            }
            // Izquierda
            for (int i = 1; i < halfRes; i++)
            {
                float rad = (90f + (180f * i / halfRes)) * Mathf.Deg2Rad;
                float x = -halfStraight + (Mathf.Cos(rad) * radius);
                float z = Mathf.Sin(rad) * radius;
                CreateWaypoint(laneObj.transform, new Vector3(x, 0, z), wpIndex++);
            }
            // Abajo
            for (int i = 0; i <= straightRes; i++)
            {
                float x = Mathf.Lerp(-halfStraight, halfStraight, (float)i / straightRes);
                CreateWaypoint(laneObj.transform, new Vector3(x, 0, -radius), wpIndex++);
            }
            // Derecha
            for (int i = 1; i < halfRes; i++)
            {
                float rad = (270f + (180f * i / halfRes)) * Mathf.Deg2Rad;
                float x = halfStraight + (Mathf.Cos(rad) * radius);
                float z = Mathf.Sin(rad) * radius;
                CreateWaypoint(laneObj.transform, new Vector3(x, 0, z), wpIndex++);
            }
        }
    }

    private void CreateWaypoint(Transform parent, Vector3 localPos, int index)
    {
        GameObject wp = new GameObject($"WP_{index:000}");
        wp.transform.parent = parent;
        wp.transform.localPosition = localPos;
    }

    // --- DIBUJAR LÍNEAS SOBRE LOS OBJETOS REALES ---
    void OnDrawGizmos()
    {
        foreach (Transform lane in transform)
        {
            Gizmos.color = lane.name.Contains("1") ? Color.green : Color.red;

            for (int i = 0; i < lane.childCount; i++)
            {
                Transform currentWP = lane.GetChild(i);
                Transform nextWP = lane.GetChild((i + 1) % lane.childCount);

                Gizmos.DrawCube(currentWP.position, Vector3.one * 0.5f);
                Gizmos.DrawLine(currentWP.position, nextWP.position);
            }
        }
    }
}