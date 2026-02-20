using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using TapHouse.Logging;

#if false
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(IdleManager))]
[RequireComponent(typeof(EatAnimationController))]
[RequireComponent(typeof(GetBall))]
[RequireComponent(typeof(TouchController))]
[RequireComponent(typeof(FaceAction))]
[RequireComponent(typeof(Snack))]
[RequireComponent(typeof(adjustment_offset))]
[RequireComponent(typeof(SleepController))]
[RequireComponent(typeof(MaskLayerManager))]
#endif
[RequireComponent(typeof(AudioAnimationPlayer))]
public class DogController : MonoBehaviour
{
    [SerializeField] public Animator animator;
    [SerializeField] private FirebaseManager _firebaseManager;
    [SerializeField] private SleepController _sleepController;
    [SerializeField] private MaskLayerManager _maskLayerManager;
    [SerializeField] private TurnAndMoveHandler turnAndMoveHandler;
    [SerializeField] private TMP_Text stateText;

    [Header("Visibility Control (Multi-Device)")]
    [Tooltip("犬のメッシュオブジェクト（非表示時にこれだけを無効化）")]
    [SerializeField] private GameObject dogMesh;

    public GetBall getBall;
    private AudioAnimationPlayer _audioAnimationPlayer;

    // LateUpdateで適用する回転（アニメーション上書き対策）
    private Quaternion? _pendingRotation;
    public void SetPendingRotation(Quaternion rotation) => _pendingRotation = rotation;
    public void ClearPendingRotation() => _pendingRotation = null;

    void Awake()
    {
        _audioAnimationPlayer = GetComponent<AudioAnimationPlayer>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (animator == null)
        {
            GameLogger.LogError(LogCategory.Dog,"DogController: Animator component not found!");
        }
    }

    void LateUpdate()
    {
        if (_pendingRotation.HasValue)
        {
            transform.rotation = _pendingRotation.Value;
        }
    }

