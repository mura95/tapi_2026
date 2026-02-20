using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;


/// <summary>
/// 遊び管理（バグ修正版）
/// カメラ視野内制御を強化し、投擲履歴を追跡
/// </summary>
public class PlayManager : MonoBehaviour
{
    [Header("Toys")]
    [SerializeField] private GameObject[] playToys;

    [Header("References")]
    [SerializeField] private DogController dogController;
    [SerializeField] private MainUIButtons mainUiButtons;
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private GetBall getBall;
    [SerializeField] private GetToy getToy;

    [Header("Toy Spawn Settings")]
    [SerializeField] private Vector3 startToyPosition = new Vector3(0, 0.6f, 1.9f);
    [SerializeField] private float floatAmplitude = 0.05f;
    [SerializeField] private float floatFrequency = 1.0f;

    [Header("Throw Settings")]
    [SerializeField] private float throwInitialSpeed = 10f;
    [SerializeField] private float maxThrowSpeed = 15f;
    [SerializeField] private float throwAngleClamp = 20f;

    private GameObject currentToy;
    private int currentToyType = 0;
    private Coroutine floatingCoroutine;
    private bool isToyFloating = false;
    private Camera mainCamera; // キャッシュ

    // 投擲履歴（カメラ外投擲の検出用）
    private Queue<Vector3> throwHistory = new Queue<Vector3>(3);
    private const int MAX_HISTORY = 3;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameLogger.LogError(LogCategory.PlayToy,"[PlayManager] Main Camera not found!");
        }
        DeactivateAllToys();
    }

    /// <summary>
    /// おもちゃ遊び開始
    /// </summary>
    public void StartToyAction(int playNum)
    {
        if (playNum <= 0 || playNum > playToys.Length)
        {
            GameLogger.LogError(LogCategory.PlayToy,$"Invalid toy number: {playNum}");
            return;
        }

        dogController.ActionBool(false);
        mainUiButtons.UpdateButtonVisibility(false);
        DeactivateAllToys();

        currentToyType = playNum;
        currentToy = playToys[currentToyType - 1];
        ReadyToy(currentToy);
    }

    /// <summary>
    /// 全てのおもちゃを非表示
    /// </summary>
    private void DeactivateAllToys()
    {
        foreach (GameObject toy in playToys)
        {
            if (toy != null) toy.SetActive(false);
        }
    }

    /// <summary>
    /// おもちゃを準備
    /// </summary>
    private void ReadyToy(GameObject toy)
    {
        if (toy == null)
        {
            GameLogger.LogError(LogCategory.PlayToy,"Toy is not assigned.");
            return;
        }

        toy.SetActive(true);
        BringToy(toy);
    }

    /// <summary>
    /// おもちゃを画面中央に浮遊させる
    /// </summary>
    private void BringToy(GameObject toy)
    {
        toy.transform.localPosition = startToyPosition;
        toy.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        Rigidbody rb = toy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        isToyFloating = true;
        dogController.ActionBool(true);

        if (floatingCoroutine != null)
        {
            StopCoroutine(floatingCoroutine);
        }
        floatingCoroutine = StartCoroutine(FloatToyCoroutine(startToyPosition, toy));
    }

    /// <summary>
    /// おもちゃを上下に浮遊させる
    /// </summary>
    private IEnumerator FloatToyCoroutine(Vector3 basePosition, GameObject toy)
    {
        while (isToyFloating && toy != null && toy.activeSelf)
        {
            float yOffset = floatAmplitude * Mathf.Sin(Time.time * floatFrequency);
            toy.transform.localPosition = new Vector3(basePosition.x, basePosition.y + yOffset, basePosition.z);
            yield return null;
        }
    }

    /// <summary>
    /// おもちゃアクションをキャンセル
    /// </summary>
    public void CancelToyAction()
    {
        if (isToyFloating)
        {
            isToyFloating = false;

            if (floatingCoroutine != null)
            {
                StopCoroutine(floatingCoroutine);
                floatingCoroutine = null;
            }

            DeactivateAllToys();
        }
    }

    /// <summary>
    /// おもちゃを投げる（カメラ視野内制御あり）
    /// </summary>
    public void ThrowToy(float throwSpeed = 1f, float throwAngle = 0f, GameObject toy = null, string toyName = null, bool throwBehind = false)
    {
        if (toy == null)
        {
            GameLogger.LogWarning(LogCategory.PlayToy,"Toy is null, cannot throw.");
            return;
        }

        Rigidbody rb = toy.GetComponent<Rigidbody>();
        if (rb == null)
        {
            GameLogger.LogWarning(LogCategory.PlayToy,"Toy has no Rigidbody.");
            return;
        }

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = false;

        dogController.ActionBool(false);

        // 速度と角度の調整
        float adjustedSpeed = Mathf.Clamp(throwInitialSpeed * throwSpeed, 0, maxThrowSpeed);
        throwAngle = Mathf.Clamp(throwAngle, -throwAngleClamp, throwAngleClamp);

        // 投擲方向（前か後ろか）
        int direction = throwBehind ? -1 : 1;
        Vector3 throwDirection = Quaternion.Euler(45f, throwAngle, 0) * transform.forward * direction;
        Vector3 force = throwDirection * adjustedSpeed;

        rb.AddForce(force, ForceMode.VelocityChange);
        if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
        {
            GameLogger.LogError(LogCategory.PlayToy,"NaN DETECTED in FORCE");
            return;
        }
        GameLogger.Log(LogCategory.PlayToy,$"ThrowToy: Speed={adjustedSpeed}, Angle={throwAngle}, Direction={(throwBehind ? "Behind" : "Front")}");
        // 投擲位置を履歴に追加（カメラ外検出用）
        AddThrowHistory(toy.transform.position);

        // おもちゃの種類に応じた処理
        ProcessToyType(toy, toyName);

        isToyFloating = false;

        if (floatingCoroutine != null)
        {
            StopCoroutine(floatingCoroutine);
            floatingCoroutine = null;
        }
    }

    /// <summary>
    /// おもちゃの種類に応じた処理
    /// </summary>
    private void ProcessToyType(GameObject toy, string toyName)
    {
        if (string.IsNullOrEmpty(toyName))
        {
            toyName = toy.name;
        }

        GameLogger.Log(LogCategory.PlayToy,$"ProcessToyType: {toyName}");

        if (toyName.Contains("Ball"))
        {
            getBall?.FetchToy(toy);
        }
        else if (toyName.Contains("Rope") || toyName.Contains("Dental_Ball"))
        {
            getToy?.FetchToy(toy);
        }
    }

    /// <summary>
    /// 投擲履歴に追加
    /// </summary>
    private void AddThrowHistory(Vector3 position)
    {
        throwHistory.Enqueue(position);
        if (throwHistory.Count > MAX_HISTORY)
        {
            throwHistory.Dequeue();
        }
    }

    /// <summary>
    /// カメラ外への投擲が連続しているか確認
    /// </summary>
    public bool IsConsistentlyOutOfView()
    {
        if (throwHistory.Count < 2) return false;
        if (mainCamera == null) return false;

        int outOfViewCount = 0;
        foreach (Vector3 pos in throwHistory)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(pos);
            if (viewportPoint.x < 0 || viewportPoint.x > 1 || viewportPoint.y < 0 || viewportPoint.y > 1 || viewportPoint.z < 0)
            {
                outOfViewCount++;
            }
        }

        return outOfViewCount >= 2; // 2回以上カメラ外なら調整が必要
    }

    private void Update()
    {
        if (isToyFloating && currentToy != null && Input.GetMouseButtonDown(0))
        {
            HandleToyTap();
        }
    }

    /// <summary>
    /// おもちゃタップ処理
    /// </summary>
    private void HandleToyTap()
    {
        if (mainCamera == null)
        {
            GameLogger.LogError(LogCategory.PlayToy,"Main Camera is null. Cannot perform raycast.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == currentToy)
        {
            string toyName = currentToy.name;
            float randomAngle = UnityEngine.Random.Range(-10f, 10f);
            float randomSpeed = GetRandomSpeedForToy(toyName);

            GameLogger.Log(LogCategory.PlayToy,$"Toy tapped: {toyName}, throwing...");
            ThrowToy(randomSpeed, randomAngle, currentToy, toyName, true);
        }
    }

    /// <summary>
    /// おもちゃの種類に応じたランダム速度
    /// </summary>
    private float GetRandomSpeedForToy(string toyName)
    {
        if (toyName.Contains("Ball"))
        {
            return UnityEngine.Random.Range(0.8f, 1.5f);
        }
        else if (toyName.Contains("Rope"))
        {
            return UnityEngine.Random.Range(0.8f, 1.0f);
        }
        else if (toyName.Contains("Dental_Ball"))
        {
            return UnityEngine.Random.Range(0.4f, 0.7f);
        }
        return 1.0f;
    }
}