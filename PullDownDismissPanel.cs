using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class PullDownDismissPanel : MonoBehaviour
{
    [SerializeField] private RectTransform dragHandleRect;
    [SerializeField] private GameObject parentToDisable;
    [SerializeField] private float dismissThreshold = 0.3f;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float velocityMultiplier = 0.1f;
    [SerializeField] private bool debugMode = true;

    private RectTransform panelRectTransform;
    private Vector2 originalAnchoredPosition;
    private float panelHeight;
    private bool isFullyShown;
    private bool wasInitiallyVisible;
    private Tween currentAnimation;

    private Vector2 startTouchPosition;
    private Vector2 lastTouchPosition;
    private float lastDragTime;
    private bool isDragging;

    private void Awake()
    {
        InitializePanel();
    }

    private void Start()
    {
        if (wasInitiallyVisible)
        {
            isFullyShown = true;
        }
        else
        {
            PositionPanelOffScreen(false);
        }
    }

    private void OnEnable()
    {
        if (!wasInitiallyVisible && !isFullyShown)
        {
            PositionPanelOffScreen(false);
        }
    }

    private void InitializePanel()
    {
        panelRectTransform = GetComponent<RectTransform>();
        dragHandleRect ??= panelRectTransform;

        originalAnchoredPosition = panelRectTransform.anchoredPosition;
        panelHeight = panelRectTransform.rect.height;
        wasInitiallyVisible = IsFullyVisible();

        EnsureDragHandleHasRaycastTarget();

        Log($"Panel initialized. Height: {panelHeight}px, Original position: {originalAnchoredPosition}, Initially visible: {wasInitiallyVisible}");
    }

    private void EnsureDragHandleHasRaycastTarget()
    {
        if (!dragHandleRect.TryGetComponent(out Image dragHandleImage))
        {
            dragHandleImage = dragHandleRect.gameObject.AddComponent<Image>();
            dragHandleImage.color = Color.clear;
        }
        dragHandleImage.raycastTarget = true;
    }

    private bool IsFullyVisible() => Mathf.Approximately(panelRectTransform.anchoredPosition.y, originalAnchoredPosition.y);

    private void PositionPanelOffScreen(bool animate)
    {
        Vector2 offScreenPosition = new Vector2(originalAnchoredPosition.x, -panelHeight);
        if (animate)
        {
            AnimatePanelPosition(offScreenPosition);
        }
        else
        {
            SetPanelPosition(offScreenPosition);
        }
        Log($"Panel positioned off-screen at {offScreenPosition}");
    }

    private void SetPanelPosition(Vector2 position)
    {
        panelRectTransform.anchoredPosition = position;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);
    }

    private void AnimatePanelPosition(Vector2 targetPosition)
    {
        KillCurrentAnimation();
        currentAnimation = panelRectTransform.DOAnchorPos(targetPosition, animationDuration)
            .SetEase(Ease.OutQuint)
            .OnUpdate(() => LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform))
            .OnComplete(() => SetPanelPosition(targetPosition));
    }

    private void Update()
    {
        if (!isFullyShown) return;

        if (Input.touchCount > 0)
        {
            HandleTouch(Input.GetTouch(0));
        }
    }

    private void HandleTouch(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (IsTouchOverDragHandle(touch.position))
                {
                    StartDrag(touch.position);
                }
                break;
            case TouchPhase.Moved:
                if (isDragging)
                {
                    ContinueDrag(touch.position);
                }
                break;
            case TouchPhase.Ended:
                if (isDragging)
                {
                    EndDrag(touch.position);
                }
                break;
        }
    }

    private bool IsTouchOverDragHandle(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(dragHandleRect, screenPosition, null)
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(dragHandleRect, screenPosition, null, out Vector2 localPoint)
            && dragHandleRect.rect.Contains(localPoint);
    }

    private void StartDrag(Vector2 position)
    {
        startTouchPosition = lastTouchPosition = position;
        lastDragTime = Time.time;
        isDragging = true;
        KillCurrentAnimation();
    }

    private void ContinueDrag(Vector2 position)
    {
        float deltaY = position.y - lastTouchPosition.y;
        if (deltaY < 0)
        {
            UpdatePanelPosition(deltaY);
        }
        lastTouchPosition = position;
        lastDragTime = Time.time;
    }

    private void EndDrag(Vector2 position)
    {
        isDragging = false;
        float finalDeltaY = position.y - startTouchPosition.y;
        float velocity = (position.y - lastTouchPosition.y) / (Time.time - lastDragTime);

        Log($"Drag ended. Delta Y: {finalDeltaY}px, Velocity: {velocity}");

        if (finalDeltaY < 0 && Mathf.Abs(finalDeltaY) > panelHeight * dismissThreshold)
        {
            DismissPanel(velocity);
        }
        else
        {
            ResetPanel();
        }
    }

    private void UpdatePanelPosition(float deltaY)
    {
        Vector2 newPosition = panelRectTransform.anchoredPosition + new Vector2(0, deltaY);
        newPosition.y = Mathf.Clamp(newPosition.y, originalAnchoredPosition.y - panelHeight, originalAnchoredPosition.y);
        SetPanelPosition(newPosition);
        Log($"Panel updated. New Y position: {newPosition.y}");
    }

    private void DismissPanel(float velocity)
    {
        Log($"Dismissing panel with velocity: {velocity}");
        KillCurrentAnimation();

        float offScreenY = originalAnchoredPosition.y - panelHeight;
        float currentY = panelRectTransform.anchoredPosition.y;
        float distance = offScreenY - currentY;

        // Use the drag velocity to determine the initial speed of the dismissal
        float initialSpeed = Mathf.Abs(velocity);

        // Calculate base duration
        float baseDuration = distance / initialSpeed;

        // Adjust duration to create an accelerating effect
        float duration = baseDuration * 0.7f; // Reduce duration to create acceleration effect

        // Clamp duration to ensure it's neither too short nor too long
        duration = Mathf.Clamp(duration, 0.2f, 0.5f);

        isFullyShown = false;
        currentAnimation = panelRectTransform.DOAnchorPosY(offScreenY, duration)
            .SetEase(Ease.InQuad) // Use InQuad for an accelerating motion
            .OnUpdate(() => {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);
                // Optional: Add extra acceleration
                float progress = currentAnimation.ElapsedPercentage();
                if (progress > 0.8f)
                {
                    currentAnimation.timeScale = 1 + (progress - 0.5f) * 2;
                }
                if(progress > 0.9f)
                {
                    currentAnimation.Complete();
                }
            })
            .OnComplete(() => {
                SetPanelPosition(new Vector2(panelRectTransform.anchoredPosition.x, offScreenY));
                Log("Panel dismissed");
                DisableParentImmediate();
            });
    }

    private void ResetPanel()
    {
        Log("Resetting panel");
        KillCurrentAnimation();

        currentAnimation = panelRectTransform.DOAnchorPosY(originalAnchoredPosition.y, 0.2f)
            .SetEase(Ease.OutQuint)
            .OnUpdate(() => LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform))
            .OnComplete(() => {
                SetPanelPosition(new Vector2(panelRectTransform.anchoredPosition.x, originalAnchoredPosition.y));
                isFullyShown = true;
                Log("Panel reset complete");
            });
    }

    public void ShowPanel()
    {
        if (isFullyShown)
        {
            Log("Panel is already fully shown. Ignoring show request.");
            return;
        }

        Log("Show panel requested");
        KillCurrentAnimation();

        gameObject.SetActive(true);

        float currentY = panelRectTransform.anchoredPosition.y;
        float targetY = originalAnchoredPosition.y;
        float distance = Mathf.Abs(targetY - currentY);
        float duration = animationDuration * (distance / panelHeight);

        currentAnimation = panelRectTransform.DOAnchorPosY(targetY, duration)
            .SetEase(Ease.OutQuint)
            .OnUpdate(() => LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform))
            .OnComplete(() => {
                SetPanelPosition(new Vector2(panelRectTransform.anchoredPosition.x, targetY));
                isFullyShown = true;
                Log("Panel show animation complete");
            });
    }

    private void KillCurrentAnimation()
    {
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }

    private void DisableParentImmediate()
    {
        if (parentToDisable != null)
        {
            parentToDisable.SetActive(false);
            Log($"Disabled parent object: {parentToDisable.name}");
        }
    }

    private void OnDisable()
    {
        KillCurrentAnimation();
        isFullyShown = false;
    }

    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[PullDownDismissPanel] {message}");
        }
    }
}
