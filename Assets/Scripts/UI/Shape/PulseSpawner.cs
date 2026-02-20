using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PulseSpawner : MonoBehaviour
{
    public static PulseSpawner Instance { get; private set; }

    [SerializeField] private RectTransform canvasTransform; // CanvasのRectTransformをInspectorでアタッチ
    [SerializeField] private GameObject pulsePrefab;         // PulseCircleプレハブをInspectorでアタッチ

    private GameObject pulseObject;
    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowPulse(float x, float y, float maxScale, float duration)
    {
        if (pulseObject == null)
        {
            pulseObject = Instantiate(pulsePrefab, canvasTransform);
            pulseObject.transform.SetAsFirstSibling();
            rt = pulseObject.GetComponent<RectTransform>();
            canvasGroup = pulseObject.GetComponent<CanvasGroup>() ?? pulseObject.AddComponent<CanvasGroup>();
        }

        rt.anchoredPosition = new Vector2(x, y);
        pulseObject.SetActive(true);

        if (pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseLoop(maxScale, duration));
    }

    public void HidePulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (pulseObject != null)
            pulseObject.SetActive(false);
    }

    private IEnumerator PulseLoop(float maxScale, float duration)
    {
        while (true)
        {
            float timer = 0f;

            // アニメーション再生
            while (timer < duration)
            {
                float t = timer / duration;
                float scale = Mathf.Lerp(0f, maxScale, t);
                float alpha = Mathf.Lerp(1f, 0f, t);

                if (rt != null) rt.localScale = Vector3.one * scale;
                if (canvasGroup != null) canvasGroup.alpha = alpha;

                timer += Time.deltaTime;
                yield return null;
            }

            // 最後まで終わったあとに完全非表示にして、次のループへ
            if (rt != null) rt.localScale = Vector3.zero;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            // ほんのわずかに間をあけると「ふんわり消えた」感じがより自然に
            yield return new WaitForSeconds(0.1f);
        }
    }

}
