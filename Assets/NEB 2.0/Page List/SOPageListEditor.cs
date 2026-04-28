//////////////////////////////////////////////////////
///        © Copyright 2024 - ReForge Mode         ///
/// See the LICENSE file for licensing information ///
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace ReForgeMode
{
    namespace NEB
    {
        using UnityEditor;

        /// <summary>
        /// This entire custom editor is just to display interface as a list, since Unity can't do it.
        /// </summary>
        [CustomEditor(typeof(SOPageList))]
        public class SOPageListEditor : Editor
        {
            private SOPageList pageList;

            public void OnEnable()
            {
                pageList = (SOPageList)target;
            }

            public override void OnInspectorGUI()
            {
                if (GUILayout.Button("Force Search", GUILayout.Height(25f)))
                {
                    pageList.FindAllPageScript();
                }

                EditorGUILayout.Space(15f);
                GUILayout.Label("All found pages", EditorStyles.boldLabel);

                if (pageList.pageList.Count == 0)
                {
                    GUILayout.Label("  No Page found at the moment.");
                    return;
                }

                for (int i = 0; i < pageList.nameList.Length; i++)
                {
                    GUILayout.Label(pageList.pageList[i].GetScriptPriority() + " " + pageList.nameList[i]);
                }
            }
        }
    }
}
#endif