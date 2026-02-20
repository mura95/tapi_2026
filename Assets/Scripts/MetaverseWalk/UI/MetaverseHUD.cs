using UnityEngine;
using TMPro;
using TapHouse.MetaverseWalk.Network;

namespace TapHouse.MetaverseWalk.UI
{
    /// <summary>
    /// メタバースシーンのHUD（ヘッドアップディスプレイ）
    /// 参加人数、接続状態などを表示
    /// </summary>
    public class MetaverseHUD : MonoBehaviour
    {
        [Header("参加人数表示")]
        [SerializeField] private GameObject playerCountPanel;
        [SerializeField] private TMP_Text playerCountText;

        [Header("接続状態表示")]
        [SerializeField] private GameObject connectionStatusPanel;
        [SerializeField] private TMP_Text connectionStatusText;

        private int currentPlayerCount = 1;
        private bool isConnected = false;

        private void Start()
        {
            // Photonイベントを購読
            if (PhotonNetworkManager.Instance != null)
            {
                PhotonNetworkManager.Instance.OnPlayerCountChanged += UpdatePlayerCount;
                PhotonNetworkManager.Instance.OnConnectionStatusChanged += UpdateConnectionStatus;
                SetPlayerCountVisible(true);
                SetConnectionStatusVisible(true);
                return;
            }

            // シングルプレイヤー時は参加人数非表示
            SetPlayerCountVisible(false);
            SetConnectionStatusVisible(false);
        }

        /// <summary>
        /// 参加人数を更新
        /// </summary>
        public void UpdatePlayerCount(int count)
        {
            currentPlayerCount = count;

            if (playerCountText != null)
            {
                playerCountText.text = $"{count}人";
            }
        }

        /// <summary>
        /// 参加人数表示の表示/非表示
        /// </summary>
        public void SetPlayerCountVisible(bool visible)
        {
            if (playerCountPanel != null)
            {
                playerCountPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// 接続状態を更新
        /// </summary>
        public void UpdateConnectionStatus(bool connected, string message = "")
        {
            isConnected = connected;

            if (connectionStatusText != null)
            {
                if (string.IsNullOrEmpty(message))
                {
                    connectionStatusText.text = connected ? "接続中" : "切断";
                }
                else
                {
                    connectionStatusText.text = message;
                }
            }
        }

        /// <summary>
        /// 接続状態表示の表示/非表示
        /// </summary>
        public void SetConnectionStatusVisible(bool visible)
        {
            if (connectionStatusPanel != null)
            {
                connectionStatusPanel.SetActive(visible);
            }
        }

        private void OnDestroy()
        {
            if (PhotonNetworkManager.Instance != null)
            {
                PhotonNetworkManager.Instance.OnPlayerCountChanged -= UpdatePlayerCount;
                PhotonNetworkManager.Instance.OnConnectionStatusChanged -= UpdateConnectionStatus;
            }
        }

    }
}
