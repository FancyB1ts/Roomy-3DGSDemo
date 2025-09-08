using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System;

/// <summary>
/// Changes the button color when we click it, enter and exit it
/// </summary>
public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private Color highlightColor, clickedColor;

    [SerializeField]
    private float fadeDuration = 0.1f;

    bool selected = false;

    Color defaultColor;

    [SerializeField]
    private bool changeColorOnClock = true;

    [SerializeField]
    private Vector3 scaleUpValue = new Vector3(1.1f, 1.1f, 1.1f);

    private Vector3 originalScale;
    public event Action OnClicked;

    private void Awake()
    {
        button = GetComponent<Button>();
        defaultColor = button.image.color;
        originalScale = button.transform.localScale;
    }

    public void ResetButton()
    {
        selected = false;
        if(button )
        { 
            button.image.color = defaultColor;
            button.transform.localScale = originalScale;
        }
            //button.image.DOColor(defaultColor, fadeDuration);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        button.image.color=  highlightColor;
        transform.DOScale(scaleUpValue, fadeDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selected)
            return;
        button.image.color = defaultColor;
        button.transform.DOScale(originalScale, fadeDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnClicked?.Invoke();
        if (changeColorOnClock == false)
            return;
        ApplyClickedFeedback();

    }

    public void ApplyClickedFeedback()
    {
        if (button)
        {
            button.image.color = clickedColor;
            button.transform.DOScale(originalScale, fadeDuration);
        }
        selected = true;
    }

    private void OnDisable()
    {
        if (button == null)
            return;

        button.image.DOComplete();
        button.transform.DOComplete();
        button.image.color = defaultColor;
        button.transform.localScale = originalScale;
    }
}
