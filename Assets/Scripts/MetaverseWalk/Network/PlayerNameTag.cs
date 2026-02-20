using UnityEngine;
using TMPro;

namespace TapHouse.MetaverseWalk.Network
{
    /// <summary>
    /// 他プレイヤーの頭上に名前を表示するビルボードUI
    /// World Space Canvasを使用し、常にカメラ方向を向く
    /// </summary>
    public class PlayerNameTag : MonoBehaviour
    {
        [Header("UI参照")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text nameText;

        [Header("表示設定")]
        [SerializeField] private float maxDisplayDistance = 15f;

        private UnityEngine.Camera mainCamera;
        private bool isLocalPlayer;

        private void Start()
        {
            mainCamera = UnityEngine.Camera.main;
        }

        /// <summary>
        /// 初期設定
        /// </summary>
        public void Initialize(bool isLocal)
        {
            isLocalPlayer = isLocal;

            // ローカルプレイヤーの名前タグは非表示
            if (isLocalPlayer && canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 表示名を更新
        /// </summary>
        public void SetName(string playerName)
        {
            if (nameText != null)
            {
                nameText.text = playerName;
            }
        }

        private void LateUpdate()
        {
            if (isLocalPlayer || mainCamera == null || canvas == null) return;

            // ビルボード処理（カメラの方を向く）
            canvas.transform.LookAt(
                canvas.transform.position + mainCamera.transform.forward
            );

            // 距離ベースの表示切り替え
            float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
            bool shouldShow = distance <= maxDisplayDistance;

            if (canvas.gameObject.activeSelf != shouldShow)
            {
                canvas.gameObject.SetActive(shouldShow);
            }
        }
    }
}
