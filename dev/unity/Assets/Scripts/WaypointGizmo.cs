// WaypointGizmo.cs — attach to each WP_X so you can SEE them in editor
using UnityEngine;

public class WaypointGizmo : MonoBehaviour
{
    [SerializeField] Color gizmoColor = Color.yellow;
    [SerializeField] float radius = 0.2f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, radius);

        // Draw label
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, gameObject.name);
#endif
    }
}