using System.Collections;
using UnityEngine;
using TapHouse.Logging;
using System.Diagnostics;

public class TurnAndMoveHandler : MonoBehaviour
{
    private enum State
    {
        Idle,
        TurningToFront,
        MovingToTarget
    }

    private State _currentState = State.Idle;
    private Vector3 _targetPosition = Vector3.zero;
    private Quaternion _startRotation;
    private Quaternion _endRotation;
    private float _rotationProgress;
    private float rotationThreshold = 5f;
    private Vector3 cameraDirection;
    [SerializeField] private Animator animator;
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float targetProximity = 1.0f;
    private bool isMoving = false;

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraDirection = (Camera.main.transform.position - transform.position).normalized;
            cameraDirection.y = 0;
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Dog, "Main Camera not found!");
        }
    }

    private void Update()
    {
        if (isMoving)
        {
            if (_currentState == State.TurningToFront)
            {
                RotateTowardsTarget();
            }
            else if (_currentState == State.MovingToTarget)
            {
                GameLogger.Log(LogCategory.Dog, "Moving to target");
                MoveTowardsTarget();
            }
        }
    }

    public void StartTurnAndMove(Vector3 targetPosition, float speed = 1.0f, PetState state = PetState.moving)
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget <= 0.1f)
        {
            GameLogger.Log(LogCategory.Dog, "Target position is too close to the current position.");
            return;
        }
        GameLogger.Log(LogCategory.Dog, "petstate: " + state);

        if (state == PetState.moving)
        {
            GlobalVariables.CurrentState = PetState.moving;
            GameLogger.Log(LogCategory.Dog, "petstate: " + GlobalVariables.CurrentState);
        }

        _targetPosition = targetPosition;
        isMoving = true;
        moveSpeed = Mathf.Max(speed, 0.1f);
        _currentState = State.TurningToFront;
        PrepareRotation();
    }

    private void PrepareRotation()
    {
        Vector3 directionToTarget = new Vector3(0, 0, 2.5f).normalized;

        _startRotation = transform.rotation;
        _endRotation = Quaternion.LookRotation(directionToTarget);
        _rotationProgress = 0.0f;

        float angleDifference = Quaternion.Angle(_startRotation, _endRotation);

        if (angleDifference > 0.1f)
        {
            animator.SetBool("TurnBool", true);
        }
        else
        {
            _currentState = State.MovingToTarget;
            animator.SetBool("WalkBool", true);
        }
    }

    private void RotateTowardsTarget()
    {
        float angleToCamera = Vector3.Angle(transform.forward, cameraDirection);

        if (angleToCamera <= rotationThreshold)
        {
            _currentState = State.MovingToTarget;
            animator.SetBool("TurnBool", false);
            animator.SetBool("WalkBool", true);
            return;
        }

        _rotationProgress += Time.deltaTime * rotationSpeed;
        transform.rotation = Quaternion.Lerp(_startRotation, _endRotation, _rotationProgress);

        UpdateTurnAnimation(transform.rotation, _endRotation);

        if (Quaternion.Angle(transform.rotation, _endRotation) <= 0.1f)
        {
            transform.rotation = _endRotation;
            animator.SetBool("TurnBool", false);
            animator.SetFloat("TurnFloat", 0.5f);
            _currentState = State.MovingToTarget;
            animator.SetBool("WalkBool", true);
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 directionToTarget = (_targetPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, _targetPosition);

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);

        UpdateWalkingAnimation(directionToTarget);

        if (distanceToTarget <= targetProximity || distanceToTarget < 0.05f)
        {
            float angleDifferenceY = Mathf.DeltaAngle(transform.eulerAngles.y, 0);

            if (angleDifferenceY > 3f || angleDifferenceY < -3f)
            {
                Quaternion finalRotation = Quaternion.Euler(0, 0, 0);
                StartCoroutine(RotateToFinalDirectionY(finalRotation));
            }

            StopWalkingAnimation();
            animator.SetBool("WalkBool", false);
            isMoving = false;
            if (GlobalVariables.CurrentState == PetState.moving)
            {
                GlobalVariables.CurrentState = PetState.idle;
            }
            _currentState = State.Idle;
        }
    }
    private IEnumerator RotateToFinalDirectionY(Quaternion finalRotation)
    {
        float rotationSpeed = 2.0f;
        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, finalRotation.eulerAngles.y)) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = finalRotation;
    }

    private void UpdateWalkingAnimation(Vector3 direction)
    {
        animator.SetFloat("walk_x", direction.x);
        animator.SetFloat("walk_y", direction.z);
    }

    private void StopWalkingAnimation()
    {
        animator.SetFloat("walk_x", 0);
        animator.SetFloat("walk_y", 0);
    }

    private void UpdateTurnAnimation(Quaternion currentRotation, Quaternion targetRotation)
    {
        Vector3 currentForward = currentRotation * Vector3.forward;
        Vector3 targetForward = targetRotation * Vector3.forward;
        Vector3 cross = Vector3.Cross(currentForward, targetForward);

        if (cross.y > 0)
        {
            animator.SetFloat("TurnFloat", 1.0f);
        }
        else if (cross.y < 0)
        {
            animator.SetFloat("TurnFloat", 0.0f);
        }
    }
}
