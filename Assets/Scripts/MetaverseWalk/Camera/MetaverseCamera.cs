using UnityEngine;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.Camera
{
    /// <summary>
    /// メタバースシーンのアイソメトリックカメラ制御
    /// 犬とプレイヤーの中間点を追従
    /// </summary>
    public class MetaverseCamera : MonoBehaviour
    {
        [Header("追従ターゲット")]
        [SerializeField] private Transform dog;
        [SerializeField] private Transform player;

        [Header("カメラ設定")]
        [SerializeField] private float distance = MetaverseConstants.DEFAULT_CAMERA_DISTANCE;
        [SerializeField] private float height = MetaverseConstants.DEFAULT_CAMERA_HEIGHT;
        [SerializeField] private float pitchAngle = MetaverseConstants.DEFAULT_CAMERA_ANGLE;
        [SerializeField] private float yawAngle = MetaverseConstants.DEFAULT_CAMERA_ROTATION;

        [Header("追従設定")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool followDogOnly = false;

        [Header("境界設定")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector3 boundsMin = new Vector3(-50, 0, -50);
        [SerializeField] private Vector3 boundsMax = new Vector3(50, 50, 50);

        private Vector3 offset;
        private UnityEngine.Camera cam;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
        }

        private void Start()
        {
            CalculateOffset();
            SetupCamera();
        }

        private void CalculateOffset()
        {
            // アイソメトリック視点のオフセット計算
            float radYaw = yawAngle * Mathf.Deg2Rad;
            float radPitch = pitchAngle * Mathf.Deg2Rad;

            offset = new Vector3(
                distance * Mathf.Sin(radYaw) * Mathf.Cos(radPitch),
                height,
                -distance * Mathf.Cos(radYaw) * Mathf.Cos(radPitch)
            );
        }

        private void SetupCamera()
        {
            if (cam != null)
            {
                // 遠近感を抑えたFOV設定
                cam.fieldOfView = 35f;
            }
        }

        private void LateUpdate()
        {
            if (dog == null) return;

            Vector3 targetPoint = GetTargetPoint();
            Vector3 desiredPosition = targetPoint + offset;

            // 境界チェック
            if (useBounds)
            {
                desiredPosition = ClampToBounds(desiredPosition);
            }

            // スムーズに追従
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // ターゲットを見る
            transform.LookAt(targetPoint);
        }

        private Vector3 GetTargetPoint()
        {
            if (followDogOnly || player == null)
            {
                return dog.position;
            }

            // 犬とプレイヤーの中間点
            return (dog.position + player.position) / 2f;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, boundsMin.x, boundsMax.x),
                Mathf.Clamp(position.y, boundsMin.y, boundsMax.y),
                Mathf.Clamp(position.z, boundsMin.z, boundsMax.z)
            );
        }

        /// <summary>
        /// 追従ターゲットを設定
        /// </summary>
        public void SetTargets(Transform dogTransform, Transform playerTransform)
        {
            dog = dogTransform;
            player = playerTransform;
        }

        /// <summary>
        /// カメラを即座にターゲット位置に移動
        /// </summary>
        public void SnapToTarget()
        {
            if (dog == null) return;

            Vector3 targetPoint = GetTargetPoint();
            transform.position = targetPoint + offset;
            transform.LookAt(targetPoint);
        }

        /// <summary>
        /// カメラパラメータを更新
        /// </summary>
        public void UpdateCameraParameters(float newDistance, float newHeight, float newPitch, float newYaw)
        {
            distance = newDistance;
            height = newHeight;
            pitchAngle = newPitch;
            yawAngle = newYaw;
            CalculateOffset();
        }
    }
}
