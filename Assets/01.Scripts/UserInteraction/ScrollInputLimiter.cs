using UnityEngine;

/// <summary>
/// Limits scroll input for 3D scene interactions (camera zoom, furniture rotation)
/// Uses cooldown + direction-only approach for consistent behavior across all devices
/// </summary>
public static class ScrollInputLimiter
{
    public static float scrollCooldown = 0.075f; // 13 scrolls per second max
    
    private static float lastScrollTime = -999f;
    
    /// <summary>
    /// Gets scroll direction with rate limiting. Returns -1, 0, or 1.
    /// </summary>
    /// <returns>Scroll direction: -1 (down), 0 (none), 1 (up)</returns>
    public static int GetScrollDirection()
    {
        // Rate limiting: prevent rapid-fire scroll events
        var currentTime = Time.unscaledTime;
        if (currentTime - lastScrollTime < scrollCooldown)
            return 0; // Reject this scroll event
            
        // Get raw scroll input
        float rawScroll = Input.mouseScrollDelta.y;
        
        // Check if there's meaningful scroll input
        if (Mathf.Abs(rawScroll) < 0.01f)
            return 0; // No scroll detected
            
        // Update last scroll time (we're accepting this event)
        lastScrollTime = currentTime;
        
        // Return direction only (ignore magnitude completely)
        return (int)Mathf.Sign(rawScroll);
    }
    
    /// <summary>
    /// Checks if there's any scroll input (respects cooldown)
    /// </summary>
    /// <returns>True if scroll input is available</returns>
    public static bool HasScrollInput()
    {
        return GetScrollDirection() != 0;
    }
    
    /// <summary>
    /// Gets raw scroll value for debugging purposes
    /// </summary>
    /// <returns>Raw Input.mouseScrollDelta.y value</returns>
    public static float GetRawScrollDelta()
    {
        return Input.mouseScrollDelta.y;
    }
}