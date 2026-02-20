using UnityEngine;

namespace TapHouse.UI
{
    /// <summary>
    /// ローディングインジケーター用の回転スクリプト
    /// </summary>
    public class LoadingSpinner : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 200f;

        void Update()
        {
            transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
        }
    }
}
