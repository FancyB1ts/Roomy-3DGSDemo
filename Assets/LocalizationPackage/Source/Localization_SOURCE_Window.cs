#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LanguageLocalization
{
    /// <summary>
    /// Main language localization window editor.
    /// Written by Matej Vanco in 2018. Updated in 2024
    /// https://matejvanco.com
    /// </summary>
    public class Localization_SOURCE_Window : EditorWindow
    {
        public static bool locWindowInitialized = false;
        public static readonly List<string> locAvailableCategories = new List<string>();
        public static readonly List<LocalizationElement> localizationElements = new List<LocalizationElement>();

        private const string locRegistryKey = "LOCATION_MANAGER_LocManagPath";
        private const string locHeadingFormat = "Localization_Manager_Source";

        private static string locSelectedPath;
        private static string locCurrentLanguage;
        private static bool locManagerSelected = true;
        private static bool locReadySteady = false;

        private static int locCategorySelected = 0;
        private static string locSelectedCategoryName;
        private static Vector2 guiScrollHelper;

        public sealed class LocalizationElement
        {
            public string key;
            [Multiline] public string text;
            public int categoryIndex;
        }

        public static void RefreshWindowIfInitialized()
        {
            if (locWindowInitialized)
                return;

            locWindowInitialized = true;

            localizationElements.Clear();
            Loc_GetLocalizationManagerPath();
        }

        [MenuItem("Window/Localization Manager")]
        public static void InitWindow()
        {
            var win = GetWindow(typeof(Localization_SOURCE_Window));
            win.minSize = new Vector2(400, 250);
            win.titleContent = new GUIContent("Localization Editor");

            localizationElements.Clear();
            Loc_GetLocalizationManagerPath();
        }

        #region Data Managing

        public static void Loc_GetLocalizationManagerPath()
        {
            locCurrentLanguage = "";

            locSelectedPath = PlayerPrefs.GetString(locRegistryKey);
            locReadySteady = !string.IsNullOrEmpty(locSelectedPath);

            if (!locReadySteady) return;

            locReadySteady = true;
            locManagerSelected = true;

            Loc_RefreshContent();
        }

        private static void Loc_SelectLanguageFile()
        {
            Loc_SaveDatabase(locSelectedPath, false);

            string f = EditorUtility.OpenFilePanel("Select Language File Path", Application.dataPath, "xml");

            if (string.IsNullOrEmpty(f)) return;

            locSelectedPath = f;
            locManagerSelected = false;

            locCurrentLanguage = Path.GetFileNameWithoutExtension(locSelectedPath);

            Loc_LoadDatabase(locSelectedPath, false);
        }

        private static void Loc_CreateLanguageFile()
        {
            Loc_SaveDatabase(locSelectedPath, false);

            string f = EditorUtility.SaveFilePanel("Create Language File", Application.dataPath, "English", "xml");

            if (string.IsNullOrEmpty(f)) return;

            File.Create(f).Dispose();

            locSelectedPath = f;
            locManagerSelected = false;

            locCurrentLanguage = Path.GetFileNameWithoutExtension(locSelectedPath);

            Loc_LoadDatabase(locSelectedPath, false);

            AssetDatabase.Refresh();
        }

        public static void Loc_RefreshContent()
        {
            locAvailableCategories.Clear();
            locAvailableCategories.Add("Default"); // Add default category
            localizationElements.Clear();

            if (!File.Exists(locSelectedPath))
            {
                Loc_ErrorDebug("The selected file path '" + locSelectedPath + "' doesn't exist!");
                PlayerPrefs.DeleteKey(locRegistryKey);
                locReadySteady = false;
                return;
            }

            string[] allLines = File.ReadAllLines(locSelectedPath);
            if (allLines.Length <= 1)
                return;

            for (int i = 1; i < allLines.Length; i++)
            {
                string currentLine = allLines[i];
                if (currentLine.Length <= 1) continue;
                if (currentLine.StartsWith(Localization_SOURCE.DELIMITER_CATEGORY))
                    locAvailableCategories.Add(currentLine.Trim().Remove(0, 1));
            }

            int currentCategory = 0;
            for (int i = 1; i < allLines.Length; i++)
            {
                string currentLine = allLines[i];
                if (currentLine.Length <= 1) continue;

                if (currentLine.StartsWith(Localization_SOURCE.DELIMITER_CATEGORY))
                {
                    currentCategory++;
                    continue;
                }

                if (!locManagerSelected && currentLine.IndexOf(Localization_SOURCE.DELIMITER_KEY) <= 1)
                    continue;

                LocalizationElement locElement = new LocalizationElement();
                string keySrc = locManagerSelected ? currentLine : currentLine.Substring(0, currentLine.IndexOf(Localization_SOURCE.DELIMITER_KEY));
                locElement.key = keySrc;
                if (!locManagerSelected)
                {
                    string keyText = currentLine.Substring(keySrc.Length + 1, currentLine.Length - keySrc.Length - 1);
                    locElement.text = keyText.Replace(Localization_SOURCE.NEW_LINE_SYMBOL, System.Environment.NewLine);
                }
                locElement.categoryIndex = currentCategory;

                localizationElements.Add(locElement);
            }
        }

        public static void Loc_SaveDatabase(string ToPath, bool RefreshData = true)
        {
            if (!File.Exists(ToPath))
            {
                Loc_ErrorDebug("The file path " + ToPath + " doesn't exist!");
                return;
            }

            File.WriteAllText(ToPath, locHeadingFormat);

            FileStream fstream = new FileStream(ToPath, FileMode.Append);
            StreamWriter fwriter = new StreamWriter(fstream);

            fwriter.WriteLine("");

            foreach (string category in locAvailableCategories)
            {
                //Write category name first
                if (category != "Default") fwriter.WriteLine(Localization_SOURCE.DELIMITER_CATEGORY + category);

                //Write category elements
                foreach (LocalizationElement locElement in localizationElements)
                {
                    //Check conditions
                    if (category != locAvailableCategories[locElement.categoryIndex]) continue;
                    if (string.IsNullOrEmpty(locElement.key)) continue;

                    //Write key & text
                    if (locElement.key.Contains(Localization_SOURCE.DELIMITER_CATEGORY)
                        ||
                        locElement.key.Contains(Localization_SOURCE.DELIMITER_KEY))
                    {
                        Loc_ErrorDebug("key '" + locElement.key + "' contains Category Delimiter or Key Delimiter. Please remove these characters from the key... Saving process was terminated");
                        fwriter.Dispose();
                        fstream.Close();
                    }

                    if (locManagerSelected)
                        fwriter.WriteLine(locElement.key);
                    else
                    {
                        StringBuilder sb = new StringBuilder(locElement.text);
                        sb.Replace(System.Environment.NewLine, Localization_SOURCE.NEW_LINE_SYMBOL);
                        sb.Replace("\n", Localization_SOURCE.NEW_LINE_SYMBOL);
                        sb.Replace("\r", Localization_SOURCE.NEW_LINE_SYMBOL);
                        fwriter.WriteLine(locElement.key + Localization_SOURCE.DELIMITER_KEY + sb.ToString());
                    }
                }
            }

            fwriter.Dispose();
            fstream.Close();

            AssetDatabase.Refresh();

            if (RefreshData) Loc_RefreshContent();
        }

        public static void Loc_LoadDatabase(string FromPath, bool RefreshData = true)
        {
            if (!File.Exists(FromPath))
            {
                Loc_ErrorDebug("The file path " + FromPath + " doesn't exist!");
                return;
            }

            if (File.ReadAllLines(FromPath).Length > 1)
            {
                List<string> storedFilelines = new List<string>();
                for (int i = 1; i < File.ReadAllLines(FromPath).Length; i++)
                    storedFilelines.Add(File.ReadAllLines(FromPath)[i]);

                foreach (string categories in locAvailableCategories)
                {
                    foreach (LocalizationElement locArray in localizationElements)
                    {
                        if (Loc_GetLocalizationCategory(categories) == locArray.categoryIndex)
                        {
                            foreach (string s in storedFilelines)
                            {
                                if (string.IsNullOrEmpty(s)) continue;
                                if (s.StartsWith(Localization_SOURCE.DELIMITER_CATEGORY)) continue;

                                string Key = s.Contains(Localization_SOURCE.DELIMITER_KEY) ? s.Substring(0, s.IndexOf(Localization_SOURCE.DELIMITER_KEY)) : s;
                                if (string.IsNullOrEmpty(Key)) continue;

                                if (Key == locArray.key)
                                {
                                    if (s.Length < Key.Length + 1) continue;
                                    locArray.text = s.Substring(Key.Length + 1, s.Length - Key.Length - 1).Replace(Localization_SOURCE.NEW_LINE_SYMBOL, System.Environment.NewLine);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (RefreshData) Loc_RefreshContent();
        }

        #endregion

        private static int Loc_GetLocalizationCategory(string entry)
        {
            int c = 0;
            foreach (string categ in locAvailableCategories)
            {
                if (categ == entry)
                    return c;
                c++;
            }
            return 0;
        }

        private void OnGUI()
        {
            EditorGUI.indentLevel++;
            PDrawSpace();

            GUILayout.BeginHorizontal();
            PDrawLabel("Localization Manager by Matej Vanco", true);
            GUILayout.FlexibleSpace();
            Color gc = GUI.color;
            GUI.color = Color.magenta;
            if (GUILayout.Button("Discord", GUILayout.Width(150)))
                Application.OpenURL("https://discord.com/invite/WdcYHBtCfr");
            GUI.color = gc;
            GUILayout.EndHorizontal();

            PDrawSpace();

            if (!locReadySteady)
            {
                GUILayout.BeginVertical("Box");

                EditorGUILayout.HelpBox("There is no Localization Manager file. To set up keys structure and language system, select or create a Localization Manager file.", MessageType.Info);
                GUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("Select Localization Manager file"))
                {
                    string f = EditorUtility.OpenFilePanel("Select Localization Manager file", Application.dataPath, "txt");
                    if (string.IsNullOrEmpty(f)) return;
                    locSelectedPath = f;
                    PlayerPrefs.SetString(locRegistryKey, locSelectedPath);
                    Loc_MessageDebug("All set! The Localization Manager is now ready.");
                    Loc_GetLocalizationManagerPath();
                    return;
                }
                if (GUILayout.Button("Create a Localization Manager file"))
                {
                    string f = EditorUtility.SaveFilePanel("Create Localization Manager file", Application.dataPath, "LocalizationManager", "txt");
                    if (string.IsNullOrEmpty(f))
                        return;
                    File.Create(f).Dispose();
                    locSelectedPath = f;
                    PlayerPrefs.SetString(locRegistryKey, locSelectedPath);
                    Loc_MessageDebug("Great! The Localization Manager is now ready.");
                    Loc_SaveDatabase(locSelectedPath, false);
                    Loc_GetLocalizationManagerPath();
                    return;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                return;
            }

            #region SECTION__UPPER

            GUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("Save System"))
                Loc_SaveDatabase(locSelectedPath);
            PDrawSpace();
            if (locManagerSelected)
            {
                if (GUILayout.Button("Reset Manager Path"))
                {
                    if (EditorUtility.DisplayDialog("Question", "You are about to reset the Localization Manager path... No file or folder will be removed, only the current Language Localization path from the registry. Are you sure to continue?", "Yes", "No"))
                    {
                        PlayerPrefs.DeleteKey(locRegistryKey);
                        Close();
                        return;
                    }
                }
            }
            GUILayout.EndHorizontal();

            PDrawSpace(5);

            string lang;
            if (!string.IsNullOrEmpty(locCurrentLanguage))
            {
                locManagerSelected = false;
                lang = locCurrentLanguage;
            }
            else
            {
                locManagerSelected = true;
                lang = "Language Manager";
            }

            GUILayout.BeginHorizontal("Box");
            PDrawLabel("Selected: " + lang);
            if (GUILayout.Button("Deselect Language"))
            {
                Loc_SaveDatabase(locSelectedPath, false);
                Loc_GetLocalizationManagerPath();
            }
            if (GUILayout.Button("Select Language"))
                Loc_SelectLanguageFile();
            PDrawSpace();
            if (GUILayout.Button("Create Language"))
                Loc_CreateLanguageFile();
            GUILayout.EndHorizontal();

            #endregion

            PDrawSpace(5);

            GUILayout.BeginVertical("Box");

            #region SECTION__CATEGORIES

            GUILayout.BeginHorizontal("Box");
            EditorGUIUtility.labelWidth -= 70;
            locCategorySelected = EditorGUILayout.Popup("Category:", locCategorySelected, locAvailableCategories.ToArray(), GUILayout.MaxWidth(300), GUILayout.MinWidth(150));
            EditorGUIUtility.labelWidth += 70;
            if (locManagerSelected)
            {
                PDrawSpace();
                locSelectedCategoryName = EditorGUILayout.TextField(locSelectedCategoryName);
                if (GUILayout.Button("Add Category"))
                {
                    if (string.IsNullOrEmpty(locSelectedCategoryName))
                    {
                        Loc_ErrorDebug("Please fill the required field! [Category Name]");
                        return;
                    }
                    locAvailableCategories.Add(locSelectedCategoryName);
                    locSelectedCategoryName = "";
                    GUI.FocusControl("Set");
                    return;
                }
                if (GUILayout.Button("Remove Category") && locAvailableCategories.Count > 1)
                {
                    if (EditorUtility.DisplayDialog("Question", "You are about to remove a category... Are you sure?", "Yes", "No"))
                    {
                        if (string.IsNullOrEmpty(locSelectedCategoryName))
                        {
                            locAvailableCategories.RemoveAt(locAvailableCategories.Count - 1);
                            locCategorySelected = 0;
                        }
                        else
                        {
                            int cc = 0;
                            bool notfound = true;
                            foreach (string cat in locAvailableCategories)
                            {
                                if (locSelectedCategoryName == cat)
                                {
                                    locAvailableCategories.RemoveAt(cc);
                                    locCategorySelected = 0;
                                    notfound = false;
                                    break;
                                }
                                cc++;
                            }
                            if (notfound) Loc_ErrorDebug("The category couldn't be found.");
                            locSelectedCategoryName = "";
                        }
                        return;
                    }
                }
            }
            GUILayout.EndHorizontal();

            #endregion

            PDrawSpace();

            #region SECTION__LOCALIZATION_ARRAY

            GUILayout.BeginHorizontal();
            PDrawLabel("Localization Keys & Texts");
            if (locManagerSelected && GUILayout.Button("+"))
                localizationElements.Add(new LocalizationElement() { categoryIndex = locCategorySelected });
            GUILayout.EndHorizontal();

            if (localizationElements.Count == 0)
                PDrawLabel(" - - Empty! - -");
            else
            {
                guiScrollHelper = EditorGUILayout.BeginScrollView(guiScrollHelper);

                int c = 0;
                foreach (LocalizationElement locA in localizationElements)
                {
                    if (locA.categoryIndex >= locAvailableCategories.Count)
                    {
                        locA.categoryIndex = 0;
                        break;
                    }
                    if (locAvailableCategories[locA.categoryIndex] != locAvailableCategories[locCategorySelected])
                        continue;

                    EditorGUIUtility.labelWidth -= 100;
                    EditorGUILayout.BeginHorizontal("Box");
                    if (!locManagerSelected)
                    {
                        EditorGUILayout.LabelField(locA.key, GUILayout.Width(100));

                        EditorGUILayout.LabelField("Text:", GUILayout.Width(100));
                        locA.text = EditorGUILayout.TextArea(locA.text, GUILayout.MinWidth(100));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Key:", GUILayout.Width(45));

                        locA.key = EditorGUILayout.TextField(locA.key, GUILayout.MaxWidth(100), GUILayout.MinWidth(30));
                        EditorGUILayout.LabelField("Category:", GUILayout.Width(75));
                        locA.categoryIndex = EditorGUILayout.Popup(locA.categoryIndex, locAvailableCategories.ToArray());
                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            localizationElements.Remove(locA);
                            return;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUIUtility.labelWidth += 100;
                    c++;
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            #endregion

            EditorGUI.indentLevel--;
        }

        #region LayoutShortcuts

        private void PDrawLabel(string text, bool bold = false)
        {
            if (bold)
            {
                string add = "<b>";
                add += text + "</b>";
                text = add;
            }
            GUIStyle style = new GUIStyle();
            style.richText = true;
            style.normal.textColor = Color.white;
            EditorGUILayout.LabelField(text, style);
        }

        private void PDrawSpace(float space = 10)
            => GUILayout.Space(space);

        #endregion

        private static void Loc_ErrorDebug(string msg)
        {
            EditorUtility.DisplayDialog("Error", msg, "OK");
        }

        private static void Loc_MessageDebug(string msg)
        {
            EditorUtility.DisplayDialog("Info", msg, "OK");
        }
    }
}
#endif