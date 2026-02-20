using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TapHouse.Logging;

/// <summary>
/// ボールとロープのおもちゃ取得処理の基底クラス
/// 共通処理を集約し、重複を排除
/// </summary>
public abstract class ToyFetcherBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected DogController dogController;
    [SerializeField] protected FirebaseManager firebaseManager;
    [SerializeField] protected MainUIButtons mainUiButtons;
    [SerializeField] protected MainUICloseButton mainUICloseButton;
    [SerializeField] protected DogStateController dogStateController;
    [SerializeField] protected TurnAndMoveHandler turnAndMoveHandler;
    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Transform mouthPosition;
    [SerializeField] protected Camera mainCamera;

    [Header("Movement Settings")]
    [SerializeField] protected float runMaxSpeed = 5f;
    [SerializeField] protected float returnSpeed = 4f;

    [Header("Toy Limits")]
    [SerializeField] protected float maxToyDistance = 50f;
    [SerializeField] protected float maxPlayTime = 180f;

    protected GameObject currentToy;
    protected Vector3 initialPosition = Vector3.zero;
    protected Quaternion toyInitialRotation;
    protected bool isReturning = false;
    protected float playTimer = 0f;

    protected bool BeNaviMesh
    {
        get => agent != null && agent.enabled;
        set
        {
            if (agent != null)
            {
                agent.enabled = value;
            }
        }
    }

    protected virtual void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        initialPosition = transform.position;
    }

    protected virtual void Update()
    {
        if (currentToy == null)
        {
            return;
        }

        if (!BeNaviMesh) return;

        Vector3 targetPosition = isReturning ? initialPosition : currentToy.transform.position;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPosition);
        }
        else
        {
            GameLogger.LogWarning(LogCategory.PlayToy,"Agent is not on NavMesh!");
            return;
        }

        UpdateMovementAnimation();

        if (isReturning && Vector3.Distance(transform.position, initialPosition) <= agent.stoppingDistance)
        {
            OnReachedReturn();
            return;
        }

        // タイムアウトと距離チェック（遊び中のみ）
        if (!isReturning)
        {
            playTimer += Time.deltaTime;
            if (playTimer > maxPlayTime ||
                (mainCamera != null && Vector3.Distance(mainCamera.transform.position, currentToy.transform.position) > maxToyDistance))
            {
                GameLogger.Log(LogCategory.PlayToy,"Toy play timeout or too far away");
                EndPlayWithReturn();
            }
        }
    }

    // ToyFetcherBase.FetchToy() に追加
    public virtual void FetchToy(GameObject toy)
    {
        if (toy == null)
        {
            GameLogger.LogWarning(LogCategory.PlayToy,"FetchToy: toy is null");
            return;
        }

        GameLogger.Log(LogCategory.PlayToy,$"FetchToy: {toy.name}");
        GameLogger.Log(LogCategory.PlayToy,$"FetchToy: Dog position={transform.position}, Dog rotation={transform.eulerAngles}");
        GameLogger.Log(LogCategory.PlayToy,$"FetchToy: Toy position={toy.transform.position}");
        GameLogger.Log(LogCategory.PlayToy,$"FetchToy: Distance to toy={Vector3.Distance(transform.position, toy.transform.position)}");

        currentToy = toy;
        toyInitialRotation = toy.transform.rotation;
        playTimer = 0f;
        isReturning = false;

        EnsureAgentOnNavMesh();
        BeNaviMesh = true;

        if (agent != null)
        {
            agent.speed = runMaxSpeed;
            agent.updateRotation = true;  // NavMeshAgentが自動で目的地に向かって回転
            agent.angularSpeed = 180f;    // 回転速度（度/秒）
            GameLogger.Log(LogCategory.PlayToy,$"FetchToy: agent.speed={agent.speed}, agent.updateRotation={agent.updateRotation}");
        }
    }

    /// <summary>
    /// NavMeshAgentの速度に基づいてアニメーション更新
    /// </summary>
    protected void UpdateMovementAnimation()
    {
        if (agent == null || dogController == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        float normalizedSpeedX = Mathf.Clamp(Mathf.Abs(localVelocity.x) / runMaxSpeed, 0, 1);
        float normalizedSpeedZ = Mathf.Clamp(Mathf.Abs(localVelocity.z) / runMaxSpeed, 0, 1);
        dogController.StartMoving(normalizedSpeedX, normalizedSpeedZ);
    }

    /// <summary>
    /// カメラの方向を向く
    /// </summary>
    protected void RotateTowardsCamera()
    {
        Quaternion targetRotation = Quaternion.Euler(0, 0, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    /// <summary>
    /// 飼い主の元に戻る処理開始
    /// </summary>
    protected void StartReturn()
    {
        GameLogger.Log(LogCategory.PlayToy,"StartReturn called");

        EnsureAgentOnNavMesh();
        BeNaviMesh = true;

        if (agent != null)
        {
            agent.speed = returnSpeed;
            isReturning = true;
            agent.SetDestination(initialPosition);
        }
    }

    /// <summary>
    /// NavMesh上に再配置
    /// </summary>
    protected void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            if (!agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }
            BeNaviMesh = true;
        }
        else
        {
            GameLogger.LogError(LogCategory.PlayToy,"Agent cannot be repositioned on NavMesh!");
        }
    }

    /// <summary>
    /// おもちゃをリセット
    /// </summary>
    protected void ResetToy()
    {
        if (currentToy == null) return;

        GameLogger.Log(LogCategory.PlayToy,"ResetToy called");

        Rigidbody rb = currentToy.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        currentToy.transform.SetParent(null);
        currentToy.SetActive(false);
        currentToy = null;

        GlobalVariables.AttentionCount = 0;
        mainUICloseButton?.SetActiveState(false);
        firebaseManager?.UpdatePetState("idle");
        dogStateController?.OnPlay();
    }

    /// <summary>
    /// 遊び終了処理（即座に終了）
    /// </summary>
    public virtual void EndPlay()
    {
        GameLogger.Log(LogCategory.PlayToy,"ToyFetcherBase.EndPlay called");

        playTimer = 0f;

        // おもちゃがない場合は直接終了処理
        if (currentToy == null)
        {
            BeNaviMesh = false;
            isReturning = false;
            GlobalVariables.CurrentState = PetState.idle;
            OnPlayEnd();
            return;
        }

        // おもちゃを非アクティブ化
        if (currentToy.activeSelf)
        {
            currentToy.SetActive(false);
        }

        // NavMeshを無効化
        BeNaviMesh = false;
        isReturning = false;

        turnAndMoveHandler?.StartTurnAndMove(Vector3.zero, 1f);
        GlobalVariables.CurrentState = PetState.idle;
        OnPlayEnd();
    }

    /// <summary>
    /// 遊び終了処理（戻ってから終了）
    /// </summary>
    public virtual void EndPlayWithReturn()
    {
        GameLogger.Log(LogCategory.PlayToy,"ToyFetcherBase.EndPlayWithReturn called");

        playTimer = 0f;

        // おもちゃがない場合は直接終了処理
        if (currentToy == null)
        {
            EndPlay();
            return;
        }

        // NavMeshを有効化して戻る準備
        EnsureAgentOnNavMesh();
        isReturning = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(initialPosition);
        }
    }

    // 派生クラスでオーバーライドするメソッド
    protected abstract void OnReachedReturn();
    protected abstract void OnPlayEnd();
}