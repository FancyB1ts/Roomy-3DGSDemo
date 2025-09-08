using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace LanguageLocalization
{
    /// <summary>
    /// Main language localization source component per-scene.
    /// Written by Matej Vanco in 2018. Updated in 2024.
    /// https://matejvanco.com
    /// </summary>
    [AddComponentMenu("Matej Vanco/Language Localization/Language Localization")]
    public sealed class Localization_SOURCE : MonoBehaviour
    {
        public const string DELIMITER_CATEGORY = ">";   // Defines custom categories in SOURCE
        public const string DELIMITER_KEY = "=";        // Splits key & it's content in text
        public const string NEW_LINE_SYMBOL = "/l";     // Splits new lines

        [SerializeField] private TextAsset[] languageFiles;
        [Min(0)] public int selectedLanguage = 0;

        [SerializeField] private bool loadLanguageOnStart = true;

        public event Action OnLanguageLoaded;

        public TextAsset[] LoadedLanguageFiles => languageFiles;

        [Serializable]
        public class LocalizationSelector
        {
            public enum AssignationType : int { None, GameObjectChild, LocalizationComponent, SpecificTextObjects };
            /// <summary>
            /// Assignation type helps you to automatize text translation that indicates to the specific physical or abstract object
            /// </summary>
            public AssignationType assignationType;

            //Essentials
            public string Key;
            public string Text;
            public int Category;

            //AT - GameObjectChild
            public bool AT_FindChildByKeyName = true;
            public string AT_ChildName;
            public bool AT_UseGeneralChildsRootObject = true;
            public Transform AT_CustomChildsRootObject;

            //AT - Allowed Types
            public bool AT_UITextComponentAllowed = true;
            public bool AT_TextMeshComponentAllowed = true;
            public bool AT_TextMeshProComponentAllowed = true;

            //AT - Available Objects
            public List<GameObject> AT_FoundObjects = new List<GameObject>();

            //AT - Specific_UIText
            public Text[] AT_UITextObject;
            //AT - Specific_TextMesh
            public TextMesh[] AT_TextMeshObject;
            //AT - Specific_TextMeshPro
            public TextMeshProUGUI[] AT_TextMeshProObject;
        }
        public List<LocalizationSelector> localizationSelector = new List<LocalizationSelector>();
        public Transform gameObjectChildsRoot;

        public int selectedCategory = 0;

        public List<string> loadedCategories = new List<string>();

        [Serializable]
        public sealed class QuickActions
        {
            public LocalizationSelector.AssignationType assignationType;
            [Tooltip("If assignation type is set to GameObjectChild & the bool is set to True, the target text will be searched in the global Childs root object")]
            public bool useGeneralChildsRoot = true;
            [Space]
            [Tooltip("Allow UIText component?")] public bool UITextAllowed = true;
            [Tooltip("Allow TextMesh component?")] public bool TextMeshAllowed = true;
            [Tooltip("Allow TextMeshPro component?")] public bool TextMeshProAllowed = true;
            [Space]
            [Tooltip("New specific UI Texts")] public Text[] SpecificUITexts;
            [Tooltip("New specific Text Meshes")] public TextMesh[] SpecificTextMeshes;
            [Tooltip("New specific Text Pro Meshes")] public TextMeshProUGUI[] SpecificTextProMeshes;
            [Tooltip("If enabled, the object fields above will be cleared if the QuickActions are applied to key in specific category")] public bool ClearAllPreviousTargets = true;
        }

        /// <summary>
        /// Quick actions allow user to manipulate with exist keys much faster!
        /// </summary>
        public QuickActions quickActions;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            RefreshTextElementsAndKeys();
            if (loadLanguageOnStart)
                LoadLanguage(selectedLanguage);
        }

        #region Internal Methods

#if UNITY_EDITOR

        internal void InternalEditor_RefreshInternalLocalization()
        {
            loadedCategories.Clear();
            loadedCategories.AddRange(Localization_SOURCE_Window.locAvailableCategories);
        }

        internal void InternalEditor_AddKey(string KeyName)
        {
            foreach (Localization_SOURCE_Window.LocalizationElement a in Localization_SOURCE_Window.localizationElements)
            {
                if (a.key == KeyName)
                {
                    localizationSelector.Add(new LocalizationSelector() { Key = a.key, Text = a.text, Category = a.categoryIndex });
                    return;
                }
            }
        }

        /// <summary>
        /// Automatically sync all keys from the localization manager
        /// </summary>
        public void AutoSyncAllKeys()
        {
            if (!Application.isEditor) return;
            
            // Refresh the localization window data first
            Localization_SOURCE_Window.RefreshWindowIfInitialized();
            
            // Clear existing keys
            localizationSelector.Clear();
            
            // Add all keys from the localization manager
            foreach (Localization_SOURCE_Window.LocalizationElement element in Localization_SOURCE_Window.localizationElements)
            {
                localizationSelector.Add(new LocalizationSelector() 
                { 
                    Key = element.key, 
                    Text = element.text, 
                    Category = element.categoryIndex,
                    assignationType = LocalizationSelector.AssignationType.LocalizationComponent // Default to LocalizationComponent
                });
            }
            
            // Refresh the loaded categories
            InternalEditor_RefreshInternalLocalization();
            
            UnityEditor.EditorUtility.SetDirty(this);
            
            Debug.Log($"Auto-synced {localizationSelector.Count} keys from localization manager");
        }

#endif

        private string Internal_ConvertAndReturnText(LocalizationSelector lSelector, string[] lines)
        {
            if (lines.Length > 1)
            {
                List<string> storedFilelines = new List<string>();
                for (int i = 1; i < lines.Length; i++)
                    storedFilelines.Add(lines[i]);

                foreach (string categories in loadedCategories)
                {
                    if (Internal_GetLocalizationCategory(categories) == lSelector.Category)
                    {
                        foreach (string s in storedFilelines)
                        {
                            if (string.IsNullOrEmpty(s))
                                continue;
                            if (s.StartsWith(DELIMITER_CATEGORY))
                                continue;
                            int del = s.IndexOf(DELIMITER_KEY);
                            if (del == 0 || del > s.Length) continue;

                            string Key = s.Substring(0, del);

                            if (string.IsNullOrEmpty(Key)) continue;
                            if (Key == lSelector.Key)
                            {
                                if (s.Length < Key.Length + 1)
                                    continue;
                                lSelector.Text = s.Substring(Key.Length + 1, s.Length - Key.Length - 1);
                                return lSelector.Text;
                            }
                        }
                    }
                }
            }
            return "";
        }

        internal int Internal_GetLocalizationCategory(string entry)
        {
            int c = 0;
            foreach (string categ in loadedCategories)
            {
                if (categ == entry) return c;
                c++;
            }
            return 0;
        }

        #endregion

        /// <summary>
        /// Refresh all resource texts by the selected options
        /// </summary>
        public void RefreshTextElementsAndKeys()
        {
            foreach (LocalizationSelector sel in localizationSelector)
            {
                switch (sel.assignationType)
                {
                    case LocalizationSelector.AssignationType.GameObjectChild:
                        string childName = sel.AT_ChildName;
                        if (sel.AT_FindChildByKeyName)
                            childName = sel.Key;

                        sel.AT_FoundObjects.Clear();
                        Transform root = sel.AT_UseGeneralChildsRootObject ? gameObjectChildsRoot : sel.AT_CustomChildsRootObject;
                        if (!root)
                        {
                            Debug.LogError("Localization: The key '" + sel.Key + "' should have been assigned to specific childs by it's key name, but the root object is empty");
                            return;
                        }

                        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name != childName) continue;

                            if (sel.AT_UITextComponentAllowed && t.GetComponent<Text>())
                                sel.AT_FoundObjects.Add(t.gameObject);
                            if (sel.AT_TextMeshComponentAllowed && t.GetComponent<TextMesh>())
                                sel.AT_FoundObjects.Add(t.gameObject);
                            if (sel.AT_TextMeshProComponentAllowed && t.GetComponent<TextMeshProUGUI>())
                                sel.AT_FoundObjects.Add(t.gameObject);
                        }
                        break;

                    case LocalizationSelector.AssignationType.LocalizationComponent:
                        sel.AT_FoundObjects.Clear();
                        foreach (Localization_KEY k in FindObjectsOfType<Localization_KEY>())
                        {
                            if (k.keyID == sel.Key) sel.AT_FoundObjects.Add(k.gameObject);
                        }
                        break;
                }
                if (sel.assignationType == LocalizationSelector.AssignationType.GameObjectChild || sel.assignationType == LocalizationSelector.AssignationType.LocalizationComponent)
                {
                    if (sel.AT_FoundObjects.Count == 0)
                        {
                        // Debug.Log("Localization: The key '" + sel.Key + "' couldn't find any child objects");
                        }                
                }
            }
        }

        /// <summary>
        ///  Load a language database by the given language index
        /// </summary>
        public void LoadLanguage(int languageIndex)
        {
            if (languageFiles.Length <= languageIndex)
            {
                Debug.LogError("Localization: The index for language selection is incorrect! Languages count: " + (languageFiles.Length - 1) + ", Your index: " + languageIndex);
                return;
            }
            else if (languageFiles[languageIndex] == null)
            {
                Debug.LogError("Localization: The language that you've selected is empty!");
                return;
            }

            foreach (LocalizationSelector sel in localizationSelector)
            {
                sel.Text = Internal_ConvertAndReturnText(sel, languageFiles[languageIndex].text.Split('\n')).Replace(NEW_LINE_SYMBOL, System.Environment.NewLine);

                switch (sel.assignationType)
                {
                    case LocalizationSelector.AssignationType.LocalizationComponent:
                    case LocalizationSelector.AssignationType.GameObjectChild:
                        foreach (GameObject gm in sel.AT_FoundObjects)
                        {
                            if (sel.AT_UITextComponentAllowed && gm.GetComponent<Text>())
                                gm.GetComponent<Text>().text = sel.Text;
                            else if (sel.AT_TextMeshComponentAllowed && gm.GetComponent<TextMesh>())
                                gm.GetComponent<TextMesh>().text = sel.Text;
                            else if (sel.AT_TextMeshProComponentAllowed && gm.GetComponent<TextMeshProUGUI>())
                                gm.GetComponent<TextMeshProUGUI>().text = sel.Text;
                        }
                        break;

                    case LocalizationSelector.AssignationType.SpecificTextObjects:
                        foreach (TextMesh t in sel.AT_TextMeshObject)
                        {
                            if (t != null)
                                t.text = sel.Text;
                        }
                        foreach (Text t in sel.AT_UITextObject)
                        {
                            if (t != null)
                                t.text = sel.Text;
                        }
                        foreach (TextMeshProUGUI t in sel.AT_TextMeshProObject)
                        {
                            if (t != null)
                                t.text = sel.Text;
                        }
                        break;
                }
            }

            if (Application.isPlaying)
                OnLanguageLoaded?.Invoke();
#if UNITY_EDITOR
            else
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Return existing text by the specific key input in the currently selected language
        /// </summary>
        public string ReturnText(string keyInput)
        {
            foreach (LocalizationSelector l in localizationSelector)
                if (l.Key == keyInput)
                    return l.Text;

            Debug.Log("Localization: Key '" + keyInput + "' couldn't be found");

            return "";
        }

        /// <summary>
        /// Load language from the defined index in the inspector
        /// </summary>
        [ContextMenu("Load Language")]
        public void LoadLanguageByDefaultIndex()
        {
            RefreshTextElementsAndKeys();
            LoadLanguage(selectedLanguage);
        }
    }
}