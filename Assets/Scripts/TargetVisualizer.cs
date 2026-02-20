using UnityEngine;

[ExecuteInEditMode]
public class TargetVisualizer : MonoBehaviour
{
    private Vector3 _targetPosition;
    private bool _showTargetGizmo = false;
    public Color gizmoColor = Color.red;

    public void Show(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
        _showTargetGizmo = true;
    }
    
    public void Hide()
    {
        _showTargetGizmo = false;
    }
    
    
    private void OnDrawGizmos()
    {
        if (_showTargetGizmo == false)
        {
            return;
        }
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(_targetPosition, 0.2f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, _targetPosition);
    }
}