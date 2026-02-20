using UnityEngine;

/// <summary>
/// オブジェクトの回転を別のオブジェクトに合わせる
/// おもちゃが犬の向きに合わせて回転するために使用
/// </summary>
public class MatchRotation : MonoBehaviour
{
    /// <summary>
    /// 指定したオブジェクトの回転に合わせる
    /// </summary>
    /// <param name="targetObject">合わせる対象のオブジェクト</param>
    public void Match(GameObject targetObject)
    {
        if (targetObject != null)
        {
            this.transform.rotation = targetObject.transform.rotation;
        }
    }
}