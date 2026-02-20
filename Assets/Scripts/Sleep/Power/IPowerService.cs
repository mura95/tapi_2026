public interface IPowerService
{
    void ClearKeepScreenOn(); // スリープ入り直前に呼ぶ
    void TurnScreenOn();      // 起床時に呼ぶ（点灯＆ロック越し表示）
    void Release();           // OnDestroy等でWakeLock解放
}
