public sealed class NoopPowerService : IPowerService
{
    public void ClearKeepScreenOn() { }
    public void TurnScreenOn() { }
    public void Release() { }
}
