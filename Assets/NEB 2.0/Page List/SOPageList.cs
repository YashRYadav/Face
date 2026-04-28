//////////////////////////////////////////////////////
///        © Copyright 2024 - ReForge Mode         ///
/// See the LICENSE file for licensing information ///
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

namespace ReForgeMode
{
    namespace NEB
    {
        [CreateAssetMenu(fileName = "NEB Page List", menuName = "ReForge Mode/NEB Page List")]
        public class SOPageList : ScriptableObject
        {
            public List<ParentPage> pageList = new List<ParentPage>();
            public string[] nameList;

            /// <summary>
            /// Find all scripts that is a Page and list them together.
            /// </summary>
            public void FindAllPageScript()
            {
                //Clear the list first
                pageList.Clear();

                //Find all types derived from ParentPage. I think this is a type of Reflection?
                var derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(assembly => assembly.GetTypes())
                                   .Where(type => type.IsSubclassOf(typeof(ParentPage)));

                //Create instances of each derived type and add them to the list
                foreach (Type type in derivedTypes)
                {
                    ParentPage script = ScriptableObject.CreateInstance(type) as ParentPage;
                    if (script != null)
                    {
                        pageList.Add(script);
                    }
                }

                //Sort them by priority
                pageList.Sort((x, y) => x.GetScriptPriority().CompareTo(y.GetScriptPriority()));

                FindScriptNames();
            }

            /// <summary>
            /// Convert the script names from pageList.
            /// </summary>
            private void FindScriptNames()
            {
                if (pageList.Count <= 0) return;

                nameList = new string[pageList.Count];
                for (int i = 0; i < pageList.Count; i++)
                {
                    nameList[i] = pageList[i].GetWindowTitle();

                    //string scriptName = pageList[i].ToString();

                    ////Cut the name after the dot
                    //int index = scriptName.IndexOf('.');
                    //if (index != -1 && index < scriptName.Length - 1)
                    //{
                    //    scriptName = scriptName.Substring(index + 1);
                    //}

                    ////Then remove the ")"
                    //scriptName = scriptName.Replace(")", "");

                    //nameList[i] = scriptName.Substring(4);
                }
            }
        }
    }
}
#endif