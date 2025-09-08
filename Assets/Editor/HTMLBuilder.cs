using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

public class HTMLBuilder
{
    private const string WEB_COMPONENTS_FOLDER = "Assets/WebComponents";

    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.WebGL)
        {
            Debug.Log("=== HTML Builder: Starting automatic HTML generation ===");
            GenerateCustomHTML(pathToBuiltProject);

            Debug.Log("=== HTML Builder: Starting WebComponents file copying ===");
            CopyWebComponents(pathToBuiltProject);
            Debug.Log("=== HTML Builder: Writing cache headers ===");
            WriteNoCacheHeaders(pathToBuiltProject);
        }
    }

    private static void GenerateCustomHTML(string buildPath)
    {
        // Find the generated index.html file
        string originalHtmlPath = Path.Combine(buildPath, "index.html");
        string backupHtmlPath = Path.Combine(buildPath, "index_original.html");
        
        if (!File.Exists(originalHtmlPath))
        {
            Debug.LogError("HTML Builder: Original index.html not found!");
            return;
        }

        // Skip if already processed (prevent double-processing)
        if (File.Exists(backupHtmlPath))
        {
            Debug.Log("HTML Builder: Backup already exists, skipping HTML generation to prevent overwrite");
            return;
        }

        try
        {
            // Create backup of original
            File.Copy(originalHtmlPath, backupHtmlPath, true);
            Debug.Log("HTML Builder: Created backup at index_original.html");

            // Read the original HTML
            string originalHtml = File.ReadAllText(originalHtmlPath);
            
            // Extract build information from original HTML
            BuildInfo buildInfo = ExtractBuildInfo(originalHtml);
            
            // Generate our custom HTML
            string customHtml = GenerateReactUnityHTML(buildInfo);
            
            // Write the new HTML only if template was successfully loaded
            if (!string.IsNullOrEmpty(customHtml))
            {
                File.WriteAllText(originalHtmlPath, customHtml);
                WriteVersionJson(buildPath, buildInfo.productVersion);
                Debug.Log("HTML Builder: Successfully generated React-Unity integrated HTML");
            }
            else
            {
                throw new BuildFailedException("HTMLTemplate.html missing or failed to load. Aborting build to prevent unusable artifact.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HTML Builder Error: {e.Message}");
        }
    }

    private static void WriteVersionJson(string buildPath, string buildId)
    {
        try
        {
            var versionPath = Path.Combine(buildPath, "version.json");
            var json = "{ \"buildId\": \"" + buildId.Replace("\"", "\\\"") + "\" }";
            File.WriteAllText(versionPath, json);
            Debug.Log($"HTML Builder: Wrote version.json with buildId '{buildId}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HTML Builder: Failed to write version.json — {e.Message}");
        }
    }

    private static void WriteNoCacheHeaders(string buildPath)
    {
        try
        {
            var headersPath = Path.Combine(buildPath, "_headers");
            var content =
@"/index.html
  Cache-Control: no-store

/*/index.html
  Cache-Control: no-store

/.netlify/functions/*
  Access-Control-Allow-Origin: *
  Access-Control-Allow-Methods: GET, POST, OPTIONS
  Access-Control-Allow-Headers: Content-Type, Authorization, X-Requested-With
";
            if (File.Exists(headersPath))
            {
                // Append if a headers file already exists
                File.AppendAllText(headersPath, "\n" + content);
                Debug.Log("HTML Builder: Appended no-cache rules for index.html to existing _headers");
            }
            else
            {
                File.WriteAllText(headersPath, content);
                Debug.Log("HTML Builder: Wrote _headers file to disable index.html caching");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HTML Builder: Failed to write _headers file — {e.Message}");
        }
    }

    private static BuildInfo ExtractBuildInfo(string originalHtml)
    {
        var buildInfo = new BuildInfo();
        
        try
        {
            // Use regex for more robust parsing
            var buildUrlMatch = System.Text.RegularExpressions.Regex.Match(
                originalHtml, @"var\s+buildUrl\s*=\s*[""']([^""']+)[""']");
            if (buildUrlMatch.Success)
            {
                buildInfo.buildUrl = buildUrlMatch.Groups[1].Value;
            }
            
            // Extract loader URL with regex
            var loaderMatch = System.Text.RegularExpressions.Regex.Match(
                originalHtml, @"buildUrl\s*\+\s*[""']/([^""']+)\.loader\.js[""']");
            if (loaderMatch.Success)
            {
                buildInfo.buildName = loaderMatch.Groups[1].Value;
                Debug.Log($"HTML Builder: Detected build name: {buildInfo.buildName}");
            }
            else
            {
                Debug.LogWarning($"HTML Builder: Could not detect build name, using fallback: {buildInfo.buildName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"HTML Builder: Could not extract build info, using defaults. Error: {e.Message}");
        }
        
        return buildInfo;
    }
    
    private class BuildInfo
    {
        public string buildUrl = "Build";
        public string buildName = "";
        public string productName = "";
        public string companyName = "";
        public string productVersion = "";
        
        public BuildInfo()
        {
            // Set safer defaults based on PlayerSettings
            productName = PlayerSettings.productName;
            companyName = PlayerSettings.companyName;
            productVersion = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
            
            // Generate fallback build name from product name
            buildName = $"Unity_WebGL_{productName.Replace(" ", "_")}_v1.0";
        }
    }

    private static string GenerateReactUnityHTML(BuildInfo buildInfo)
    {
        // Get the product name from PlayerSettings
        string productName = PlayerSettings.productName;

        // Load external template; do not fallback to embedded HTML
        string template = LoadHTMLTemplate();
        if (string.IsNullOrEmpty(template))
        {
            // Signal caller to skip writing
            return null;
        }

        string customHtml = template
            .Replace("__PRODUCT_VERSION__", buildInfo.productVersion)
            .Replace("__PRODUCT_NAME__", productName)
            .Replace("__BUILD_URL__", buildInfo.buildUrl)
            .Replace("__BUILD_NAME__", buildInfo.buildName)
            .Replace("__COMPANY_NAME__", buildInfo.companyName);

        return customHtml;
    }

    private static string LoadHTMLTemplate()
    {
        try
        {
            string editorPath = Path.Combine(Application.dataPath, "Editor/HTMLTemplate.html");
            string webComponentsPath = Path.Combine(Application.dataPath, "WebComponents/HTMLTemplate.html");

            if (File.Exists(editorPath))
                return File.ReadAllText(editorPath);
            if (File.Exists(webComponentsPath))
                return File.ReadAllText(webComponentsPath);

            Debug.LogError("HTML Builder: HTMLTemplate.html not found in Assets/Editor or Assets/WebComponents.");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HTML Builder: Failed to load HTMLTemplate.html — {e.Message}");
            return null;
        }
    }

    private static void CopyWebComponents(string buildPath)
    {
        string sourceDir = Path.Combine(Application.dataPath.Replace("/Assets", ""), WEB_COMPONENTS_FOLDER);
        
        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning($"HTML Builder: WebComponents source directory not found: {sourceDir}");
            return;
        }

        try
        {
            int copiedFiles = CopyDirectoryRecursive(sourceDir, buildPath, WEB_COMPONENTS_FOLDER);
            Debug.Log($"HTML Builder: Successfully copied {copiedFiles} files from WebComponents to build folder");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HTML Builder: Error copying WebComponents files: {e.Message}");
        }
    }

        private static int CopyDirectoryRecursive(string sourceDir, string destDir, string relativePath)
    {
        int filesCopied = 0;
        
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Copy all files in current directory
        string[] files = Directory.GetFiles(sourceDir);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            
            if (ShouldSkipFile(fileName))
            {
                Debug.Log($"HTML Builder: Skipping file: {fileName}");
                continue;
            }

            string destFile = Path.Combine(destDir, fileName);
            
            try
            {
                File.Copy(file, destFile, true);
                filesCopied++;
                Debug.Log($"HTML Builder: Copied {fileName}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"HTML Builder: Failed to copy {fileName}: {e.Message}");
            }
        }

        // Recursively copy subdirectories
        string[] directories = Directory.GetDirectories(sourceDir);
        foreach (string directory in directories)
        {
            string dirName = Path.GetFileName(directory);
            string destSubDir = Path.Combine(destDir, dirName);
            filesCopied += CopyDirectoryRecursive(directory, destSubDir, Path.Combine(relativePath, dirName));
        }

        return filesCopied;
    }

    private static bool ShouldSkipFile(string fileName)
    {
        // Skip Unity .meta files
        if (fileName.EndsWith(".meta"))
            return true;
        
        // Skip temp files and system files
        if (fileName.StartsWith(".") || fileName.StartsWith("~"))
            return true;
        
        // Skip common temp file extensions
        string extension = Path.GetExtension(fileName).ToLower();
        if (extension == ".tmp" || extension == ".temp" || extension == ".bak")
            return true;
        
        return false;
    }

    [MenuItem("Build/Generate index.html")]
    public static void GenerateHTMLManually()
    {
        string outputPath = EditorUtility.SaveFilePanel(
            "Save Generated index.html",
            "",
            "index.html",
            "html"
        );
        
        if (!string.IsNullOrEmpty(outputPath))
        {
            var buildInfo = new BuildInfo();
            string customHtml = GenerateReactUnityHTML(buildInfo);
            if (string.IsNullOrEmpty(customHtml))
            {
                throw new BuildFailedException("HTMLTemplate.html missing or failed to load. Cannot generate index.html manually.");
            }
            File.WriteAllText(outputPath, customHtml);
            Debug.Log($"HTML Builder: index.html generated at {outputPath}");
        }
    }
    
    [MenuItem("Build/Update index.html")]
    public static void ForceRegenerateHTML()
    {
        string buildPath = EditorUtility.OpenFolderPanel("Select WebGL Build Folder", "", "");
        if (!string.IsNullOrEmpty(buildPath))
        {
            // Remove backup to force regeneration
            string backupPath = Path.Combine(buildPath, "index_original.html");
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            
            GenerateCustomHTML(buildPath);
        }
    }
    // Pre-build check: ensure HTMLTemplate.html is present before starting WebGL build
    class HtmlTemplatePrecheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
            {
                string editorPath = Path.Combine(Application.dataPath, "Editor/HTMLTemplate.html");
                string webComponentsPath = Path.Combine(Application.dataPath, "WebComponents/HTMLTemplate.html");

                if (!File.Exists(editorPath) && !File.Exists(webComponentsPath))
                {
                    throw new BuildFailedException("HTMLTemplate.html not found in Assets/Editor or Assets/WebComponents. Build aborted.");
                }
            }
        }
    }
}