using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using TapHouse.Logging;

/// <summary>
/// ボール取得処理（完全修正版）
/// Ball専用 - Rope/Dental_Ballには反応しない
/// </summary>
public class GetBall : ToyFetcherBase
{
    protected override void Update()
    {
        base.Update();

        if (isReturning)
        {
            RotateTowardsCamera();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentToy == null || collision.gameObject != currentToy)
            return;

        string toyName = currentToy.name;
        if (!toyName.Contains("Ball") || toyName.Contains("Dental_Ball"))
        {
            GameLogger.Log(LogCategory.PlayToy,$"GetBall: Ignoring {toyName} (not Ball)");
            return;
        }

        GameLogger.Log(LogCategory.PlayToy,$"GetBall: Caught {toyName}");

        Rigidbody rb = currentToy.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        currentToy.transform.SetParent(mouthPosition);
        currentToy.transform.localPosition = Vector3.zero;

        agent.speed = returnSpeed;
        StartReturn();
    }

    public override void FetchToy(GameObject toy)
    {
        base.FetchToy(toy);
        GlobalVariables.CurrentState = PetState.ball;
        firebaseManager?.UpdatePetState("ball");
        GameLogger.Log(LogCategory.PlayToy,$"GetBall.FetchToy() called: {toy.name}");
    }

    protected override void OnReachedReturn()
    {
        BeNaviMesh = false;
        dogController.StartMoving(0, 0);
        StartCoroutine(ReturnSequence());
    }

    protected override void OnPlayEnd()
    {
        BeNaviMesh = false;
        dogController.StartMoving(0, 0);
        ResetToy();
        mainUiButtons?.UpdateButtonVisibility(true);
    }

    private IEnumerator ReturnSequence()
    {
        yield return new WaitForSeconds(1f);

        // ボールを落とす
        if (currentToy != null)
        {
            Rigidbody rb = currentToy.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
            currentToy.transform.SetParent(null);
            dogController.ActionBool(true);
        }

        yield return new WaitForSeconds(1.5f);

        // ボール終了
        ResetToy();
        mainUiButtons?.UpdateButtonVisibility(true);

        // Firebaseログ更新
        var task = firebaseManager?.UpdateLog("play");
        if (task != null)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.Exception != null)
            {
                GameLogger.LogException(LogCategory.PlayToy, task.Exception);
            }
        }
    }
}