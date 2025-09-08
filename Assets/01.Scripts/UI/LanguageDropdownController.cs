using UnityEngine;
using TMPro;
using LanguageLocalization;

public class LanguageDropdownController : MonoBehaviour
{
    [Header("Localization Settings")]
    public Localization_SOURCE[] targets;
    public TMP_Dropdown dropdown;
    
    [Header("Advanced Settings")]
    [Tooltip("If true, will also update inactive UI objects when language changes")]
    public bool updateInactiveObjects = true;

    private void Start()
    {
        // Initialize dropdown with current language
        if (targets.Length > 0 && targets[0] != null)
        {
            dropdown.value = targets[0].selectedLanguage;
        }
    }

    public void OnDropdownValueChanged(int index)
    {
        foreach (var target in targets)
        {
            if (target == null) continue;
            
            target.selectedLanguage = index;                      // update internal state
            
            if (updateInactiveObjects)
            {
                // Enhanced refresh that includes inactive objects
                RefreshAllObjectsIncludingInactive(target);
            }
            else
            {
                target.RefreshTextElementsAndKeys();              // standard refresh
            }
            
            target.LoadLanguage(index);                           // load and apply language
        }
    }
    
    private void RefreshAllObjectsIncludingInactive(Localization_SOURCE target)
    {
        // First do the standard refresh for active objects
        target.RefreshTextElementsAndKeys();
        
        // Then manually find and update inactive Localization_KEY objects
        var allLocalizationKeys = Resources.FindObjectsOfTypeAll<Localization_KEY>();
        
        foreach (var key in allLocalizationKeys)
        {
            // Skip objects that aren't in the current scene
            if (key.gameObject.scene.name == null) continue;
            
            // Find matching localization selector
            foreach (var selector in target.localizationSelector)
            {
                if (selector.Key == key.keyID)
                {
                    // Make sure this key is in the found objects list
                    if (!selector.AT_FoundObjects.Contains(key.gameObject))
                    {
                        selector.AT_FoundObjects.Add(key.gameObject);
                    }
                    break;
                }
            }
        }
    }
}