using UnityEngine;

/// <summary>
/// 特定のポイントに向かって投げる角度計算（最適化版）
/// </summary>
public class AngleSetForPoint : MonoBehaviour, I_ToyThrowAngle
{
    [SerializeField] private Vector3 targetPoint = Vector3.zero;
    [SerializeField] private float radius = 2f; // ランダムなばらつき範囲

    public float ThrowAngle(GameObject toy)
    {
        // ランダムなオフセットを追加してターゲット位置を決定
        Vector3 randomOffset = new Vector3(
            Random.Range(-radius, radius),
            0,
            Random.Range(-radius, radius)
        );
        Vector3 target = targetPoint + randomOffset;

        // おもちゃからターゲットへの水平方向ベクトル
        Vector3 toTarget = target - toy.transform.position;
        toTarget.y = 0f;

        // ターゲットがほぼ同じ位置の場合
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        // 現在のforward方向とターゲット方向の角度差を計算
        float angle = Vector3.SignedAngle(toy.transform.forward, toTarget.normalized, Vector3.up);

        return angle;
    }
}