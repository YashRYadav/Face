//////////////////////////////////////////////////////
///        © Copyright 2024 - ReForge Mode         ///
/// See the LICENSE file for licensing information ///
//////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;
using System;





#if UNITY_EDITOR
using UnityEditor;

namespace ReForgeMode
{
    namespace NEB
    {
        public class MainPage : EditorWindow
        {
            private SOPageList SOPageList;
            private int currentPageIndex = 0;
            private int last_currentPageIndex = -1;

            private Color backgroundColor;

            [MenuItem("ReForge Mode/NEB Plugin")]
            public static void ShowWindow()
            {
                string windowTitle = VersionCheck.isProVersion ? "NEB PRO" : "NEB Lite";
                windowTitle += " " + VersionCheck.versionNumber;
                GetWindow<MainPage>(windowTitle);
            }

            private void OnEnable()
            {
                //Set Custom GUI styles
                Utilities.CustomGUI.SetGUIStyles();

                //Find Find Asset
                SOPageList = (SOPageList)Utilities.SearchAndFind.FindAsset("ScriptableObject", "NEB Page List", ".asset");
                SOPageList.FindAllPageScript();
            }

            private void OnGUI()
            {
                //Calculate the inner window rect
                float margin = 20f;
                Rect innerWindowRect = new Rect(margin, margin, position.width - (margin * 2),
                                                position.height - (margin * 2));

                DisplayHelpButton();

                //Set background as transparent box
                backgroundColor = EditorStyles.helpBox.normal.textColor;
                backgroundColor.a = 0f;
                EditorGUI.DrawRect(innerWindowRect, backgroundColor);
                GUILayout.BeginArea(innerWindowRect);
                {
                    GUILayout.Space(10f);

                    DisplayMenuToolbar();
                    GUILayout.Space(20f);

                    DisplayPageContent();
                }
                GUILayout.EndArea();
            }

            private void DisplayPageContent()
            {
                SOPageList.pageList[currentPageIndex].DisplayPageContent();
                CheckPageChange();
            }


            private void DisplayHelpButton()
            {
                //Calculate the lower navigation button
                float windowWidth = position.width;
                float windowHeight = position.height;
                float areaWidth = 20f;
                float areaHeight = 30f;
                float x = windowWidth - areaWidth - 10f;
                float y = areaHeight / 2 - 5f;

                Rect buttonRect = new Rect(x, y, areaWidth, areaHeight);
                EditorGUI.DrawRect(buttonRect, backgroundColor);

                //Example UI element inside the inner window
                GUILayout.BeginArea(buttonRect);
                {
                    string tooltip = "Click here to open \nvideo documentation on this page!";
                    if (GUILayout.Button(new GUIContent("?", tooltip)))
                    {
                        string link = SOPageList.pageList[currentPageIndex].GetWindowHelpLink();
                        Application.OpenURL(link);
                    }
                }
                GUILayout.EndArea();
            }

            private void DisplayMenuToolbar()
            {
                //string[] menus = new string[]
                //{ 
                //    "Transfer", 
                //    "Organizer", 
                //    "Combiner", 
                //    "Calibrator", 
                //    "Creator", 
                //    "Extractor", 
                //    "Splitter"
                //};

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                currentPageIndex = GUILayout.Toolbar(currentPageIndex, SOPageList.nameList, GUILayout.Width(500f), GUILayout.Height(25f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            private void CheckPageChange()
            {
                if (currentPageIndex == last_currentPageIndex) return;

                SOPageList.pageList[currentPageIndex].PrepareThisPage();

                last_currentPageIndex = currentPageIndex;
            }
        }
    }
}
#endif