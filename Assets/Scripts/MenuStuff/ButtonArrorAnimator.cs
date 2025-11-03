using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class ButtonClickArrow : MonoBehaviour
{
    [Header("References")]
    public RectTransform arrow;       
    public AudioSource audioSource;   
    public AudioClip clickSound;      

    [Header("Settings")]
    public float offsetX = 50f;       
    public float speed = 1400f;       

    Coroutine moveRoutine;

   
    public void MoveArrowToButton(RectTransform target, System.Action onComplete)
    {
        if (clickSound && audioSource) audioSource.PlayOneShot(clickSound);

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveArrowRoutine(target, onComplete));
    }

    IEnumerator MoveArrowRoutine(RectTransform target, System.Action onComplete)
    {
        if (!arrow) yield break;

        arrow.gameObject.SetActive(true);

       
        Vector3 targetPos = target.position + new Vector3(-offsetX, 0f, 0f);

        Vector3 startPos = new Vector3(targetPos.x - Screen.width, targetPos.y, targetPos.z);
        arrow.position = startPos;
        yield return null;

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = Mathf.Max(0.01f, distance / speed);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            arrow.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        arrow.position = targetPos;
        onComplete?.Invoke();
    }
}