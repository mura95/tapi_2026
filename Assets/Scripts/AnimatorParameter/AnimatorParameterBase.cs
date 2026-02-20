using UnityEngine;

public interface IAnimatorParameter
{
    void Apply(Animator animator);
}

public abstract class AnimatorParameterBase : IAnimatorParameter
{
    public string ParameterName;

    protected AnimatorParameterBase(string parameterName)
    {
        ParameterName = parameterName;
    }

    public abstract void Apply(Animator animator);
}