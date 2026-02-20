using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HungerStatusChangedEvent", menuName = "PetData/Hunger/HungerStatusChangedEvent")]
public class HungerStatusChangedEvent : ScriptableObject
{
    private List<HungerStatusChangedEventListener> listeners = new List<HungerStatusChangedEventListener>();
        
    public void Raise(int status)
    {
        for (var i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i].OnEventRaised(status);
        }
    }

    public void RegisterListener(HungerStatusChangedEventListener listener)
    {
        listeners.Add(listener);
    }
        
    public void UnregisterListener(HungerStatusChangedEventListener listener)
    {
        listeners.Remove(listener);
    }

}
