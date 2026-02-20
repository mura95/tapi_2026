using UnityEngine;

/// <summary>
/// おもちゃ投擲角度計算のインターフェース
/// </summary>
public interface I_ToyThrowAngle
{
    /// <summary>
    /// おもちゃを投げる角度を計算
    /// </summary>
    /// <param name="toy">投げるおもちゃ</param>
    /// <returns>投擲角度（度数）</returns>
    public float ThrowAngle(GameObject toy);
}