using System;
using System.Linq;
using UnityEditor;

namespace DesertTower.Levels.Diagnostics.Editor
{
    public static class DiagnosticPackageExport
    {
        [MenuItem("Tools/Desert Tower/Export Optional Diagnostics Package…",false,51)]
        public static void Export()
        {
            string path=EditorUtility.SaveFilePanel("Export optional diagnostics","","DesertTowerLevelDiagnostics-0.2.0","unitypackage");
            if(string.IsNullOrEmpty(path)) return;
            string source=AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:MonoScript DiagnosticPackageExport").First());
            string folder=source.Substring(0,source.LastIndexOf("/Editor/",StringComparison.Ordinal));
            var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(folder+"/",StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)).ToArray();
            AssetDatabase.ExportPackage(paths,path,ExportPackageOptions.Default);
            EditorUtility.RevealInFinder(path);
        }
    }
}
