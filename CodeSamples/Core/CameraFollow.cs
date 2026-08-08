using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;

    [Header("Ajustes de TV")]
    public Vector3 Offset = new Vector3(5f, 3f, -5f);
    public float SmoothTime = 0.2f;

    private Vector3 _velocity = Vector3.zero;

    public void SetTarget(Transform newTarget)
    {
        Target = newTarget;
    }

    void LateUpdate()
    {
        if (Target == null) return;

        Vector3 targetPosition = Target.position + Offset;

        // Movimiento suave amortiguado
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, SmoothTime);

        // Mirar un poco arriba del caballo
        transform.LookAt(Target.position + Vector3.up * 1.5f);
    }
}