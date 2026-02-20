// GetToy.cs

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TapHouse.Logging;

public class GetToy : ToyFetcherBase
{
    [SerializeField] private PlayToy playToy;
    private bool shouldReturn = false;
    private Coroutine playSequenceCoroutine;
    private bool isPlayingWithToy = false;

    private Rigidbody cachedToyRigidbody;
    private Collider cachedToyCollider;

    protected override void Update()
    {
        base.Update();

        if (isReturning)
        {
            RotateTowardsCamera();
        }

        // �� �ǉ�: �����ŃL���b�`����i�R���C�_�[���������Ȃ����̕⏕�j
        if (currentToy != null && !isPlayingWithToy && playSequenceCoroutine == null && BeNaviMesh)
        {
            float distanceToToy = Vector3.Distance(mouthPosition.position, currentToy.transform.position);
            if (distanceToToy < 0.3f)  // ���Ƃ̋�����0.3m�ȓ�
            {
                GameLogger.Log(LogCategory.PlayToy,$"GetToy: Caught by distance check ({distanceToToy:F2}m)");
                CatchToy();
            }
        }
    }

    /// <summary>
    /// ����������L���b�`���鏈���iOnCollisionEnter�Ƌ��ʉ��j
    /// </summary>
    private void CatchToy()
    {
        if (currentToy == null) return;
        if (isPlayingWithToy || playSequenceCoroutine != null) return;

        string toyName = currentToy.name;
        if (!toyName.Contains("Rope") && !toyName.Contains("Dental_Ball")) return;

        GameLogger.Log(LogCategory.PlayToy,$"GetToy: Caught {toyName}");
        isPlayingWithToy = true;

        if (cachedToyCollider != null)
            cachedToyCollider.enabled = false;

        if (cachedToyRigidbody != null)
            cachedToyRigidbody.isKinematic = true;

        currentToy.transform.SetParent(mouthPosition);
        currentToy.transform.localPosition = Vector3.zero;
        currentToy.transform.rotation = toyInitialRotation;

        if (shouldReturn)
        {
            StartReturn();
        }
        else
        {
            BeNaviMesh = false;
            dogController.StartMoving(0, 0);

            if (playToy != null)
                playToy.CancelPlay();

            playSequenceCoroutine = StartCoroutine(PlayToySequence());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentToy == null || collision.gameObject != currentToy)
            return;

        CatchToy();
    }

    private IEnumerator PlayToySequence()
    {
        GameLogger.Log(LogCategory.PlayToy,"PlayToySequence: START");

        if (playToy == null)
        {
            GameLogger.LogError(LogCategory.PlayToy,"PlayToy is null!");
            isPlayingWithToy = false;
            playSequenceCoroutine = null;
            yield break;
        }

        if (currentToy == null)
        {
            GameLogger.LogError(LogCategory.PlayToy,"CurrentToy is null!");
            isPlayingWithToy = false;
            playSequenceCoroutine = null;
            yield break;
        }

        yield return StartCoroutine(playToy.DoPlayCoroutine(currentToy));

        GameLogger.Log(LogCategory.PlayToy,"PlayToySequence: FINISHED");
        isPlayingWithToy = false;
        playSequenceCoroutine = null;

        if (currentToy != null && !shouldReturn)
        {
            GameLogger.Log(LogCategory.PlayToy,"PlayToySequence: Starting next fetch");
            FetchToy(currentToy);
        }
    }

    public override void FetchToy(GameObject toy)
    {
        base.FetchToy(toy);
        shouldReturn = false;
        isPlayingWithToy = false;

        cachedToyRigidbody = toy.GetComponent<Rigidbody>();
        cachedToyCollider = toy.GetComponent<Collider>();

        if (cachedToyCollider != null)
            cachedToyCollider.enabled = true;

        // �m����agent��]��L�����ibase.FetchToy��ɍĐݒ�j
        if (agent != null)
        {
            agent.updateRotation = true;
            agent.angularSpeed = 180f;
            GameLogger.Log(LogCategory.PlayToy,$"GetToy.FetchToy() - agent.enabled={agent.enabled}, agent.updateRotation={agent.updateRotation}, agent.angularSpeed={agent.angularSpeed}");
        }

        GlobalVariables.CurrentState = PetState.toy;
        mainUICloseButton?.SetActiveState(true);
        GameLogger.Log(LogCategory.PlayToy,$"GetToy.FetchToy() called: {toy.name}");
    }

