using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Limits scroll input for UI ScrollRect components
/// Uses cooldown + direction-only approach for consistent behavior across all devices
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class UIScrollInputLimiter : MonoBehaviour, IScrollHandler
{
    [Header("Scroll Limiting Settings")]
    [SerializeField] private float scrollCooldown = 0.02f; // 50 scrolls per second max for UI
    [SerializeField] private float scrollMultiplier = 10f; // How much to scroll per step
    
    private float lastScrollTime = -999f;
    private ScrollRect scrollRect;
    
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }
    
    public void OnScroll(PointerEventData eventData)
    {
        // Rate limiting: prevent rapid-fire scroll events
        var currentTime = Time.unscaledTime;
        if (currentTime - lastScrollTime < scrollCooldown)
            return; // Reject this scroll event
            
        // Get raw scroll input
        var rawDelta = eventData.scrollDelta;
        
        // Check if there's meaningful scroll input
        if (Mathf.Abs(rawDelta.y) < 0.01f)
            return; // No scroll detected
            
        // Update last scroll time (we're accepting this event)
        lastScrollTime = currentTime;
        
        // Calculate scroll direction and apply fixed amount
        var scrollDirection = Mathf.Sign(rawDelta.y);
        var scrollAmount = scrollDirection * scrollMultiplier * Time.unscaledDeltaTime;
        
        // Apply scroll to ScrollRect
        if (scrollRect.vertical)
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                scrollRect.verticalNormalizedPosition + scrollAmount
            );
        }
        else if (scrollRect.horizontal)
        {
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(
                scrollRect.horizontalNormalizedPosition + scrollAmount
            );
        }
    }
    
    /// <summary>
    /// Adjust scroll settings at runtime
    /// </summary>
    public void SetScrollSettings(float cooldown, float multiplier)
    {
        scrollCooldown = cooldown;
        scrollMultiplier = multiplier;
    }
}