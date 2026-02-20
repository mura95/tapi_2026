using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TailLayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [HideInInspector] private Vector3 targetPosition = Vector3.zero;
    [SerializeField] private MaskLayerManager _maskLayerManager;

    public float maxDistance = 5f;
    public float exponent = 2f;
    public float positionThreshold = 0.01f;

    private Vector3 previousPosition;
    private string tailLayerName = "tail";

    void Start()
    {
        previousPosition = transform.position;
    }

    void Update()
    {
        if (GlobalVariables.CurrentState == PetState.sleeping || GlobalVariables.CurrentState == PetState.napping) _maskLayerManager.SetLayerWeight(tailLayerName, 0);
        if (GlobalVariables.CurrentState != PetState.idle) return;

        // 現在の位置と前回の位置の差を計算
        float positionDelta = Vector3.Distance(transform.position, previousPosition);

        // 位置の変化が閾値を超えた場合のみウェイトを再計算
        if (positionDelta > positionThreshold)
        {
            // キャラクターとターゲットの距離を計算
            float distance = Vector3.Distance(transform.position, targetPosition);

            // 距離に応じてウェイトを計算（指数関数的に減衰）
            float weight = Mathf.Exp(-exponent * distance / maxDistance);

            // ウェイトを0から1の範囲にクランプ
            weight = Mathf.Clamp01(weight);

            _maskLayerManager.SetLayerWeight(tailLayerName, weight);

            // 現在の位置を前回の位置として保存
            previousPosition = transform.position;
        }
    }


}
