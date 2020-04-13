using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using System;

public class JPiolhoAssetBundles : EditorWindow
{
   private struct BuildStatusContainer {
      public bool IsBuilding { get; set; }
      public int CurrentStep { get; set; }
      public int TotalSteps { get; set; }
   }
   
   
   
   
   // Add menu item
   [MenuItem("KC Toolkit/JPiolho's Build Asset Bundle")]
   public static void Init()
   {
      EditorWindow window = EditorWindow.CreateInstance<JPiolhoAssetBundles>();
      window.Show();
   }
   
   
   
   private void UpdateStatus(string status,bool progress=true) {
      if(progress)
         buildStatus.CurrentStep++;

      // Show the progress bar
      EditorUtility.DisplayProgressBar("KC Asset Bundle build",status,buildStatus.CurrentStep / (float)buildStatus.TotalSteps);
   }

   private BuildStatusContainer buildStatus;
   private void Build()
   {
      try {
         buildStatus.TotalSteps = !guiToggleCopyToModFolder ? 8 : 10;
         buildStatus.IsBuilding = true;
         buildStatus.CurrentStep = 0;
         
         var win32Path = Path.Combine(Application.dataPath, "Workspace/win32");
         UpdateStatus("Preparing directory for win32",false);
         PrepareDirectory(win32Path);
         UpdateStatus("Building win32");
         BuildPipeline.BuildAssetBundles(win32Path, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneWindows);
         
         var win64Path = Path.Combine(Application.dataPath, "Workspace/win64");
         UpdateStatus("Preparing directory for win64");
         PrepareDirectory(win64Path);
         UpdateStatus("Building win64");
         BuildPipeline.BuildAssetBundles(win64Path, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneWindows64);
         
         var osxPath = Path.Combine(Application.dataPath, "Workspace/osx");
         UpdateStatus("Preparing directory for osx");
         PrepareDirectory(osxPath);
         UpdateStatus("Building osx");
         BuildPipeline.BuildAssetBundles(osxPath, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneOSX);
         
         var linuxPath = Path.Combine(Application.dataPath, "Workspace/linux");
         UpdateStatus("Preparing directory for linux");
         PrepareDirectory(linuxPath);
         UpdateStatus("Building linux");
         BuildPipeline.BuildAssetBundles(linuxPath, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneLinuxUniversal);
         
         if(guiToggleCopyToModFolder) {        
            UpdateStatus("Purging old bundles in mod folder");
            PrepareDirectory(Path.Combine(guiTextFieldModFolder,"win32"));
            PrepareDirectory(Path.Combine(guiTextFieldModFolder,"win64"));
            PrepareDirectory(Path.Combine(guiTextFieldModFolder,"osx"));
            PrepareDirectory(Path.Combine(guiTextFieldModFolder,"linux"));
            
            UpdateStatus("Copying bundles to mod folder");
            DirectoryCopy(win32Path,Path.Combine(guiTextFieldModFolder,"win32"));
            DirectoryCopy(win64Path,Path.Combine(guiTextFieldModFolder,"win64"));
            DirectoryCopy(osxPath,Path.Combine(guiTextFieldModFolder,"osx"));
            DirectoryCopy(linuxPath,Path.Combine(guiTextFieldModFolder,"linux"));
            
            EditorUtility.DisplayDialog("Asset Bundles","Asset Bundles built and copied to mod folder","Yay!");
         } else {
            EditorUtility.DisplayDialog("Asset Bundles","Asset Bundles built","Yay!");
         }
      }
      finally {
         buildStatus.IsBuilding = false;
         EditorUtility.ClearProgressBar();
      }
   }
   
   private bool Verify() {
      if(guiToggleCopyToModFolder) {
         if(string.IsNullOrEmpty(guiTextFieldModFolder)) {
            EditorUtility.DisplayDialog("Error","You need to specify a mod folder.","Ok");
            return false;
         }
         
         if(!Directory.Exists(guiTextFieldModFolder)) {
            EditorUtility.DisplayDialog("Error","Could not find mod folder. Check that the path is correct.","Ok");
            return false;
         }
      }
      
      return true;
   }
   
   
   private string guiTextFieldModFolder;
   private bool guiToggleCopyToModFolder;
   void OnGUI()
   {
      GUILayout.BeginVertical();
      {
         guiTextFieldModFolder = EditorGUILayout.TextField("Mod folder:",guiTextFieldModFolder);
         guiToggleCopyToModFolder = EditorGUILayout.Toggle("Copy to mod folder?",guiToggleCopyToModFolder);
         
         if (GUILayout.Button("Build Asset Bundles"))
         {
            if(Verify())
               Build();
         }
      }
      GUILayout.EndVertical();
   }
   
   protected void OnEnable() {
      guiTextFieldModFolder = EditorPrefs.GetString("KCModFolder");
      guiToggleCopyToModFolder = EditorPrefs.GetBool("KCCopyToMod");
   }
   
   protected void OnDisable() {
      EditorPrefs.SetString("KCModFolder",guiTextFieldModFolder);
      EditorPrefs.SetBool("KCCopyToMod",guiToggleCopyToModFolder);
   }
   
   
   private static void PrepareDirectory(string path) {
      // If folder doesn't exist, create it. Otherwise, delete everything and re-create it.
      if(!Directory.Exists(path)) {
         Directory.CreateDirectory(path);
      }
      else {
         // Delete directory
         Directory.Delete(path,true);
         
         // Wait for the folder to delete fully (also define a small runaway to prevent infinite loop)
         const int MaxRunaway = 120;
         int runaway = 0;
         while(Directory.Exists(path) && ++runaway < MaxRunaway) {
            Thread.Sleep(50);
         }
         
         if(runaway >= MaxRunaway)
            throw new Exception($"Failed to delete directory '{path}'");
         
         // Re-create the folder
         Directory.CreateDirectory(path);
      }
   }

   
   // Code from: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
   private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs=true)
   {
     // Get the subdirectories for the specified directory.
     DirectoryInfo dir = new DirectoryInfo(sourceDirName);

     if (!dir.Exists)
     {
         throw new DirectoryNotFoundException(
             "Source directory does not exist or could not be found: "
             + sourceDirName);
     }

     DirectoryInfo[] dirs = dir.GetDirectories();
     // If the destination directory doesn't exist, create it.
     if (!Directory.Exists(destDirName))
     {
         Directory.CreateDirectory(destDirName);
     }
     
     // Get the files in the directory and copy them to the new location.
     FileInfo[] files = dir.GetFiles();
     foreach (FileInfo file in files)
     {
         string temppath = Path.Combine(destDirName, file.Name);
         file.CopyTo(temppath, false);
     }

     // If copying subdirectories, copy them and their contents to new location.
     if (copySubDirs)
     {
         foreach (DirectoryInfo subdir in dirs)
         {
             string temppath = Path.Combine(destDirName, subdir.Name);
             DirectoryCopy(subdir.FullName, temppath, copySubDirs);
         }
     }
   }
}

