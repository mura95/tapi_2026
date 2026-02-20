using System;
using System.Collections;
using UnityEngine;

public class IdleManager : MonoBehaviour
{
    [SerializeField] private DogController _dogController;
    private int transitionNo = 1;
    // デフォルトの確率配列
    private float[] defaultTransitionProbabilities = new float[] { 0.3f, 0.3f, 0.3f, 0.1f };
    // 特定条件下の確率配列
    private float[] alternateTransitionProbabilities = new float[] { 0.1f, 0.4f, 0.4f, 0.1f };
    // 現在の確率配列
    private float[] transitionProbabilities;
    void Start()
    {
        transitionProbabilities = defaultTransitionProbabilities;
        StartCoroutine(ChangeTransitionState());
    }

    IEnumerator ChangeTransitionState()
    {
        while (true)
        {
            float randomInterval = UnityEngine.Random.Range(10f, 50f);
            yield return new WaitForSeconds(randomInterval);

            // 犬の位置が (0, 0, 0) の範囲内にいない場合は確率配列を切り替え
            if (_dogController.transform.position != Vector3.zero && _dogController.transform.position.magnitude > 0.5f)
            {
                transitionProbabilities = alternateTransitionProbabilities;
            }
            else
            {
                transitionProbabilities = defaultTransitionProbabilities;
            }

            if (_dogController != null && _dogController.GetTransitionNo() != 3 && _dogController.GetTransitionNo() != 4)
            {
                transitionNo = GetWeightedRandomTransitionNo(transitionProbabilities);
                _dogController.UpdateTransitionState(transitionNo);
            }
        }
    }
    int GetWeightedRandomTransitionNo(float[] transitionProbabilities)
    {
        float total = 0f;
        foreach (var prob in transitionProbabilities) total += prob;

        if (Mathf.Approximately(total, 0f)) return 0;

        float randomPoint = UnityEngine.Random.value * total;

        for (int i = 0; i < transitionProbabilities.Length; i++)
        {
            if (randomPoint < transitionProbabilities[i])
                return i;
            randomPoint -= transitionProbabilities[i];
        }
        return transitionProbabilities.Length - 1;
    }
}
