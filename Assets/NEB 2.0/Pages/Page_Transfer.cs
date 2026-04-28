//////////////////////////////////////////////////////
///        © Copyright 2024 - ReForge Mode         ///
/// See the LICENSE file for licensing information ///
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;



#if UNITY_EDITOR
using UnityEditor;

namespace ReForgeMode
{
    namespace NEB
    {
        public class Page_Transfer : ParentPage
        {
            public SkinnedMeshRenderer sourceMeshRenderer;
            public SkinnedMeshRenderer targetMeshRenderer;
            public GameObject sourceModel;
            public GameObject targetModel;
            public Mesh sourceMesh;
            public Mesh targetMesh;

            private bool useMeshField;
            private bool isAdvancedMode;

            //Reference model
            public GameObject referenceModel;
            public SkinnedMeshRenderer referenceMeshRenderer;
            public bool useReferenceModel = false;
            public bool last_useReferenceModel;

            //Error message validations variables
            private bool isCopyButtonEnabled = true;
            private bool isFieldNotNull = true;
            private bool isSourceFaceExist = true;
            private bool isTargetFaceExist = true;
            private bool isSourceSkinnedMeshExist = true;
            private bool isTargetSkinnedMeshExist = true;
            private bool isSourceMeshExist = true;
            private bool isTargetMeshExist = true;
            private bool isModelDifferent = true;
            private bool isVertexCountMatched = true;
            private bool isSourceBlendshapeCountNotZero = true;

            //Operational variables
            private bool last_useMeshField = true;
            private SkinnedMeshRenderer last_sourceMeshRenderer;
            private SkinnedMeshRenderer last_targetMeshRenderer;
            private List<BlendshapeData> blendshapeDataList = new List<BlendshapeData>();
            private Vector2 scrollPos = Vector2.zero;
            private int includedBlendshapeCount = 0;

            //Editor custom settings
            private float labelWidth = 90f;
            private Color colorWarning = Color.yellow;



            public override int GetScriptPriority()
            {
                return 10;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(500f, 280f);
            }

            public override string GetWindowTitle()
            {
                return "Transfer";
            }

            public override string GetWindowHelpLink()
            {
                return "https://www.youtube.com/@ReForgeMode";
            }

            private void OnEnable()
            {
                //Set Custom GUI styles
                Utilities.CustomGUI.SetGUIStyles();

                //Find Find Asset
                referenceModel = (GameObject)Utilities.SearchAndFind.FindAsset("GameObject", "Reference Model for v1.27.0", ".prefab");
                referenceMeshRenderer = referenceModel.transform.Find("Face").GetComponent<SkinnedMeshRenderer>();
            }

            public override void PrepareThisPage()
            {
                OnEnable(); 
            }


            public void CheckFieldsUpdate()
            {
                if (useMeshField == last_useMeshField && sourceMeshRenderer == last_sourceMeshRenderer &&
                                                         targetMeshRenderer == last_targetMeshRenderer)
                    return;

                //Swap the original Target mesh with a backup
                if (targetMesh != null)
                {
                    SaveModifiedMesh();
                    targetMesh = targetMeshRenderer.sharedMesh;
                }

                //Update...
                GetBlendshapeList();



                last_useMeshField = useMeshField;
                last_sourceMeshRenderer = sourceMeshRenderer;
                last_targetMeshRenderer = targetMeshRenderer;
            }

            public override void DisplayPageContent()
            {
                FieldValidation();

                CheckFieldsUpdate();
                CheckMassSelectUpdate();
                CheckReferenceModelState();

                DisplaySourceField();
                DisplayTargetField();

                EditorGUILayout.Space(5f);

                DisplayButton();

                EditorGUILayout.Space(5f);
                DisplayAdvancedSettings();

                DisplayFieldGuide();
            }

