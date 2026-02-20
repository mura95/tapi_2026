using UnityEngine;
using UnityEngine.Events;

public class HungerStatusChangedEventListener : MonoBehaviour
{
    public HungerStatusChangedEvent hungerStatusChangedEvent;

    public UnityAction<int> response;

    private void OnEnable()
    {
        hungerStatusChangedEvent.RegisterListener(this);
    }

    private void OnDisable()
    {
        hungerStatusChangedEvent.UnregisterListener(this);
    }

    public void OnEventRaised(int foodId)
    {
        response?.Invoke(foodId);
    }
}