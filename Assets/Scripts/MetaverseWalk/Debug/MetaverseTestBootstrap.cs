#if UNITY_EDITOR
using UnityEngine;
using TapHouse.MetaverseWalk.Network;

namespace TapHouse.MetaverseWalk.DebugTools
{
    /// <summary>
    /// Metaverse.unityを直接Playした時に必要な初期化を自動実行するコンポーネント。
    /// SceneTransitionManager経由でない場合のフォールバック初期化を行う。
    /// ParrelSyncクローン検出でプレイヤー名を自動区別する。
    /// Editor専用（リリースビルドには含まれない）。
    /// </summary>
    public class MetaverseTestBootstrap : MonoBehaviour
    {
        [Header("デバッグ設定")]
        [SerializeField] private string hostPlayerName = "Host_Player";
        [SerializeField] private string clonePlayerName = "Clone_Player";

        private bool isClone;

        private void Awake()
        {
            // PetStateがwalkでない場合は直接Play起動と判断して設定
            if (GlobalVariables.CurrentState != PetState.walk)
            {
                GlobalVariables.CurrentState = PetState.walk;
                UnityEngine.Debug.Log("[MetaverseTestBootstrap] Set PetState to walk (direct play mode)");
            }

            // ParrelSyncクローン判定と表示名設定
            isClone = ParrelSync.ClonesManager.IsClone();

            if (isClone)
            {
                PlayerPrefs.SetString(PrefsKeys.DisplayName, clonePlayerName);
                UnityEngine.Debug.Log($"[MetaverseTestBootstrap] Clone detected - DisplayName set to '{clonePlayerName}'");
            }
            else
            {
                // オリジナルEditor: 未設定時のみホスト名を設定
                if (string.IsNullOrEmpty(PlayerPrefs.GetString(PrefsKeys.DisplayName, "")))
                {
                    PlayerPrefs.SetString(PrefsKeys.DisplayName, hostPlayerName);
                }
                UnityEngine.Debug.Log($"[MetaverseTestBootstrap] Original editor - DisplayName: '{PlayerPrefs.GetString(PrefsKeys.DisplayName)}'");
            }
        }

        private void Start()
        {
            // Photon接続状態のログ出力をSubscribe
            var networkManager = PhotonNetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
                networkManager.OnPlayerCountChanged += OnPlayerCountChanged;
            }

            UnityEngine.Debug.Log($"[MetaverseTestBootstrap] Initialized - IsClone: {isClone}, DisplayName: {PlayerPrefs.GetString(PrefsKeys.DisplayName, "N/A")}");
        }

        private void OnConnectionStatusChanged(bool connected, string message)
        {
            UnityEngine.Debug.Log($"[MetaverseTestBootstrap] Connection: {(connected ? "Connected" : "Disconnected")} - {message}");
        }

        private void OnPlayerCountChanged(int count)
        {
            UnityEngine.Debug.Log($"[MetaverseTestBootstrap] Player count: {count}");
        }

        private void OnDestroy()
        {
            var networkManager = PhotonNetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.OnConnectionStatusChanged -= OnConnectionStatusChanged;
                networkManager.OnPlayerCountChanged -= OnPlayerCountChanged;
            }
        }
    }
}
#endif
