//////////////////////////////////////////////////////
///        © Copyright 2024 - ReForge Mode         ///
/// See the LICENSE file for licensing information ///
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;

namespace ReForgeMode
{
    namespace NEB
    {
        public class Page_Organizer : ParentPage
        {
            public override int GetScriptPriority()
            {
                return 20;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(500f, 280f);
            }

            public override string GetWindowTitle()
            {
                return "Organizer";
            }

            public override string GetWindowHelpLink()
            {
                return "https://www.youtube.com/@ReForgeMode";
            }

            public override void PrepareThisPage()
            {
                
            }


            public override void DisplayPageContent()
            {
                
            }









        }
    }
}
#endif