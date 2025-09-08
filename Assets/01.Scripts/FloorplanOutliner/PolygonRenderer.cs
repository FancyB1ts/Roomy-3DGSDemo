using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Radishmouse;

public class PolygonRenderer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color fillColor = new Color(0.3f, 0.5f, 1f, 0.7f);
    
    [Header("Required Components")]
    
    // UI-compatible fill rendering using Image with generated sprite
    private GameObject fillObject;
    private Image fillImage;
    
    void Start()
    {
        // CRITICAL: Missing RectTransform setup from original
        SetupFillImage();
        SetActive(false);
    }
    
    // MISSING: Critical RectTransform setup from original
    private void SetupRectTransform(RectTransform rect)
    {
        if (rect == null) return;
        RectTransform parent = GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.anchorMin = parent.anchorMin;
        rect.anchorMax = parent.anchorMax;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = parent.pivot;
    }
    
    private void SetupFillImage()
    {
        // Create a new GameObject for the fill
        fillObject = new GameObject("PolygonFill");
        fillObject.transform.SetParent(transform, false);
        
        // Add Image component for UI-compatible rendering
        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        
        // Setup RectTransform to match parent
        RectTransform fillRect = fillImage.rectTransform;
        SetupRectTransform(fillRect);
        
        // Position fill behind the line
        fillImage.transform.SetSiblingIndex(0);
        
        fillImage.enabled = false;
    }

    // BUG FIX: Method signature was wrong - should match original exactly
    // BUG FIX: Method signature was wrong - should match original exactly
    public void UpdatePolygon(List<Vector2> points, bool isComplete)
    {
        if (points == null || points.Count == 0)
        {
            Clear();
            return;
        }

        // Only handle fill rendering - PolygonDrawer handles line rendering
        if (isComplete && points.Count >= 3)
        {
            // Update fill
            UpdateFill(points);
        }
        else
        {
            // Hide fill for incomplete polygons
            if (fillImage != null)
                fillImage.enabled = false;
        }

        SetActive(true); // Only affects fill rendering now
    }
    
    private void UpdateFill(List<Vector2> points)
    {
        if (points.Count < 3 || fillImage == null) return;
        
        // Clean up previous sprite
        if (fillImage.sprite != null)
        {
            DestroyImmediate(fillImage.sprite.texture);
            DestroyImmediate(fillImage.sprite);
            fillImage.sprite = null;
        }
        
        // Generate a sprite that fills the polygon
        Sprite fillSprite = GeneratePolygonSprite(points);
        
        if (fillSprite != null)
        {
            fillImage.sprite = fillSprite;
            fillImage.enabled = true;
        }
    }
    
    private Sprite GeneratePolygonSprite(List<Vector2> points)
    {
        // Get the bounds of our polygon
        RectTransform rect = GetComponent<RectTransform>();
        Rect bounds = rect.rect;
        
        int textureWidth = Mathf.RoundToInt(bounds.width);
        int textureHeight = Mathf.RoundToInt(bounds.height);
        
        // Ensure minimum texture size
        textureWidth = Mathf.Max(textureWidth, 64);
        textureHeight = Mathf.Max(textureHeight, 64);
        
        // Create texture
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureWidth * textureHeight];
        
        // Initialize with transparent pixels
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Convert points to texture space
        List<Vector2> texturePoints = new List<Vector2>();
        foreach (Vector2 point in points)
        {
            // Convert from local rect space to texture space
            float x = (point.x - bounds.x) / bounds.width * textureWidth;
            float y = (point.y - bounds.y) / bounds.height * textureHeight;
            texturePoints.Add(new Vector2(x, y));
        }
        
        // Fill polygon using scanline algorithm
        FillPolygon(pixels, textureWidth, textureHeight, texturePoints, fillColor);
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Create sprite from texture
        return Sprite.Create(
            texture,
            new Rect(0, 0, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
    
    private void FillPolygon(Color[] pixels, int width, int height, List<Vector2> points, Color fillColor)
    {
        if (points.Count < 3) return;
        
        // Scanline polygon fill algorithm
        for (int y = 0; y < height; y++)
        {
            List<float> intersections = new List<float>();
            
            // Find intersections with polygon edges for this scanline
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % points.Count];
                
                // Check if scanline intersects this edge
                if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                {
                    // Calculate intersection x coordinate
                    float intersectionX = p1.x + (y - p1.y) * (p2.x - p1.x) / (p2.y - p1.y);
                    intersections.Add(intersectionX);
                }
            }
            
            // Sort intersections
            intersections.Sort();
            
            // Fill between pairs of intersections
            for (int i = 0; i < intersections.Count; i += 2)
            {
                if (i + 1 < intersections.Count)
                {
                    int startX = Mathf.Max(0, Mathf.RoundToInt(intersections[i]));
                    int endX = Mathf.Min(width - 1, Mathf.RoundToInt(intersections[i + 1]));
                    
                    for (int x = startX; x <= endX; x++)
                    {
                        int pixelIndex = y * width + x;
                        if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                        {
                            pixels[pixelIndex] = fillColor;
                        }
                    }
                }
            }
        }
    }

    public void Clear()
    {
        // Remove lineRenderer code

        if (fillImage != null)
        {
            fillImage.enabled = false;
            if (fillImage.sprite != null)
            {
                DestroyImmediate(fillImage.sprite.texture);
                DestroyImmediate(fillImage.sprite);
                fillImage.sprite = null;
            }
        }

        SetActive(false);
    }
    
    public void SetActive(bool active)
    {     
        if (fillImage != null && fillImage.sprite != null)
            fillImage.enabled = active;
    }
    
    // Debug methods
    [ContextMenu("Clear Renderer")]
    public void Clear_Inspector() => Clear();
    
    [ContextMenu("Debug Renderer State")]
    public void DebugRendererState()
    {
        Debug.Log("=== PolygonRenderer State ===");
        Debug.Log($"fillImage assigned: {fillImage != null}");
        
        if (fillImage != null)
        {
            Debug.Log($"fillImage enabled: {fillImage.enabled}");
            Debug.Log($"fillImage sprite: {fillImage.sprite != null}");
        }
    }
}