#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class FixOculusNamespace : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000; // Run early

    public void OnPreprocessBuild(BuildReport report)
    {
        ExecuteFix();
    }

    [MenuItem("Tools/Fix Oculus Namespace Conflicts")]
    public static void ExecuteFix()
    {
        Debug.Log("[FixOculusNamespace] Pre-build check starting to fix com.oculus.Integration namespace conflicts...");

        // Search directories
        string[] searchDirs = new string[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache"),
            Path.Combine(Directory.GetCurrentDirectory(), "Packages"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets")
        };

        int fixCount = 0;

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;

            string[] aarFiles = Directory.GetFiles(dir, "*.aar", SearchOption.AllDirectories);
            foreach (var aarPath in aarFiles)
            {
                string filename = Path.GetFileName(aarPath);
                string targetNamespace = "";

                if (filename.Equals("OVRPlugin.aar", System.StringComparison.OrdinalIgnoreCase))
                {
                    targetNamespace = "com.oculus.Integration.ovr";
                }
                else if (filename.Equals("InteractionSdk.aar", System.StringComparison.OrdinalIgnoreCase))
                {
                    targetNamespace = "com.oculus.Integration.interaction";
                }
                else if (filename.Equals("SDKTelemetry.aar", System.StringComparison.OrdinalIgnoreCase))
                {
                    targetNamespace = "com.oculus.Integration.telemetry";
                }

                if (!string.IsNullOrEmpty(targetNamespace))
                {
                    if (ProcessAarFile(aarPath, targetNamespace))
                    {
                        fixCount++;
                    }
                }
            }
        }

        Debug.Log($"[FixOculusNamespace] Pre-build namespace fix completed. Patched {fixCount} files.");
    }

    private static bool ProcessAarFile(string aarPath, string targetNamespace)
    {
        string tempAarPath = aarPath + ".tmp";
        bool modified = false;

        try
        {
            using (ZipArchive sourceArchive = ZipFile.Open(aarPath, ZipArchiveMode.Read))
            {
                ZipArchiveEntry manifestEntry = sourceArchive.GetEntry("AndroidManifest.xml");
                if (manifestEntry == null) return false;

                string manifestContent;
                using (Stream stream = manifestEntry.Open())
                using (StreamReader reader = new StreamReader(stream))
                {
                    manifestContent = reader.ReadToEnd();
                }

                if (manifestContent.Contains("package=\"com.oculus.Integration\""))
                {
                    modified = true;
                    string updatedContent = manifestContent.Replace("package=\"com.oculus.Integration\"", $"package=\"{targetNamespace}\"");

                    Debug.Log($"[FixOculusNamespace] Rebuilding AAR: Patching {Path.GetFileName(aarPath)} to namespace '{targetNamespace}'...");

                    // Create a new zip archive from scratch
                    using (ZipArchive targetArchive = ZipFile.Open(tempAarPath, ZipArchiveMode.Create))
                    {
                        // Copy all entries except AndroidManifest.xml
                        foreach (var entry in sourceArchive.Entries)
                        {
                            if (entry.FullName.Equals("AndroidManifest.xml", System.StringComparison.OrdinalIgnoreCase)) continue;

                            ZipArchiveEntry newEntry = targetArchive.CreateEntry(entry.FullName, System.IO.Compression.CompressionLevel.Optimal);
                            using (Stream sourceStream = entry.Open())
                            using (Stream targetStream = newEntry.Open())
                            {
                                sourceStream.CopyTo(targetStream);
                            }
                        }

                        // Add the modified AndroidManifest.xml
                        ZipArchiveEntry modifiedManifestEntry = targetArchive.CreateEntry("AndroidManifest.xml", System.IO.Compression.CompressionLevel.Optimal);
                        using (Stream targetStream = modifiedManifestEntry.Open())
                        using (StreamWriter writer = new StreamWriter(targetStream))
                        {
                            writer.Write(updatedContent);
                        }
                    }
                }
            }

            if (modified)
            {
                if (File.Exists(aarPath))
                {
                    File.SetAttributes(aarPath, FileAttributes.Normal);
                    File.Delete(aarPath);
                }
                File.Move(tempAarPath, aarPath);
                Debug.Log($"[FixOculusNamespace] Successfully rebuilt and replaced {Path.GetFileName(aarPath)}");
                return true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FixOculusNamespace] Error processing AAR file {aarPath}: {ex.Message}");
            if (File.Exists(tempAarPath))
            {
                try { File.Delete(tempAarPath); } catch {}
            }
        }
        return false;
    }
}
#endif
