using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DogAnimationGroupBase : ScriptableObject
{
    [Serializable]
    public struct RangeFloat
    {
        public float minValue;
        public float maxValue;
    }
    
    [Serializable]
    public class DogAnimationOption
    {
        public string stateName;
        public int animationType;        
        public AudioClip audioClip;
        public EmotionState emotionState;
    }
    
    [SerializeField]
    private RangeFloat waitLoopTime;
    public float WaitLoopTime => UnityEngine.Random.Range(waitLoopTime.minValue, waitLoopTime.maxValue);
    
    public string transitionAnimationTypeName;
    public List<string> loopAnimationStateFullPaths;
    public List<DogAnimationOption> oneShotAnimations; // 単発アニメーション
    public List<DogAnimationOption> loopAnimations; // ループアニメーション
    
    
    public DogAnimationOption GetRandomOneShotAnimation()
    {
        var randomIndex = UnityEngine.Random.Range(0, oneShotAnimations.Count);
        return oneShotAnimations[randomIndex];
    }

    public DogAnimationOption GetRandomOneShotAnimationByEmotion(EmotionState emotionState)
    {
        List<DogAnimationOption> matchingAnimations; 
        
        if (emotionState == EmotionState.None)
        {
            matchingAnimations = oneShotAnimations.Where(animation => animation.emotionState == emotionState).ToList();
        }
        else
        {
            matchingAnimations = oneShotAnimations.Where(animation => (animation.emotionState & emotionState) != 0).ToList();
        }
        
        if (matchingAnimations.Count == 0)
        {
            return null;
        }

        var randomIndex = UnityEngine.Random.Range(0, matchingAnimations.Count);
        return matchingAnimations[randomIndex];
    }
    
    public DogAnimationOption GetRandomLoopAnimation()
    {
        var randomIndex = UnityEngine.Random.Range(0, loopAnimations.Count);
        return loopAnimations[randomIndex];
    }
    
}
