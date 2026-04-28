//////////////////////////////////////////////////////
///        © Copyright 2024 - ReForge Mode         ///
/// See the LICENSE file for licensing information ///
//////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
namespace ReForgeMode
{
    namespace NEB
    {
        using UnityEditor;

        public abstract class ParentPage : EditorWindow
        {
            /// <summary>
            /// Get the supposed value of window size to be set on startup.
            /// </summary>
            /// <param name="width"></param>
            /// <param name="height"></param>
            public abstract Vector2 GetWindowSize();

            /// <summary>
            /// Get the window name to be displayed on the top of the screen.
            /// </summary>
            public abstract string GetWindowTitle();

            /// <summary>
            /// Get the youtube video where for that page
            /// </summary>
            /// <returns></returns>
            public abstract string GetWindowHelpLink();

            /// <summary>
            /// Get the script priority to be sorted by PageList ScriptableObject
            /// </summary>
            /// <returns></returns>
            public abstract int GetScriptPriority();

            /// <summary>
            /// Function to run before a page is run.
            /// </summary>
            public abstract void PrepareThisPage();

            /// <summary>
            /// Function layout to display the page.
            /// </summary>
            public abstract void DisplayPageContent();
        }
    }
}
#endif