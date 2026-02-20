using UnityEngine;
using UnityEngine.Events;

public class PlayItemButtonEventListener : MonoBehaviour
{
    public PlayItemButtonEvent itemButtonEvent;
    [SerializeField] public PlayManager _playManager;
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
        _playManager?.StartToyAction(Id);
        response?.Invoke(Id);
    }
}