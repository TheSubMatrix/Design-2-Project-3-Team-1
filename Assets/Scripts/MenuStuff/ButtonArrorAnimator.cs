using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using AudioSystem;

public class ButtonClickArrow : MonoBehaviour
{
    [Header("References")]
    public Image m_arrowImage;

    [Header("Audio")]
    public SoundData m_clickSound; 

    [Header("Settings")]
    [Tooltip("Additional spacing between arrow tip and button (in Pixels)")]
    public float m_additionalOffset = 10f;
    [Tooltip("Speed in Pixels per second")]
    public float m_speed = 2500f; 
    [Tooltip("Extra distance to start off-screen (in Pixels)")]
    public float m_extraOffscreenDistance = 100f;

    RectTransform m_rectTransform;
    Coroutine m_moveRoutine;

    void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        
        if (m_rectTransform == null)
            Debug.LogError("ButtonClickArrow: Missing RectTransform!");

        if (m_arrowImage == null)
            m_arrowImage = GetComponent<Image>();
    }

    void Start()
    {
        if (m_arrowImage != null) m_arrowImage.enabled = false;
    }

    public void MoveToButton(RectTransform target, Action onComplete = null)
    {
        if (m_clickSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.CreateSound()
                .WithSoundData(m_clickSound)
                .WithRandomPitch()
                .Play();
        }

        if (m_moveRoutine != null) StopCoroutine(m_moveRoutine);
        m_moveRoutine = StartCoroutine(MoveArrowRoutine(target, onComplete));
    }

    IEnumerator MoveArrowRoutine(RectTransform target, Action onComplete)
    {
        if (!m_rectTransform || !target || !m_arrowImage)
            yield break;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas && rootCanvas.rootCanvas)
            rootCanvas = rootCanvas.rootCanvas;
        m_arrowImage.enabled = true;
        float scaleFactor = m_rectTransform.lossyScale.x;
        if (scaleFactor == 0) scaleFactor = 1f;

        float worldOffset = m_additionalOffset * scaleFactor;
        float worldSpeed = m_speed * scaleFactor;
        float worldExtraDist = m_extraOffscreenDistance * scaleFactor;

        Vector3[] targetCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners);
        Vector3 targetLeftEdgeWorld = (targetCorners[0] + targetCorners[1]) * 0.5f;

        float localTipX = m_rectTransform.rect.width * (1f - m_rectTransform.pivot.x);
        float localTipY = m_rectTransform.rect.height * (0.5f - m_rectTransform.pivot.y);
        Vector3 localTipPos = new Vector3(localTipX, localTipY, 0f);

        Vector3 pivotWorldPos = m_rectTransform.position;
        Vector3 tipWorldPos = m_rectTransform.TransformPoint(localTipPos);
        Vector3 tipOffsetWorld = tipWorldPos - pivotWorldPos;

        Vector3 canvasRight = Vector3.right;
        float canvasWidthWorld = 1920f * scaleFactor; // Fallback

        if (rootCanvas)
        {
            Transform ct = rootCanvas.transform;
            canvasRight = ct.right.normalized; // Direction of "Right" in world space
            
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect)
            {
                // rect.width is pixels, multiply by scale to get World Width
                canvasWidthWorld = canvasRect.rect.width * canvasRect.lossyScale.x;
            }
        }

        // Apply the calculated position
        Vector3 finalWorldPos = targetLeftEdgeWorld 
                                - tipOffsetWorld 
                                - (canvasRight * worldOffset); // Use scaled offset
        
        float startDist = (canvasWidthWorld / 1.5f) + worldExtraDist; // Use scaled extra distance
        Vector3 startWorldPos = finalWorldPos - (canvasRight * startDist);

        // Match Z depth to ensure no clipping behind background
        startWorldPos.z = finalWorldPos.z;

        // Snap to start
        m_rectTransform.position = startWorldPos;

        yield return null;

        // --- 3. ANIMATE ---

        const float closeEnough = 0.05f; // Tolerance
        
        // Loop until close
        while (Vector3.Distance(m_rectTransform.position, finalWorldPos) > closeEnough)
        {
            m_rectTransform.position = Vector3.MoveTowards(
                m_rectTransform.position,
                finalWorldPos,
                worldSpeed * Time.deltaTime // Use scaled speed
            );

            yield return null;
        }

        // Snap exactly to final
        m_rectTransform.position = finalWorldPos;

        onComplete?.Invoke();
        m_moveRoutine = null;
    }
}