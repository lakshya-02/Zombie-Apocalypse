using UnityEngine;

/// <summary>
/// Visual gizmo for spawn points. Shows a red sphere and label in Scene view.
/// Attach to each spawn point empty GameObject (auto-added by scene setup).
/// </summary>
public class SpawnPointGizmo : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Gizmo Settings")]
    public Color gizmoColor = new Color(1f, 0.3f, 0.3f, 0.6f);
    public float gizmoRadius = 1f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);

        // Draw line to center (player position)
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
        Gizmos.DrawLine(transform.position, Vector3.zero);

        // Label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, gameObject.name);
    }
#endif
}
