using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;
public class SnackManager : MonoBehaviour
{
    [SerializeField] private GameObject[] snacks;
    [SerializeField] private Transform dogMouthPosition;
    [SerializeField] private DogController _dogController;
    [SerializeField] private Animator snackAnimator;
    [SerializeField] private TurnAndMoveHandler _turnAndMoveHandler;
    [SerializeField] private MainUICloseButton _mainUICloseButton;
    [SerializeField] private MainUIButtons _mainUiButtons;
    [SerializeField] private HungerManager _hungerManager;
    [SerializeField] private DogStateController _dogStateController;
    private int currentSnackType = 0;
    private Coroutine floatingCoroutine;
    private Coroutine snackCoroutine;

    void Start()
    {
        foreach (GameObject snack in snacks)
        {
            snack.SetActive(false);
        }
    }

    /// <summary>
    /// Snack の動作を開始します。
    /// </summary>
    /// <param name="snackType">表示する Snack のタイプ</param>
    public void StartSnackAction(int snackType)
    {
        // snackType の有効性を確認
        if (snackType <= 0 || snackType > snacks.Length)
        {
            GameLogger.LogError(LogCategory.Dog,"Invalid snackType specified.");
            return;
        }

        _dogController.ActionBool(false);

        // 既存の Snack を非アクティブ化
        DeactivateCurrentSnack();

        currentSnackType = snackType;
        GameObject snack = snacks[currentSnackType - 1];

        snack.SetActive(true);
        PlaceSnackInFrontOfCamera(snack, currentSnackType);

        if (floatingCoroutine != null)
        {
            StopCoroutine(floatingCoroutine);
        }
        floatingCoroutine = StartCoroutine(FloatSnack(snack, currentSnackType));

        if (snackCoroutine != null)
        {
            StopCoroutine(snackCoroutine);
        }

        GlobalVariables.CurrentState = PetState.snack;
        _dogController.UpdateTransitionState(0);
        switch (currentSnackType)
        {
            case 1:
                snackCoroutine = StartCoroutine(BornSnackAnimation(currentSnackType, snack, 60.0f));
                break;
            case 2:
                snackCoroutine = StartCoroutine(ChuRuChuRuSnackAnimation(currentSnackType, 10.0f));
                break;
            default:
                GameLogger.LogWarning(LogCategory.Dog,$"No specific action defined for Snack Type {currentSnackType}.");
                break;
        }
    }

    // 現在の Snack を非アクティブ化
    private void DeactivateCurrentSnack()
    {
        foreach (GameObject snack in snacks)
        {
            snack.SetActive(false);
        }
    }

    // Snack をカメラの前に配置
    private void PlaceSnackInFrontOfCamera(GameObject snack, int snackType)
    {
        Vector3 position;
        Quaternion rotation;

        // snackType に応じて位置と回転を設定
        switch (snackType)
        {
            case 1:
                position = new Vector3(-0.002f, 0.52f, 1.62f);
                rotation = Quaternion.identity;
                break;
            case 2:
                position = new Vector3(0.021f, 0.535f, 1.76f);
                rotation = Quaternion.Euler(23.88f, 1.8f, -48.04f);
                break;
            default:
                position = new Vector3(-0.002f, 0.52f, 1.62f);
                rotation = Quaternion.identity;
                break;
        }

        snack.transform.localPosition = position;
        snack.transform.localRotation = rotation;

        Rigidbody rb = snack.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        if (_dogStateController != null)
        {
            _dogStateController.OnGiveSnack();
        }
    }
    private IEnumerator FloatSnack(GameObject snack, int snackType)
    {
        float amplitude;
        float frequency;

        switch (snackType)
        {
            case 1: // 例: Bone
                amplitude = 0.01f;
                frequency = 2.0f;
                break;
            case 2: // 例: ChuRuChuRu
                amplitude = 0.01f;
                frequency = 0.5f;
                break;
            default:
                amplitude = 0.01f;
                frequency = 5f;
                break;
        }

        // 初期のXとZの値を保持
        Vector3 originalPosition = snack.transform.localPosition;

        while (snack.activeSelf)
        {
            if (snack != null)
            {
                // Y軸のオフセットを計算し、XとZは固定
                float yOffset = amplitude * Mathf.Sin(Time.time * frequency);
                snack.transform.localPosition = new Vector3(
                    originalPosition.x,
                    originalPosition.y + yOffset,
                    originalPosition.z
                );
            }
            yield return null;
        }
    }