    public float GetCurrentAnimationLength()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length;
    }
    public int GetSnackType()
    {
        return animator.GetInteger("snackType");
    }
    public void UpdateSnackType(int value)
    {
        animator.SetInteger("snackType", value);
    }
    public void UpdateLieOnBackState(int value)
    {
        animator.SetInteger("LieOnBackType", value);
    }
    public int GetLiOenBackState()
    {
        return animator.GetInteger("LieOnBackType");
    }
    public void UpdateTransitionState(int Update_transition_state = 0)
    {
        animator.SetInteger("TransitionNo", Update_transition_state);
    }
    public void TriggerSleepOneWakeAction()
    {
        animator.GetInteger("SleepOneWakeAction");
    }
    public int GetTransitionNo()
    {
        return animator.GetInteger("TransitionNo");
    }
    public void isEating(bool isEatingState)
    {
        animator.SetBool("EatingBool", isEatingState);
    }
    // public IEnumerator isEating(bool isEatingState)
    // {
    //     animator.SetBool("EatingBool", isEatingState);
    //     if (isEatingState)
    //     {
    //         yield return new WaitForSeconds(1f);
    //     }
    //     yield break;
    // }
    public void SetLieOnBackTrue()
    {
        animator.SetBool("LieOnBackBool", true);
        GameLogger.Log(LogCategory.Dog,"LieOnBackBool set to: true");
    }

    public void SetLieOnBackFalse()
    {
        animator.SetBool("LieOnBackBool", false);
        GameLogger.Log(LogCategory.Dog,"LieOnBackBool set to: false");
    }
    public void SetBite(bool value)
    {
        animator.SetBool("BiteBool", value);
    }
    public void SetSwing(bool value)
    {
        animator.SetBool("SwingBool", value);
    }

    public bool GetLieOnBackBool()
    {
        return animator.GetBool("LieOnBackBool");
    }
    //なでなでアニメーション
    public void Petting(bool isPetting)
    {
        animator.SetBool("isPetting", isPetting);
    }
    public void Sleeping(bool isSleeping)
    {
        animator.SetBool("SleepBool", isSleeping);
    }
    public bool GetSleeping()
    {
        return animator.GetBool("SleepBool");
    }
    //走っているときのアニメーション
    public void StartMoving(float move_x, float move_y)
    {
        animator.SetFloat("move_x", move_x);
        animator.SetFloat("move_y", move_y);
    }
    public void StartWalking(float walk_x, float walk_y)
    {
        animator.SetFloat("walk_x", walk_x);
        animator.SetFloat("walk_y", walk_y);
    }
    public void SetPatFloat(float patFloat)
    {
        animator.SetFloat("PatFloat", patFloat);
    }
    public void ResetDogPosition()
    {
        if (GetSleeping())
        {
            if (_sleepController == null)
            {
                GameLogger.LogError(LogCategory.Dog,"sleepController is not assigned!");
            }
            else
            {
                _sleepController.WakeUp();
                GameLogger.Log(LogCategory.Dog,"Exiting sleep state007.");
            }
        }
        else if (GlobalVariables.CurrentState == PetState.ball)
        {
            if (getBall == null)
            {
                GameLogger.LogError(LogCategory.Dog,"getBall is not assigned!");
            }
            else
            {
                getBall.EndPlay();
            }
        }
        else if (GlobalVariables.CurrentState == PetState.feeding)
        {
            isEating(false);
        }

        if (_firebaseManager == null)
        {
            GameLogger.LogError(LogCategory.Dog,"firebaseManager is not assigned!");
        }
        else
        {
            _firebaseManager.UpdatePetState("idle");
        }
        GlobalVariables.CurrentState = PetState.idle;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void ActionRPaw()
    {
        turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
        animator.SetTrigger("PawRStart");
    }
    public void ActionLPaw()
    {
        turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
        animator.SetTrigger("PawLStart");
    }
    public void ActionDance()
    {
        turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
        animator.SetTrigger("DanceStart");
    }
    public void ActionDang()
    {
        turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
        animator.SetTrigger("BangStart");
    }
    public void ActionStand()
    {
        turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
        animator.SetTrigger("StandStart");
    }
    public void ActionHighDance()
    {
        turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
        animator.SetTrigger("HighDanceStart");
    }
    public void ActionLieDown()
    {
        ActionBool(false);
        UpdateTransitionState(1);
    }
    public void ActionBark()
    {
        turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
        animator.SetTrigger("BarkStart");
    }
    public void ActionBool(bool value = false)
    {
        if (GetTransitionNo() == 3 || GlobalVariables.CurrentHungerState == HungerState.Hungry || GetSleepBool()) return;

        if (Vector3.Distance(transform.position, Vector3.zero) > 0.5f && value)
        {
            StartCoroutine(WaitForMoveToComplete(value));
        }
        else
        {
            _maskLayerManager.SetLayerWeight("face", value ? 1 : 0, 15f);
            animator.SetBool("ActionBool", value);
        }
    }

    private IEnumerator WaitForMoveToComplete(bool value)
    {
        turnAndMoveHandler.StartTurnAndMove(Vector3.zero, 1.5f);

        // 移動が完了するまで待機 (ここでは1.5秒間待つ)
        yield return new WaitForSeconds(1.5f);
        _maskLayerManager.SetLayerWeight("face", value ? 1 : 0, 15f);
        animator.SetBool("ActionBool", value);
    }
    public void WalkBool(bool value)
    {
        animator.SetBool("WalkBool", value);
    }
    public bool GetIsAction()
    {
        return animator.GetBool("ActionBool");
    }
    public void LayerBarkTrigger()
    {
        animator.SetTrigger("LayersBark");
    }
    public bool GetSleepBool()
    {
        return animator.GetBool("SleepBool");
    }

    #region Visibility Control (Multi-Device)
    /// <summary>
    /// 犬の表示/非表示を切り替え（マルチデバイス機能用）
    /// メッシュとAnimatorのみを制御し、親GameObjectは常にアクティブに保つ
    /// これによりtransform（position, rotation, scale）の状態が維持される
    /// </summary>
    /// <param name="visible">表示する場合true</param>
    public void SetVisible(bool visible)
    {
        // メッシュの表示/非表示
        if (dogMesh != null)
        {
            dogMesh.SetActive(visible);
        }
        else
        {
            // dogMeshが未設定の場合はフォールバック（従来動作）
            GameLogger.LogWarning(LogCategory.Dog, "dogMesh is not assigned, falling back to gameObject.SetActive");
            gameObject.SetActive(visible);
            return;
        }

        // Animatorの有効/無効（処理負荷削減）
        if (animator != null)
        {
            animator.enabled = visible;
        }

        GameLogger.Log(LogCategory.Dog, $"Dog visibility set to: {visible} (mesh + animator control)");
    }

    /// <summary>
    /// 犬が表示されているか
    /// </summary>
    public bool IsVisible => dogMesh != null ? dogMesh.activeSelf : gameObject.activeSelf;
    #endregion

}