            private void DisplaySourceField()
            {
                //Add a debug highlight to this field
                if (isFieldNotNull == true)
                {
                    //For normal mode, do full checks
                    if (useMeshField == false)
                    {
                        if (!isSourceFaceExist || !isSourceSkinnedMeshExist ||
                            !isSourceMeshExist || !isModelDifferent || !isVertexCountMatched ||
                            !isSourceBlendshapeCountNotZero)
                        {
                            GUI.color = colorWarning;
                        }
                    }
                    //For advanced mode, skip some checks
                    else if (!isSourceMeshExist || !isModelDifferent || !isVertexCountMatched ||
                             !isSourceBlendshapeCountNotZero)
                    {
                        GUI.color = colorWarning;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                {
                    if (useMeshField == false)
                    {
                        string tooltip = "Drag in your character model here from the Hierarchy Window.";
                        EditorGUILayout.LabelField(new GUIContent("Source Model", tooltip), GUILayout.Width(labelWidth));

                        EditorGUI.BeginDisabledGroup(useReferenceModel);
                        sourceModel = (GameObject)EditorGUILayout.ObjectField(sourceModel, typeof(GameObject), true, GUILayout.ExpandWidth(true));
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        string tooltip = "Drag in body parts of your character model here (Face, Body, or Hair). " +
                                         "That part must have a SkinnedMeshRenderer component attached.";
                        EditorGUILayout.LabelField(new GUIContent("Source Mesh", tooltip), GUILayout.Width(labelWidth));

                        EditorGUI.BeginDisabledGroup(useReferenceModel);
                        sourceMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(sourceMeshRenderer, typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
                        EditorGUI.EndDisabledGroup();
                    }

                    //Add options for reference model and assign the reference model when activated
                    EditorGUILayout.LabelField("", GUILayout.Width(1f));
                    useReferenceModel = EditorGUILayout.Toggle("", useReferenceModel, GUILayout.Width(15f));

                    string tooltip2 = "Use the Reference Model as the Source Model. " +
                                      "The Reference Model contains all 52 blendshape kit plus additional blendshapes custom made by yours truly. " +
                                      "This does require you to use model made by VRoid Studio version 1.27 or later.";
                    EditorGUILayout.LabelField(new GUIContent("Use Reference Model", tooltip2), GUILayout.Width(122f));
                    EditorGUILayout.LabelField("", GUILayout.Width(5f));
                }
                EditorGUILayout.EndHorizontal();

                GUI.color = Color.white;
            }

            private void DisplayTargetField()
            {
                //Add a debug highlight to this field
                if (isFieldNotNull == true)
                {
                    //For normal mode, do full checks
                    if (useMeshField == false)
                    {
                        if (!isTargetFaceExist || !isTargetSkinnedMeshExist ||
                            !isTargetMeshExist || !isModelDifferent || !isVertexCountMatched)
                        {
                            GUI.color = colorWarning;
                        }
                    }
                    //For advanced mode, skip some checks
                    else if (!isTargetMeshExist || !isModelDifferent || !isVertexCountMatched)
                    {
                        GUI.color = colorWarning;
                    }
                }


                EditorGUILayout.BeginHorizontal();
                if (useMeshField == false)
                {
                    string tooltip = "Drag in your character model here from the Hierarchy Window.";
                    EditorGUILayout.LabelField(new GUIContent("Target Model", tooltip), GUILayout.Width(labelWidth));
                    targetModel = (GameObject)EditorGUILayout.ObjectField(targetModel, typeof(GameObject), true, GUILayout.ExpandWidth(true));
                }
                else
                {
                    string tooltip = "Drag in body parts of your character model here (Face, Body, or Hair). " +
                                     "That part must have a SkinnedMeshRenderer component attached.";
                    EditorGUILayout.LabelField(new GUIContent("Target Mesh", tooltip), GUILayout.Width(labelWidth));
                    targetMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(targetMeshRenderer, typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
                }
                EditorGUILayout.EndHorizontal();

                GUI.color = Color.white;
            }

            private void DisplayButton()
            {
                EditorGUI.BeginDisabledGroup(!isCopyButtonEnabled);
                {
                    string tooltip = "Click here to copy the blendshape! This button won't be available until all errors are fixed.";
                    if (GUILayout.Button(new GUIContent("Copy Blendshapes", tooltip), GUILayout.Height(50)))
                    {
                        CopyBlendshape();
                        SaveModifiedMesh();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }

            private void DisplayAdvancedSettings()
            {
                EditorGUILayout.BeginHorizontal();
                string tooltip1 = "Replace the Source and Target field with SkinnedMeshRenderer field. " +
                                  "This will allow you to transfer blendshape from body parts that are not limited to just Faces.";
                EditorGUILayout.LabelField(new GUIContent("Use Mesh Renderer Field", tooltip1), GUILayout.Width(150f));
                useMeshField = EditorGUILayout.Toggle(useMeshField, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                string tooltip2 = "Show additional options for more advanced use cases.";
                EditorGUILayout.LabelField(new GUIContent("Show Advanced Settings", tooltip2), GUILayout.Width(150f));
                isAdvancedMode = EditorGUILayout.Toggle(isAdvancedMode, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                if (isAdvancedMode == true && isCopyButtonEnabled == true)
                {
                    EditorGUILayout.Space(20f);

                    //Catch the weird GUILayout error with Scroll View
                    try
                    {
                        DisplayBlendshapeListItem();
                    }
                    catch (ArgumentException)
                    {
                        //Nothing
                    }
                    
                    EditorGUILayout.Space(10f);
                    DisplayMassSelectionOption();

                    if (displayDebug == true)
                    {
                        EditorGUILayout.Space(20f);
                        Debug_DisplayListItem();
                        Debug_DisplayListOption();
                    }
                }
            }

            



            /// <summary>
            /// Check when useReferenceModel value changes. 
            /// Assign or unassign Reference Model to the Source field.
            /// </summary>
            private void CheckReferenceModelState()
            {
                //Usual check for value changes type of function.
                if (useReferenceModel == last_useReferenceModel) return;

                //Assign reference model on both GameObject field and SkinnedMeshRenderer field.
                if (useMeshField == false)
                {
                    sourceModel = useReferenceModel ? referenceModel : null;
                }
                else
                {
                    sourceMeshRenderer = useReferenceModel ? referenceMeshRenderer : null;
                }

                last_useReferenceModel = useReferenceModel;
            }

            private int CopyBlendshape()
            {
                sourceMesh = sourceMeshRenderer.sharedMesh;
                targetMesh = targetMeshRenderer.sharedMesh;

                Mesh backupMesh = Instantiate(targetMesh);
                targetMesh.ClearBlendShapes();

                Vector3[] deltaVertices = new Vector3[sourceMesh.vertexCount];
                Vector3[] deltaNormals = new Vector3[sourceMesh.vertexCount];
                Vector3[] deltaTangents = new Vector3[sourceMesh.vertexCount];

                //For every blendshapes...
                for (int shapeIndex = 0; shapeIndex < blendshapeDataList.Count; shapeIndex++)
                {
                    //Skip any skipped blendshapes
                    if (blendshapeDataList[shapeIndex].isIncluded == false)
                    {
                        //If it's new blendshapes from the source, skip it entirely.
                        if (blendshapeDataList[shapeIndex].presenceInfo == PresenceInfo.SourceOnly)
                        {
                            continue;
                        }
                        else
                        {
                            //Otherwise, for both TargetOnly and Both sources, set it immediately to prefer target.
                            blendshapeDataList[shapeIndex].preferredSource = BlendshapeSource.Target;
                        }
                    }



                    if (blendshapeDataList[shapeIndex].preferredSource == BlendshapeSource.Source)
                    {
                        string shapeName = blendshapeDataList[shapeIndex].blendshapeName;
                        int sourceMeshIndex = blendshapeDataList[shapeIndex].sourceMeshIndex;

                        //Copy across the keyframes in the blendshape (most have 1 keyframe).
                        int frameCount = sourceMesh.GetBlendShapeFrameCount(sourceMeshIndex);
                        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                        {
                            float frameWeight = sourceMesh.GetBlendShapeFrameWeight(sourceMeshIndex, frameIndex);
                            try
                            {
                                sourceMesh.GetBlendShapeFrameVertices(sourceMeshIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
                                targetMesh.AddBlendShapeFrame(shapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                            }
                            catch (ArgumentException ex)
                            {
                                //Handle the exception here
                                Debug.LogError("The number of Source and Target models vertices don't match! " +
                                               "This is because the model was exported from VRoid Studio with <b>\"Delete Transparent Meshes\"</b> option checked. " +
                                               "This option is enabled by default in the <b>\"Reduce Polygon\"</b> export settings. " +
                                               "Make sure to uncheck it first and export the model again into this Unity project!\n\n" + ex.Message);

                                // Stop the function on error
                                return 0;
                            }
                        }
                    }
                    else if (blendshapeDataList[shapeIndex].preferredSource == BlendshapeSource.Target)
                    {
                        string shapeName = blendshapeDataList[shapeIndex].blendshapeName;
                        int targetMeshIndex = blendshapeDataList[shapeIndex].targetMeshIndex;

                        //Copy across the keyframes in the blendshape (most have 1 keyframe).
                        int frameCount = backupMesh.GetBlendShapeFrameCount(targetMeshIndex);
                        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                        {
                            float frameWeight = backupMesh.GetBlendShapeFrameWeight(targetMeshIndex, frameIndex);
                            try
                            {
                                backupMesh.GetBlendShapeFrameVertices(targetMeshIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
                                targetMesh.AddBlendShapeFrame(shapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                            }
                            catch (ArgumentException ex)
                            {
                                //Handle the exception here
                                Debug.LogError("The number of Source and Target models vertices don't match! " +
                                               "This is because the model was exported from VRoid Studio with <b>\"Delete Transparent Meshes\"</b> option checked. " +
                                               "This option is enabled by default in the <b>\"Reduce Polygon\"</b> export settings. " +
                                               "Make sure to uncheck it first and export the model again into this Unity project!\n\n" + ex.Message);

                                // Stop the function on error
                                return 0;
                            }
                        }
                    }
                }

                ////Re-add the Target blendshape
                //for (int shapeIndex = 0; shapeIndex < blendshapeDataList.Count; shapeIndex++)
                //{
                //    //Skip any non-Target blendshapes
                //    if (blendshapeDataList[shapeIndex].presenceInfo == PresenceInfo.TargetOnly)
                //    {
                //        string shapeName = blendshapeDataList[shapeIndex].blendshapeName;
                //        int targetMeshIndex = blendshapeDataList[shapeIndex].targetMeshIndex;

                //        //Copy across the keyframes in the blendshape (most have 1 keyframe).
                //        int frameCount = backupMesh.GetBlendShapeFrameCount(targetMeshIndex);
                //        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                //        {
                //            Debug.Log(shapeName);

                //            float frameWeight = backupMesh.GetBlendShapeFrameWeight(targetMeshIndex, frameIndex);
                //            backupMesh.GetBlendShapeFrameVertices(targetMeshIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
                //            targetMesh.AddBlendShapeFrame(shapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                //        }
                //    }
                //}

                //Assign the mesh to the object
                targetMeshRenderer.sharedMesh = Instantiate(targetMesh);

                EditorUtility.DisplayDialog("Success!", includedBlendshapeCount + " blendshapes have been copied successfully!", "Yay!");

                //Force refresh the data in the window
                GetBlendshapeList();

                return 1;
            }

            private void SaveModifiedMesh()
            {
                //Don't save it if it doesn't exist!
                if (targetMeshRenderer == null) return;

                //Find the mesh asset path using the instantiated object
                string meshPath = AssetDatabase.GetAssetPath(targetMesh);
                if (string.IsNullOrEmpty(meshPath))
                {
                    Debug.LogError("Invalid mesh path! Mesh is not an Asset! It might be an uninstantiated copy");
                    return;
                }
                string folderPath = System.IO.Path.GetDirectoryName(meshPath);
                string objectName = targetMeshRenderer.gameObject.name;
                string fileName = objectName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string path = Path.Combine(folderPath, fileName + ".asset");

                if (!string.IsNullOrEmpty(path))
                {
                    //Instantiate a new mesh
                    Mesh newMesh = Instantiate(targetMesh);

                    //Create the asset and save it
                    AssetDatabase.CreateAsset(newMesh, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    //Register the modified mesh for undo
                    Undo.RegisterCompleteObjectUndo(targetMeshRenderer, "Modify Mesh");

                    //Assign the new mesh to the renderer
                    targetMeshRenderer.sharedMesh = newMesh;
                }
                else
                {
                    Debug.LogError("Invalid path for saving the asset.");
                }
            }



            #region Display Error Messages
            /// <summary>
            /// Perform all the checks needed to display field error messages.
            /// </summary>
            private void FieldValidation()
            {
                //Pre-assume that there will be something wrong (so the function can quit early)
                isCopyButtonEnabled = false;

                //Reset validation to default, pre-assuming everything is true.
                isFieldNotNull = true;
                isSourceFaceExist = true;
                isTargetFaceExist = true;
                isSourceSkinnedMeshExist = true;
                isTargetSkinnedMeshExist = true;
                isSourceMeshExist = true;
                isTargetMeshExist = true;
                isModelDifferent = true;
                isVertexCountMatched = true;
                isSourceBlendshapeCountNotZero = true;

                //Check if any of the field is null
                if (useMeshField == false)
                {
                    isFieldNotNull = (sourceModel != null) && (targetModel != null);
                }
                else
                {
                    isFieldNotNull = (sourceMeshRenderer != null) && (targetMeshRenderer != null);
                }
                if (isFieldNotNull == false) return;

                if (useMeshField == false)
                {
                    //Does the Source Face exist?
                    Transform sourceFace = sourceModel.transform.Find("Face");
                    isSourceFaceExist = (sourceFace != null);
                    if (isSourceFaceExist == false) return;

                    //Does the Target Face exist?
                    Transform targetFace = targetModel.transform.Find("Face");
                    isTargetFaceExist = (targetFace != null);
                    if (isTargetFaceExist == false) return;

                    //Does the Source's Part has a SkinnedMeshRenderer component?
                    sourceMeshRenderer = sourceFace.GetComponent<SkinnedMeshRenderer>();
                    isSourceSkinnedMeshExist = (sourceMeshRenderer != null);
                    if (isSourceSkinnedMeshExist == false) return;

                    //Does the Target's Part has a SkinnedMeshRenderer component?
                    targetMeshRenderer = targetFace.GetComponent<SkinnedMeshRenderer>();
                    isTargetSkinnedMeshExist = (targetMeshRenderer != null);
                    if (isTargetSkinnedMeshExist == false) return;
                }

                //Does the Source's Face has a mesh assigned in the SkinnedMeshRenderer component?
                sourceMesh = sourceMeshRenderer.sharedMesh;
                isSourceMeshExist = (sourceMesh != null);
                if (isSourceMeshExist == false) return;

                //Does the Target's Face has a mesh assigned in the SkinnedMeshRenderer component?
                targetMesh = targetMeshRenderer.sharedMesh;
                isTargetMeshExist = (targetMesh != null);
                if (isTargetMeshExist == false) return;

                //This doesn't allow the reference model to be used in the input field
                isModelDifferent = (sourceMeshRenderer != targetMeshRenderer);
                if (isModelDifferent == false) return;

                //Does the Target's Face has different mesh count with the Reference Model?
                //This error can be caused because of beta/v1.x VRoid Studio model
                //or if "Delete Transparent Meshes" option checked
                isVertexCountMatched = (sourceMesh.vertexCount == targetMesh.vertexCount);
                if (isVertexCountMatched == false) return;

                //Does the Source model has blendshapes in it?
                isSourceBlendshapeCountNotZero = (sourceMesh.blendShapeCount > 0);
                if(isSourceBlendshapeCountNotZero == false) return;

                //No problem left!
                isCopyButtonEnabled = true;
            }

            /// <summary>
            /// Find the correct guide message that corresponds to the checks done in Field Validation.
            /// </summary>
            /// <returns></returns>
            private string FindGuideMessage()
            {
                string errorMessage;

                if (isFieldNotNull == false)
                {
                    errorMessage = "\nThis is a tool to copy blendshapes between two models. \n" +
                                   "To get started, drag in your Source and Target models into these fields.\n\n" +
                                   "Hover your mouse over any fields to see additional details.\n";
                    return errorMessage;
                }

                if (isSourceFaceExist == false)
                {
                    errorMessage = "\nError! The Source model doesn't have a Face child game object! \n\n" +
                                   "Make sure you drag in the model itself, not the Face gameobject! \n " +
                                   "Or specify using \"Other\" option in the \"Copy From\" drop down menu\n";
                    return errorMessage;
                }

                if (isSourceSkinnedMeshExist == false)
                {
                    errorMessage = "\n\nError! The Source model's Face doesn't have a SkinnedMeshRenderer component.\n\n" +
                                   "Make sure you drag in a model that contains \n" +
                                   "Face child gameobject with SkinnedMeshRenderer component!\n";
                    return errorMessage;
                }

                if (isSourceMeshExist == false)
                {
                    errorMessage = "\n\nError! The Source SkinnedMeshRenderer doesn't have any mesh assigned in it! \n" +
                                   "Please drag in a model that has the mesh assigned to it!\n\n";
                    return errorMessage;
                }



                if (isTargetFaceExist == false)
                {
                    errorMessage = "\nError! The Target model doesn't have a Face child game object! \n\n" +
                                   "Make sure you drag in the model itself, not the Face gameobject! \n" +
                                   "Or specify using \"Other\" option in the \"Copy From\" drop down menu\n";
                    return errorMessage;
                }

                if (isTargetSkinnedMeshExist == false)
                {
                    errorMessage = "\n\nError! The Target model's Face doesn't have a SkinnedMeshRenderer component.\n\n" +
                                   "Make sure you drag in a model that contains \n" +
                                   "Face child gameobject with SkinnedMeshRenderer component!\n";
                    return errorMessage;
                }

                if (isTargetMeshExist == false)
                {
                    errorMessage = "\n\nError! The Target SkinnedMeshRenderer doesn't have any mesh assigned in it! \n" +
                                   "Please drag in a model that has the mesh assigned to it!\n\n";
                    return errorMessage;
                }



                if (isModelDifferent == false)
                {
                    errorMessage = "\n\nError! Don't use the same model for both Source and Target field!\n\n\n";
                    return errorMessage;
                }

                if (isVertexCountMatched == false)
                {
                    errorMessage = "\nError! Vertex count mismatch between Source and Target models! \n" +
                                   "(" + sourceMesh.vertexCount + " polycount vs " + targetMesh.vertexCount + " polygon" + ") \n\n" +
                                   "This is most likely because the model was exported with \"Delete Transparent Meshes\". \n" +
                                   "This option is enabled by default in the \"Reduce Polygon\" VRoid Studio export settings. \n" +
                                   "Make sure to uncheck it and export the model again into this Unity project. \n" +
                                   "It could also because you're using VRoid Beta model on export.";
                    return errorMessage;
                }



                if (isSourceBlendshapeCountNotZero == false)
                {
                    errorMessage = "Error! There is no blendshape in Source model!\n" +
                                   "There's no blendshape to copy to the Target model!";
                    return errorMessage;
                }


                //Count the included blendshape
                includedBlendshapeCount = 0;
                for (int i = 0; i < blendshapeDataList.Count; i++)
                {
                    if (blendshapeDataList[i].presenceInfo == PresenceInfo.TargetOnly) continue;

                    if (blendshapeDataList[i].isIncluded == true)
                        includedBlendshapeCount++;
                }

                errorMessage = "All good! " + includedBlendshapeCount + " Source blendshapes will be copied to the Target model. \n" +
                               "Note that Target model currently has " + targetMesh.blendShapeCount + " blendshapes, \n" +
                               "which will be overwritten based on the options selected. \n\n" +
                               "Click on the Copy button to proceed!";
                return errorMessage;
            }

            /// <summary>
            /// Display the Field Guide message.
            /// </summary>
            private void DisplayFieldGuide()
            {
                string errorMessage = FindGuideMessage();

                //The GUI has different height depending on whether the blendshape list is displayed or not
                float height = (isCopyButtonEnabled && isAdvancedMode) ? 90f : 120f;

                EditorGUILayout.LabelField(errorMessage, Utilities.CustomGUI.guiStyle.centerLower, GUILayout.Height(height));
            }
            #endregion



            #region Advanced Blendshape Selection Menu
            /// <summary>
            /// Contains the instruction about how to copy the final combined list of blendshapes
            /// Like the name of the blendshape, where is the original index of the blendshapes, 
            /// and where is this index come from originally: Source or Target
            /// </summary>
            [System.Serializable]
            public class BlendshapeData
            {
                public string blendshapeName;
                public int sourceMeshIndex;                 //The original index where this blendshape exist in Source model. By default: -1.
                public int targetMeshIndex;                 //The original index where this blendshape exist in Target model. By default: -1.
                public PresenceInfo presenceInfo;           //Information on where this blendshape can be found.
                public BlendshapeSource preferredSource;    //This blendshape will copy from Source or Target.
                public bool isIncluded;                     //This blendshape will be copied to the Target model

                public BlendshapeData(string blendshapeName, PresenceInfo presenceInfo)
                {
                    this.blendshapeName = blendshapeName;
                    this.sourceMeshIndex = -1;
                    this.targetMeshIndex = -1;
                    this.presenceInfo = presenceInfo;
                    this.preferredSource = BlendshapeSource.Source;
                    this.isIncluded = true;
                }
            }

            public enum BlendshapeSource
            {
                Source, Target
            }

            /// <summary>
            /// Information about the original source of this blendshape.
            /// </summary>
            public enum PresenceInfo
            {
                SourceOnly,     //This blendshape only exist in Source mesh
                TargetOnly,     //This blendshape only exist in Target mesh
                Both            //This blendshape exist in both Source and Target mesh
            }

            /// <summary>
            /// Find all blendshapes that exists in Source model and Target model, 
            /// prioritizing blendshapes from the Target model first.
            /// </summary>
            public void GetBlendshapeList()
            {
                //Error handling
                if (sourceMesh == null || targetMesh == null) return;

                //Add the Target blendshapes into the list
                blendshapeDataList = new List<BlendshapeData>();
                for (int i = 0; i < targetMesh.blendShapeCount; i++)
                {
                    var blendshapeName = targetMesh.GetBlendShapeName(i);
                    var presenceInfo = PresenceInfo.TargetOnly;

                    BlendshapeData data = new BlendshapeData(blendshapeName, presenceInfo);
                    data.targetMeshIndex = i;
                    data.preferredSource = BlendshapeSource.Target;

                    blendshapeDataList.Add(data);
                }

                //Now, add in new blendshapes from Source blendshapes
                for (int i = 0; i < sourceMesh.blendShapeCount; i++)
                {
                    //Check if this blendshape is already in the list
                    bool isFoundBoth = false;
                    string blendshapeName = sourceMesh.GetBlendShapeName(i);
                    for (int j = 0; j < blendshapeDataList.Count; j++)
                    {
                        if (blendshapeDataList[j].blendshapeName == blendshapeName)
                        {
                            blendshapeDataList[j].presenceInfo = PresenceInfo.Both;
                            blendshapeDataList[j].preferredSource = BlendshapeSource.Source;
                            blendshapeDataList[j].sourceMeshIndex = i;
                            isFoundBoth = true;
                            break;
                        }
                    }

                    //If it goes through the loop without finding a duplicate...
                    //Then it's a Source only blendshape. Add it into the list too.
                    if (isFoundBoth == false)
                    {
                        var presenceInfo = PresenceInfo.SourceOnly;

                        BlendshapeData data = new BlendshapeData(blendshapeName, presenceInfo);
                        data.sourceMeshIndex = i;
                        data.preferredSource = BlendshapeSource.Source;

                        blendshapeDataList.Add(data);
                    }
                }
            }

            /// <summary>
            /// Display all the scrolling blendshape options settings.
            /// </summary>
            private void DisplayBlendshapeListItem()
            {
                string notification = "Found " + sourceMesh.blendShapeCount + " blendshapes that is going to added to Target model.\n" +
                                      "Please choose carefully which blendshape will be copied.";
                EditorGUILayout.LabelField(notification, Utilities.CustomGUI.guiStyle.centerMiddle, GUILayout.Height(20f));

                //Display mass selection option
                EditorGUILayout.Space(10f);

                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true));
                for (int i = 0; i < blendshapeDataList.Count; i++)
                {
                    //We don't want to display any blendshapes exclusively from the Target mesh.
                    if (blendshapeDataList[i].presenceInfo == PresenceInfo.TargetOnly) continue;

                    GUILayout.BeginHorizontal();
                    {
                        blendshapeDataList[i].isIncluded = EditorGUILayout.Toggle("", blendshapeDataList[i].isIncluded, GUILayout.Width(15f));

                        EditorGUI.BeginDisabledGroup(!blendshapeDataList[i].isIncluded);
                        if (blendshapeDataList[i].presenceInfo == PresenceInfo.Both)
                        {
                            EditorGUILayout.LabelField(blendshapeDataList[i].blendshapeName, GUILayout.MinWidth(25f), GUILayout.ExpandWidth(true));

                            string tooltip = "This blendshape exist in both Source and Target model. " +
                                             "If you choose Source, then the existing blendshape in the Target model will be overwritten. " +
                                             "If you choose Target, then the existing blendshape in the Target model will remain as is.";
                            EditorGUILayout.LabelField(new GUIContent("Priority: ", tooltip), Utilities.CustomGUI.guiStyle.rightMiddle, GUILayout.Width(100f));
                            blendshapeDataList[i].preferredSource = (BlendshapeSource)EditorGUILayout.EnumPopup("", blendshapeDataList[i].preferredSource, GUILayout.Width(75f));
                        }
                        else if (blendshapeDataList[i].presenceInfo == PresenceInfo.SourceOnly)
                        {
                            EditorGUILayout.LabelField(blendshapeDataList[i].blendshapeName + " - New!", GUILayout.MinWidth(25f), GUILayout.ExpandWidth(true));
                            EditorGUILayout.LabelField("", GUILayout.Width(100f));
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            #endregion



            #region Display Mass Selection Options
            private bool massSelectionIncluded = false;
            private bool last_massSelectionIncluded = false;
            private BlendshapeSource massSelectionSource = BlendshapeSource.Source;
            private BlendshapeSource last_massSelectionSource = BlendshapeSource.Source;

            /// <summary>
            /// Check for changes to all Mass Selection variables. 
            /// If there is a change, apply it to all blendshape items in the list.
            /// </summary>
            private void CheckMassSelectUpdate()
            {
                if (blendshapeDataList.Count == 0) return;

                if(massSelectionIncluded != last_massSelectionIncluded)
                {
                    SetAllBlendshapeList(massSelectionIncluded);
                }

                if (massSelectionSource != last_massSelectionSource)
                {
                    SetAllBlendshapeList(massSelectionSource);
                }

                last_massSelectionIncluded = massSelectionIncluded;
                last_massSelectionSource = massSelectionSource;
            }

            private void DisplayMassSelectionOption()
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    string tooltip = "Mass selection option. Click here to apply all settings to all items on the list when applicable.";
                    EditorGUILayout.LabelField(new GUIContent("Select All: ", tooltip), GUILayout.Width(60f));
                    massSelectionIncluded = EditorGUILayout.Toggle("", massSelectionIncluded, GUILayout.Width(15f));
                    massSelectionSource = (BlendshapeSource)EditorGUILayout.EnumPopup("", massSelectionSource, GUILayout.Width(75f));
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// Set all items in blendshape list to be included in the list.
            /// </summary>
            /// <param name="isIncluded"></param>
            private void SetAllBlendshapeList(bool isIncluded)
            {
                if (blendshapeDataList.Count == 0) return;

                for (int i = 0; i < blendshapeDataList.Count; i++)
                {
                    //We will always include the Target only blendshape
                    if (blendshapeDataList[i].presenceInfo == PresenceInfo.TargetOnly)
                    {
                        blendshapeDataList[i].isIncluded = true;
                        continue;
                    }

                    blendshapeDataList[i].isIncluded = isIncluded;
                }
            }

            /// <summary>
            /// Set all items in blendshape list to one source.
            /// </summary>
            /// <param name="blendshapeSource"></param>
            private void SetAllBlendshapeList(BlendshapeSource preferredSource)
            {
                if (blendshapeDataList.Count == 0) return;

                for (int i = 0; i < blendshapeDataList.Count; i++)
                {
                    //We don't want to change any blendshapes exclusively from the Target mesh.
                    if (blendshapeDataList[i].presenceInfo == PresenceInfo.TargetOnly) continue;

                    //We also don't want to change any blendshape from Source Only mesh,
                    //since that will mess up where the source is taken from
                    if (blendshapeDataList[i].presenceInfo == PresenceInfo.SourceOnly) continue;

                    blendshapeDataList[i].preferredSource = preferredSource;
                }
            }

            #endregion

            

            #region Debug Option for Display Blendshape List
            private bool displayDebug = false;
            private bool debugToggleFilter_SourceOnly = true;
            private bool debugToggleFilter_TargetOnly = false;
            private bool debugToggleFilter_Both = true;
            private Vector2 scrollPosDebug;

            /// <summary>
            /// Display the filter setting to show different blendshape list
            /// </summary>
            private void Debug_DisplayListOption()
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.LabelField("Debug Option: ", GUILayout.Width(82f));

                    EditorGUILayout.LabelField("", GUILayout.Width(5f));
                    EditorGUILayout.LabelField("Show SourceOnly: ", GUILayout.Width(106f));
                    debugToggleFilter_SourceOnly = EditorGUILayout.Toggle("", debugToggleFilter_SourceOnly, GUILayout.Width(15f));

                    EditorGUILayout.LabelField("", GUILayout.Width(5f));
                    EditorGUILayout.LabelField("Show TargetOnly: ", GUILayout.Width(105f));
                    debugToggleFilter_TargetOnly = EditorGUILayout.Toggle("", debugToggleFilter_TargetOnly, GUILayout.Width(15f));

                    EditorGUILayout.LabelField("", GUILayout.Width(5f));
                    EditorGUILayout.LabelField("Show Both: ", GUILayout.Width(68f));
                    debugToggleFilter_Both = EditorGUILayout.Toggle("", debugToggleFilter_Both, GUILayout.Width(15f));

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// Display the full blendshape data list with filter for different presence status.
            /// </summary>
            private void Debug_DisplayListItem()
            {
                scrollPosDebug = GUILayout.BeginScrollView(scrollPosDebug, GUILayout.ExpandWidth(true), GUILayout.Height(100f));
                for (int i = 0; i < blendshapeDataList.Count; i++)
                {
                    if (debugToggleFilter_SourceOnly == false && blendshapeDataList[i].presenceInfo == PresenceInfo.SourceOnly) continue;
                    if (debugToggleFilter_TargetOnly == false && blendshapeDataList[i].presenceInfo == PresenceInfo.TargetOnly) continue;
                    if (debugToggleFilter_Both == false && blendshapeDataList[i].presenceInfo == PresenceInfo.Both) continue;

                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(blendshapeDataList[i].blendshapeName, GUILayout.MinWidth(25f), GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField("", GUILayout.Width(100f));
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

            #endregion

            //showAdvancedMenu = EditorGUILayout.Foldout(showAdvancedMenu, "Advanced Settings");
            //    if (showAdvancedMenu)
            //    {
            //        EditorGUI.indentLevel++;

            //        EditorGUI.indentLevel--;
            //    }
        }
    }
}
#endif