    /// <summary>
    /// Snack の終了処理をトリガーします。
    /// </summary>
    public void TriggerSnackCompletion()
    {

        if (snackCoroutine != null)
        {
            StopCoroutine(snackCoroutine);
            snackCoroutine = null;
        }

        snackAnimator.SetInteger("snackType", 0);
        _dogController.UpdateTransitionState(0);
        _hungerManager.IncreaseHungerState();
        GlobalVariables.CurrentState = PetState.idle;
        _turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 2.0f);
        DeactivateCurrentSnack();
    }


    /// <summary>
    /// Snack アニメーション01の進行を監視
    /// </summary>
    private IEnumerator BornSnackAnimation(int snackType, GameObject snack, float duration)
    {
        if (snackAnimator == null)
        {
            GameLogger.LogError(LogCategory.Dog,"Animator is not assigned.");
            yield break;

        }

        _mainUiButtons.UpdateButtonVisibility(false);
        _turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 1f), 1.5f, PetState.snack);

        _dogController.UpdateSnackType(snackType);

        float elapsedTime = 0f;

        while (!snackAnimator.GetCurrentAnimatorStateInfo(0).IsName("055_Expression_Eat"))
        {
            yield return null;
        }

        yield return StartCoroutine(BoneToMouthEvent(snackType, snack));
        _dogController.UpdateTransitionState(1);

        // anim12_bite_Bone が開始されるまで待機
        while (!snackAnimator.GetCurrentAnimatorStateInfo(0).IsName("idle_Blend_lying"))
        {
            yield return null;
        }
        DetachSnack(snack);

        while (elapsedTime < duration)
        {

            elapsedTime += Time.deltaTime;
            yield return null;
            if (snackCoroutine == null)
            {
                yield break;
            }

        }
        TimeEndEvent();
    }
    private IEnumerator ChuRuChuRuSnackAnimation(int snackType, float duration)
    {
        if (snackAnimator == null)
        {
            GameLogger.LogError(LogCategory.Dog,"Animator is not assigned.");
            yield break;
        }
        float elapsedTime = 0f;
        _mainUiButtons.UpdateButtonVisibility(false);
        _turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 1f), 1.5f, PetState.snack);

        _dogController.UpdateSnackType(snackType);
        _mainUICloseButton.ShowUI();

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
            GameLogger.Log(LogCategory.Dog,"ChuRuChuRuSnackElapseTime." + elapsedTime);
            if (snackCoroutine == null)
            {
                yield break;
            }
        }
        TimeEndEvent();
        GameLogger.Log(LogCategory.Dog,"Snack completion triggered ChuRuChuRu.");
    }
    /// <summary>
    /// スナックを犬の口に合わせ、必要に応じて浮遊コルーチンを停止します。
    /// </summary>
    /// <param name="snackType">スナックのタイプ</param>
    /// <param name="snack">スナックのオブジェクト</param>
    public IEnumerator BoneToMouthEvent(int snackType, GameObject snack)
    {
        // 浮遊コルーチンを停止
        StopFloatingCoroutine();

        // snackType の有効性と dogMouthPosition の確認
        if (snackType <= 0 || snackType > snacks.Length)
        {
            GameLogger.LogError(LogCategory.Dog,$"Invalid snack type: {snackType}. No snack available.");
            yield break;
        }

        if (dogMouthPosition == null)
        {
            GameLogger.LogError(LogCategory.Dog,"Dog mouth position is not assigned.");
            yield break;
        }

        // スナックを犬の口の位置に合わせる
        snack.transform.SetParent(dogMouthPosition);
        snack.transform.localPosition = Vector3.zero;
    }

    // Snack を切り離し落とす
    private void DetachSnack(GameObject snack)
    {
        GlobalVariables.CurrentState = PetState.snack;
        snack.transform.SetParent(null);
        Rigidbody rb = snack.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = snack.AddComponent<Rigidbody>(); // Rigidbody がない場合は追加
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        _mainUICloseButton.ShowUI();
    }
    private void StopFloatingCoroutine()
    {
        if (floatingCoroutine != null)
        {
            StopCoroutine(floatingCoroutine);
            floatingCoroutine = null;
        }
    }
    private void TimeEndEvent()
    {
        _mainUICloseButton.SetActiveState(false);
        _dogController.UpdateTransitionState(0);
        TriggerSnackCompletion();
        GameLogger.Log(LogCategory.Dog,"Snack completion triggered TimeUp.");
    }
}
