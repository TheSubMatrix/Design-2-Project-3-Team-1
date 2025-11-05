using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using AudioSystem;
using UnityEngine.Serialization;

public class ButtonClickArrow : MonoBehaviour
{
    [Header("References")]
    public RectTransform m_arrow;       
    public SoundData m_clickSound; 

    [FormerlySerializedAs("offsetX")] [Header("Settings")]
    public float m_offsetX = 50f;       
    [FormerlySerializedAs("speed")] public float m_speed = 1400f;       

    Coroutine m_moveRoutine;

    public void MoveArrowToButton(RectTransform target, System.Action onComplete)
    {
        SoundManager.Instance.CreateSound().WithSoundData(m_clickSound).WithRandomPitch().Play();
        if (m_moveRoutine != null) StopCoroutine(m_moveRoutine);
        m_moveRoutine = StartCoroutine(MoveArrowRoutine(target, onComplete));
    }

    IEnumerator MoveArrowRoutine(RectTransform target, System.Action onComplete)
    {
        if (!m_arrow || !target) yield break;

        m_arrow.gameObject.SetActive(true);
        Canvas canvas = m_arrow.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 targetScreenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_arrow.parent as RectTransform, 
            targetScreenPoint, 
            cam, 
            out Vector2 targetLocal
        );
        
        targetLocal.x -= m_offsetX;

        // Calculate canvas width for off-screen start position
        float canvasWidth;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvasWidth = Screen.width / canvas.scaleFactor;
        }
        else
        {
            canvasWidth = canvasRect.rect.width;
        }

        // Start position (off-screen to the left)
        Vector2 startLocal = targetLocal;
        startLocal.x -= canvasWidth;

        m_arrow.localPosition = startLocal;
        yield return null;

        float distance = Vector2.Distance(startLocal, targetLocal);
        float duration = Mathf.Max(0.01f, distance / m_speed);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            m_arrow.localPosition = Vector2.Lerp(startLocal, targetLocal, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        m_arrow.localPosition = targetLocal;
        onComplete?.Invoke();
    }
}