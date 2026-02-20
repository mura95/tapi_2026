using UnityEngine;
using UnityEngine.Events;

public class FoodItemButtonEventListener : MonoBehaviour
{
    public FoodItemButtonEvent itemButtonEvent;

    public UnityAction<int> response;

    private void OnEnable()
    {
        itemButtonEvent.RegisterListener(this);
    }

    private void OnDisable()
    {
        itemButtonEvent.UnregisterListener(this);
    }

    public void OnEventRaised(int foodId)
    {
        response?.Invoke(foodId);
    }
}