    /// <summary>
    /// �V�яI�� - �S�Ă̕Еt���������ōs��
    /// </summary>
    public override void EndPlay()
    {
        GameLogger.Log(LogCategory.PlayToy,"GetToy.EndPlay() called");

        if (playSequenceCoroutine != null)
        {
            StopCoroutine(playSequenceCoroutine);
            playSequenceCoroutine = null;
        }

        if (playToy != null)
        {
            playToy.CancelPlay();
        }

        isPlayingWithToy = false;
        shouldReturn = false;
        isReturning = false;
        playTimer = 0f;

        BeNaviMesh = false;
        dogController.StartMoving(0, 0);

        CleanupToy();

        GlobalVariables.CurrentState = PetState.idle;
        turnAndMoveHandler?.StartTurnAndMove(Vector3.zero, 1f);
        mainUiButtons?.UpdateButtonVisibility(true);
        firebaseManager?.UpdatePetState("idle");

        GameLogger.Log(LogCategory.PlayToy,"GetToy.EndPlay() completed");
    }

    /// <summary>
    /// ��������̕Еt������
    /// </summary>
    private void CleanupToy()
    {
        if (currentToy == null)
        {
            cachedToyRigidbody = null;
            cachedToyCollider = null;
            return;
        }

        GameLogger.Log(LogCategory.PlayToy,$"CleanupToy: {currentToy.name}");

        if (cachedToyCollider != null)
            cachedToyCollider.enabled = true;

        if (cachedToyRigidbody != null)
            cachedToyRigidbody.isKinematic = false;

        currentToy.transform.SetParent(null);
        currentToy.SetActive(false);
        currentToy = null;
        cachedToyRigidbody = null;
        cachedToyCollider = null;
        GlobalVariables.AttentionCount = 0;
    }
    // GetToy.cs

    /// <summary>
    /// �^�C���A�E�g�E�������ߎ��̏I���i�߂��Ă��Ă���I���j
    /// </summary>
    public override void EndPlayWithReturn()
    {
        GameLogger.Log(LogCategory.PlayToy,"GetToy.EndPlayWithReturn() called");

        // �R���[�`����~
        if (playSequenceCoroutine != null)
        {
            StopCoroutine(playSequenceCoroutine);
            playSequenceCoroutine = null;
        }

        if (playToy != null)
        {
            playToy.CancelPlay();
        }

        isPlayingWithToy = false;

        // ������������ɂ��킦��
        if (currentToy != null)
        {
            currentToy.transform.SetParent(mouthPosition);
            currentToy.transform.localPosition = Vector3.zero;

            if (cachedToyCollider != null)
                cachedToyCollider.enabled = false;

            if (cachedToyRigidbody != null)
                cachedToyRigidbody.isKinematic = true;
        }

        shouldReturn = true;
        StartReturn();
    }

    /// <summary>
    /// ������ɓ���������
    /// </summary>
    protected override void OnReachedReturn()
    {
        GameLogger.Log(LogCategory.PlayToy,"GetToy.OnReachedReturn() called");

        BeNaviMesh = false;
        dogController.StartMoving(0, 0);
        isReturning = false;

        StartCoroutine(ReturnSequence());
    }

    /// <summary>
    /// �߂��Ă�����̏����i��������𗎂Ƃ��ď�����j
    /// </summary>
    private IEnumerator ReturnSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (currentToy != null)
        {
            // ��������������痣��
            currentToy.transform.SetParent(null);

            // ���Ƃ��i�d�͂ŗ����j
            if (cachedToyRigidbody != null)
            {
                cachedToyRigidbody.isKinematic = false;
            }

            if (cachedToyCollider != null)
                cachedToyCollider.enabled = true;

            dogController.ActionBool(true);
        }

        yield return new WaitForSeconds(1.5f);

        // �Еt��
        CleanupToy();

        // ��ԃ��Z�b�g
        shouldReturn = false;
        GlobalVariables.CurrentState = PetState.idle;

        // UI�X�V
        mainUiButtons?.UpdateButtonVisibility(true);
        firebaseManager?.UpdatePetState("idle");

        GameLogger.Log(LogCategory.PlayToy,"ReturnSequence completed");
    }
    protected override void OnPlayEnd()
    {
        GameLogger.Log(LogCategory.PlayToy,"GetToy.OnPlayEnd() called");
        ResetToy();
        mainUiButtons?.UpdateButtonVisibility(true);
    }

    private void OnDestroy()
    {
        GameLogger.Log(LogCategory.PlayToy,"GetToy.OnDestroy() called");
        StopAllCoroutines();
        playSequenceCoroutine = null;

        if (playToy != null)
        {
            playToy.CancelPlay();
        }

        isPlayingWithToy = false;
        cachedToyRigidbody = null;
        cachedToyCollider = null;
    }
}