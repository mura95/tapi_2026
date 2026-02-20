using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionAnimation : StateMachineBehaviour
{
    [SerializeField]
    private float _timeUntilBored;

    [SerializeField]
    private int _numberOfBoredAnimations;

    private bool _isBored;
    private float _idleTime;
    private int _boredAnimation;

    private float _totalTime;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ResetIdle();
        _totalTime = 0;
    }
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _totalTime += Time.deltaTime;

        if (_isBored == false)
        {
            _idleTime += Time.deltaTime;

            if (_idleTime > _timeUntilBored && stateInfo.normalizedTime % 1 < 0.02f)
            {
                _isBored = true;
                _boredAnimation = Random.Range(1, _numberOfBoredAnimations + 1);
                _boredAnimation = _boredAnimation * 2 - 1;

                animator.SetFloat("PlayfulAnimation", _boredAnimation - 1);
                if (Random.Range(0, 3) == 0)
                {
                    animator.SetTrigger("LayersBark");
                }
            }
        }
        else if (stateInfo.normalizedTime % 1 > 0.98)
        {
            ResetIdle();
        }

        if (_totalTime > 15f)
        {
            animator.SetBool("ActionBool", false);
            GlobalVariables.CurrentState = PetState.idle;
        }

        animator.SetFloat("PlayfulAnimation", _boredAnimation, 0.2f, Time.deltaTime);
        if (Mathf.Abs(animator.GetFloat("PlayfulAnimation") - _boredAnimation) < 0.01f)
        {
            animator.SetFloat("PlayfulAnimation", _boredAnimation);
        }
    }

    private void ResetIdle()
    {
        if (_isBored)
        {
            _boredAnimation--;
        }

        _isBored = false;
        _idleTime = 0;
    }
}
