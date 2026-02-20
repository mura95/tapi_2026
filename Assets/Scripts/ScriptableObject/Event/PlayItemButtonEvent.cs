using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayItemButtonEvent", menuName = "PetData/UI/PlayItemButtonEvent")]
public class PlayItemButtonEvent : ScriptableObject
{
    private List<PlayItemButtonEventListener> listeners = new List<PlayItemButtonEventListener>();
        
    public void Raise(int foodId)
    {
        for (var i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i].OnEventRaised(foodId);
        }
    }

    public void RegisterListener(PlayItemButtonEventListener listener)
    {
        listeners.Add(listener);
    }
        
    public void UnregisterListener(PlayItemButtonEventListener listener)
    {
        listeners.Remove(listener);
    }

}
