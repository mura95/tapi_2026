using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TapHouse.Logging;
using TapHouse.MultiDevice;

public class TouchController : MonoBehaviour
{
    [SerializeField] public DogController characterController;
    [SerializeField] public FirebaseManager pettingManager;
    [SerializeField] private TurnAndMoveHandler turnAndMoveHandler;
    [SerializeField] private MaskLayerManager _maskLayerManager;
    [SerializeField] private SleepController _sleepController;
    [SerializeField] private Image pettingIcon;
    [SerializeField] private string targetLayerName = "dog";
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private DogStateController _dogStateController;

    private RectTransform pettingIconRectTransform;
    private RectTransform canvasRectTransform;
    private Canvas canvas;
    private int layerMask;
    private bool isTouching = false;
    private float touchTimer = 0f;
    private float releaseTouchTimer = 0f;
    private bool isInLieOnBackState = false;

    private const float MaxTouchDuration = 3f;
    private const float ResetDelay = 3f;
    private const float RaycastDistance = 5f;
    private float LieOnBackAnimationLength = 0f;
    private bool continuousTouch = false;
    private bool shouldLogPetting = false;
    private DogTransferAnimation _dogTransferAnimation;

    void Start()
    {
        if (pettingIcon == null)
        {
            GameLogger.LogError(LogCategory.General,"PettingIcon is not assigned in TouchController!");
            return;
        }

        pettingIconRectTransform = pettingIcon.GetComponent<RectTransform>();
        canvas = pettingIcon.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            GameLogger.LogError(LogCategory.General,"Canvas not found for pettingIcon!");
            return;
        }

        canvasRectTransform = canvas.GetComponent<RectTransform>();
        pettingIcon.enabled = false;

        if (string.IsNullOrEmpty(targetLayerName))
        {
            GameLogger.LogWarning(LogCategory.General,"targetLayerName is null or empty, using default 'dog' layer");
            targetLayerName = "dog";
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        int targetLayer = LayerMask.NameToLayer(targetLayerName);

        if (targetLayer == -1)
        {
            GameLogger.LogError(LogCategory.General,$"Layer '{targetLayerName}' does not exist! Please create it in the Layer settings.");
            layerMask = LayerMask.GetMask("Default");
        }
        else
        {
            layerMask = LayerMask.GetMask(targetLayerName) & ~(1 << uiLayer);
        }

        GameLogger.Log(LogCategory.General,$"LayerMask setup complete. TargetLayer: {targetLayerName}, LayerMask: {layerMask}");

        // DogTransferAnimationをキャッシュ
        _dogTransferAnimation = FindObjectOfType<DogTransferAnimation>();
    }

    private void Update()
    {
        bool currentTouchInput = Input.touchCount > 0 || Input.GetMouseButton(0);
        Vector2 screenPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

        // マルチデバイス：犬がいない場合、または転送アニメーション中はタッチ処理をスキップ
        if (DogLocationSync.Instance != null)
        {
            // 犬がいない場合
            if (!DogLocationSync.Instance.HasDog)
            {
                pettingIcon.enabled = false;
                ResetTouchState("Dog not present (multi-device)");
                UpdateTouchState(currentTouchInput);
                return;
            }

            // 転送アニメーション中の場合
            if (_dogTransferAnimation != null && _dogTransferAnimation.IsAnimating)
            {
                pettingIcon.enabled = false;
                ResetTouchState("Dog transfer animation in progress");
                UpdateTouchState(currentTouchInput);
                return;
            }
        }

        // Handle Napping state
        if (GlobalVariables.CurrentState == PetState.napping)
        {
            // Check if currently in sleepy reaction - if so, ignore all touches
            if (_sleepController.IsInSleepyReaction())
            {
                pettingIcon.enabled = false;
                UpdateTouchState(currentTouchInput);
                return;
            }

            // Only trigger when touch starts (not every frame)
            if (currentTouchInput && !isTouching)
            {
                LogDebug("[Napping] Touch detected during nap");
                _sleepController.OnPetTouched();
            }

            pettingIcon.enabled = false;
            ResetTouchState("Napping state");
            UpdateTouchState(currentTouchInput);
            return;
        }

        // reminder状態中はタッチ無効
        if (GlobalVariables.CurrentState == PetState.reminder)
        {
            pettingIcon.enabled = false;
            ResetTouchState("Reminder state");
            UpdateTouchState(currentTouchInput);
            return;
        }

        if (GlobalVariables.IsInputUserName ||
            GlobalVariables.CurrentState != PetState.idle ||
            characterController.GetSleepBool() ||
            characterController.GetSnackType() != 0 ||
            characterController.GetTransitionNo() == 3)
        {
            pettingIcon.enabled = false;
            characterController.Petting(false);
            ResetTouchState("Touch disabled condition");
            UpdateTouchState(currentTouchInput);
            return;
        }

        CheckHoldTouch(currentTouchInput, screenPosition);
        DefaultPetting(currentTouchInput, screenPosition);
        UpdateTouchState(currentTouchInput);
    }

    private void UpdateTouchState(bool currentInput)
    {
        bool previousTouch = isTouching;
        isTouching = currentInput;

        if (previousTouch != isTouching)
        {
            LogDebug($"[Touch State Changed] {previousTouch} -> {isTouching}");
        }
    }

