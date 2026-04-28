//////////////////////////////////////////////////////
///        © Copyright 2024 - ReForge Mode         ///
/// See the LICENSE file for licensing information ///
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace ReForgeMode
{
    namespace NEB
    {
        public static class Utilities
        {

            public static class SearchAndFind
            {
                public static UnityEngine.Object FindAsset(string filterType, string name, string extension)
                {
                    // Get all assets in the project with the given name and extension
                    string[] assetGuids = AssetDatabase.FindAssets("t:" + filterType);
                    //string[] assetGuids = AssetDatabase.FindAssets("t:" + typeof(UnityEngine.Object).Name);

                    // Loop through all found assets and check if any has the correct name and extension
                    foreach (string assetGuid in assetGuids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                        UnityEngine.Object file = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));

                        // Check if the asset name matches exactly and has the correct extension
                        if (file != null && file.name == name && assetPath.EndsWith(extension))
                        {
                            //Debug.Log("Found file: " + file.name);
                            return file;
                        }
                    }

                    Debug.LogWarning("Error: File not found! Check Utilities.");
                    return null; // File not found with the given name and extension
                }
            }

            public static class CustomGUI
            {
                public static CustomGUIStyle guiStyle;

                public struct CustomGUIStyle
                {
                    public GUIStyle rightMiddle;
                    public GUIStyle centerUpper;
                    public GUIStyle centerMiddle;
                    public GUIStyle centerLower;
                    public GUIStyle bold;
                    public GUIStyle boldCenter;
                }

                public static void SetGUIStyles()
                {
                    guiStyle.bold = new GUIStyle();
                    guiStyle.bold.fontStyle = FontStyle.Bold;
                    guiStyle.bold.normal.textColor = new Color(1f, 1f, 1f, 0.75f);

                    guiStyle.boldCenter = new GUIStyle();
                    guiStyle.boldCenter.fontStyle = FontStyle.Bold;
                    guiStyle.boldCenter.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
                    guiStyle.boldCenter.alignment = TextAnchor.MiddleCenter;

                    guiStyle.centerMiddle = new GUIStyle();
                    guiStyle.centerMiddle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
                    guiStyle.centerMiddle.alignment = TextAnchor.MiddleCenter;

                    guiStyle.centerUpper = new GUIStyle();
                    guiStyle.centerUpper.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
                    guiStyle.centerUpper.alignment = TextAnchor.UpperCenter;

                    guiStyle.centerLower = new GUIStyle();
                    guiStyle.centerLower.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
                    guiStyle.centerLower.alignment = TextAnchor.LowerCenter;

                    guiStyle.rightMiddle = new GUIStyle();
                    guiStyle.rightMiddle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
                    guiStyle.rightMiddle.alignment = TextAnchor.MiddleRight;
                }
            }
        }
    }
}
#endif
