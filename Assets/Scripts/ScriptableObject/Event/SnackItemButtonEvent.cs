using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SnackItemButtonEvent", menuName = "PetData/UI/SnackItemButtonEvent")]
public class SnackItemButtonEvent : ScriptableObject
{
    private List<SnackItemButtonEventListener> listeners = new List<SnackItemButtonEventListener>();
        
    public void Raise(int foodId)
    {
        for (var i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i].OnEventRaised(foodId);
        }
    }

    public void RegisterListener(SnackItemButtonEventListener listener)
    {
        listeners.Add(listener);
    }
        
    public void UnregisterListener(SnackItemButtonEventListener listener)
    {
        listeners.Remove(listener);
    }

}