    private void ResetTouchState(string reason)
    {
        bool wasActive = continuousTouch || isTouching;

        continuousTouch = false;
        shouldLogPetting = false;
        touchTimer = 0f;

        if (wasActive)
        {
            LogDebug($"[Touch Reset] Reason: {reason}");
        }
    }

    private void CheckHoldTouch(bool currentTouchInput, Vector2 screenPosition)
    {
        if (currentTouchInput)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPosition, canvas.worldCamera, out var localPoint))
            {
                pettingIconRectTransform.anchoredPosition = localPoint;
                pettingIcon.enabled = true;
            }
            touchTimer += Time.deltaTime;
            releaseTouchTimer = 0;

            if (touchTimer > MaxTouchDuration)
            {
                if (!isInLieOnBackState)
                {
                    LogDebug($"[LieOnBack] Triggered after {touchTimer:F2}s hold");
                    characterController.SetLieOnBackTrue();
                    characterController.UpdateTransitionState(4);
                    isInLieOnBackState = true;
                    StartCoroutine(UpdateLieOnBackStateLoop());
                }
            }
        }
        else if (isInLieOnBackState)
        {
            pettingIcon.enabled = false;
            releaseTouchTimer += Time.deltaTime;
            touchTimer = 0;

            if (releaseTouchTimer >= ResetDelay)
            {
                LogDebug($"[LieOnBack] Exiting after {releaseTouchTimer:F2}s release delay");
                isInLieOnBackState = false;
                characterController.UpdateTransitionState(0);
                releaseTouchTimer = 0;

                characterController.Petting(false);
                continuousTouch = false;
            }
        }
        else
        {
            pettingIcon.enabled = false;
            touchTimer = 0;
        }
    }

    private async void DefaultPetting(bool currentTouchInput, Vector2 screenPosition)
    {
        if (isInLieOnBackState)
        {
            if (!currentTouchInput)
            {
                characterController.Petting(false);
                continuousTouch = false;
                LogDebug("[LieOnBack] Petting disabled - no touch");
            }
            return;
        }

        if (!currentTouchInput && isTouching)
        {
            LogDebug("[Touch End] Triggering bark and resetting");
            characterController.Petting(false);

            if (shouldLogPetting)
            {
                LogDebug("[Petting Log] Sending petting log to Firebase");
                pettingManager.UpdateLog("skill");
                _dogStateController.OnPet();
                shouldLogPetting = false;
            }

            continuousTouch = false;
            _maskLayerManager.SetLayerWeight("Bark", 1, 3f);
            characterController.LayerBarkTrigger();
            GlobalVariables.AttentionCount = 0;
            return;
        }

        if (currentTouchInput)
        {
            if (characterController.transform.position.sqrMagnitude > 0.1f)
            {
                LogDebug($"[Position Check] Dog too far: {characterController.transform.position.sqrMagnitude:F2}");
                characterController.Petting(false);
                pettingIcon.enabled = false;
                turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.5f);
                continuousTouch = false;
                shouldLogPetting = false;
                return;
            }

            if (Camera.main == null)
            {
                GameLogger.LogError(LogCategory.General,"[Camera Error] Main Camera is null. Cannot perform raycast.");
                characterController.Petting(false);
                continuousTouch = false;
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, RaycastDistance, layerMask))
            {
                if (hit.collider != null && hit.collider.gameObject == characterController.gameObject)
                {
                    Vector3 localPointInObject = characterController.transform.InverseTransformPoint(hit.point);

                    Collider collider = characterController.GetComponent<Collider>();
                    if (collider != null)
                    {
                        float objectWidth = collider.bounds.size.x;
                        float normalizedX = (localPointInObject.x / objectWidth) + 0.5f;

                        if (!continuousTouch)
                        {
                            LogDebug($"[Petting Start] Hit at normalized X: {normalizedX:F2}");
                            continuousTouch = true;
                            shouldLogPetting = true;
                        }

                        characterController.SetPatFloat(normalizedX);
                        characterController.UpdateTransitionState(0);
                        _maskLayerManager.SetLayerWeight("face", 0);
                        characterController.Petting(true);
                    }
                    else
                    {
                        GameLogger.LogError(LogCategory.General,"[Collider Error] Collider is missing on the characterController game object.");
                        characterController.Petting(false);
                        continuousTouch = false;
                    }
                }
                else
                {
                    if (continuousTouch)
                    {
                        LogDebug($"[Raycast] Hit other object: {hit.collider?.gameObject.name}");
                    }
                    characterController.Petting(false);
                    continuousTouch = false;
                }
            }
            else
            {
                if (continuousTouch)
                {
                    LogDebug("[Raycast] No hit detected");
                }
                characterController.Petting(false);
                continuousTouch = false;
            }
        }
    }

    private IEnumerator UpdateLieOnBackStateLoop()
    {
        LogDebug("[LieOnBack Loop] Started");
        while (isInLieOnBackState)
        {
            if (releaseTouchTimer > 0)
            {
                characterController.UpdateLieOnBackState(2);
            }
            else
            {
                characterController.UpdateLieOnBackState(Random.Range(0, 3));
            }
            LieOnBackAnimationLength = characterController.GetCurrentAnimationLength();
            LogDebug($"[LieOnBack Loop] Animation length: {LieOnBackAnimationLength:F2}s");
            yield return new WaitForSeconds(LieOnBackAnimationLength);
        }
        LogDebug("[LieOnBack Loop] Ended");
    }

    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            GameLogger.Log(LogCategory.General,$"[TouchController] {message}");
        }
    }
}