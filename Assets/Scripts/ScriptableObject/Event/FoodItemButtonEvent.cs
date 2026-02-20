using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FoodItemButtonEvent", menuName = "PetData/UI/FoodItemButtonEvent")]
public class FoodItemButtonEvent : ScriptableObject
{
    private List<FoodItemButtonEventListener> listeners = new List<FoodItemButtonEventListener>();
        
    public void Raise(int foodId)
    {
        for (var i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i].OnEventRaised(foodId);
        }
    }

    public void RegisterListener(FoodItemButtonEventListener listener)
    {
        listeners.Add(listener);
    }
        
    public void UnregisterListener(FoodItemButtonEventListener listener)
    {
        listeners.Remove(listener);
    }

}
