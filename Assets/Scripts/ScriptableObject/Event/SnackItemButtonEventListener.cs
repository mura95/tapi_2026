using UnityEngine;
using UnityEngine.Events;

public class SnackItemButtonEventListener : MonoBehaviour
{
    public SnackItemButtonEvent itemButtonEvent;
    public SnackManager snackManager;

    public UnityAction<int> response;

    private void OnEnable()
    {
        itemButtonEvent.RegisterListener(this);
    }

    private void OnDisable()
    {
        itemButtonEvent.UnregisterListener(this);
    }

    public void OnEventRaised(int Id)
    {
        response?.Invoke(Id);
        snackManager?.StartSnackAction(Id);
    }
}