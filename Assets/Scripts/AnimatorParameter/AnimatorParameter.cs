using UnityEngine;

public class TriggerParameter : AnimatorParameterBase
{
    public TriggerParameter(string parameterName) : base(parameterName) { }

    public override void Apply(Animator animator)
    {
        if (!string.IsNullOrEmpty(ParameterName))
        {
            animator.SetTrigger(ParameterName);
        }
    }
}

public class IntParameter : AnimatorParameterBase
{
    public int Value;

    public IntParameter(string parameterName, int value) : base(parameterName)
    {
        Value = value;
    }

    public override void Apply(Animator animator)
    {
        if (!string.IsNullOrEmpty(ParameterName))
        {
            animator.SetInteger(ParameterName, Value);
        }
    }
}

public class BoolParameter : AnimatorParameterBase
{
    public bool Value;

    public BoolParameter(string parameterName, bool value) : base(parameterName)
    {
        Value = value;
    }

    public override void Apply(Animator animator)
    {
        if (!string.IsNullOrEmpty(ParameterName))
        {
            animator.SetBool(ParameterName, Value);
        }
    }
}

public class FloatParameter : AnimatorParameterBase
{
    public float Value;

    public FloatParameter(string parameterName, float value) : base(parameterName)
    {
        Value = value;
    }

    public override void Apply(Animator animator)
    {
        if (!string.IsNullOrEmpty(ParameterName))
        {
            animator.SetFloat(ParameterName, Value);
        }
    }
}