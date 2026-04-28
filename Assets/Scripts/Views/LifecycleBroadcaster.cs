using UnityEngine;
using Zenject;

public class LifecycleBroadcaster : MonoBehaviour
{
    private SignalBus _signalBus;
    
    [Inject]
    public void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    private void OnApplicationQuit()
    {
        _signalBus.Fire(new OnApplicationQuitSignal());
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _signalBus.Fire(new OnApplicationFocusSignal() { Focus = hasFocus });
    }
}