using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Mathematics;

namespace BezierCurve
{
    public partial class Curve
    {
        [SerializeField] Color handleLineColor = Color.white;
        [SerializeField] Color curveColor = Color.green;
        [SerializeField] Color anchorColor = Color.black;
        [SerializeField] Color anchorHighlightColor = Color.yellow;
        [SerializeField] Color handleColor = Color.white;
        [SerializeField] Color normalColor = Color.green;
        [SerializeField, Min(.05f)] float anchorSize = .1f;
        [SerializeField, Min(.05f)] float handleSize = .1f;
        [SerializeField, Min(2)] int lineCountPerSegment = 32;
        [SerializeField] bool drawNormals = false;
        [SerializeField] float normalsPreviewLength = 1;
        [SerializeField, Min(0)] int lerpFrameCount = 0;
        const float mouseHighlightDistanceMultiplier = .3f;

        private void OnValidate()
        {
            InitCache();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = curveColor;
            for (int i = 0; i < SegmentCount; i++)
            {
                var segment = GetSegmentAtIndex(i);
                float3 start = segment.Position(0);
                for (int j = 1; j < lineCountPerSegment; j++)
                {
                    float t = (float)j / (lineCountPerSegment - 1);
                    float3 end = segment.Position(t);
                    Gizmos.DrawLine(start, end);
                    start = end;
                }
            }
        }

        [CustomEditor(typeof(Curve))]
        public class Curve_Editor : Editor
        {
            Curve curve;

            private void OnEnable()
            {
                curve = target as Curve;
            }

            public override VisualElement CreateInspectorGUI()
            {
                var so = serializedObject;
                var root = new VisualElement();

                var loopField = new PropertyField(so.FindProperty(nameof(isLoop)));
                loopField.RegisterValueChangeCallback(InitCache);
                root.Add(loopField);

                var autoSetHandlesField = new PropertyField(so.FindProperty(nameof(autoSetHandles)));
                autoSetHandlesField.RegisterValueChangeCallback((e) =>
                {
                    PerformStructuralChange("Auto set handles", curve.AutoSetHandles);
                });
                root.Add(autoSetHandlesField);

                var framesPerSegmentField = new PropertyField(so.FindProperty(nameof(cacheFramesPerSegment)));
                framesPerSegmentField.RegisterValueChangeCallback(InitCache);
                root.Add(framesPerSegmentField);

                var resetButton = new Button(() =>
                {
                    PerformStructuralChange("Reset curve", curve.Reset);
                });
                resetButton.text = "Reset curve";
                root.Add(resetButton);

                //var anchorsArrayField = new PropertyField(so.FindProperty(nameof(anchors)));
                //root.Add(anchorsArrayField);

                var foldout = new Foldout();
                foldout.value = false;
                foldout.text = "Preview Settings";
                foldout.Add(new PropertyField(so.FindProperty(nameof(drawNormals)), "Draw Normals"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(normalsPreviewLength)), "Normals Preview Length"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(lerpFrameCount)), "Lerp Frame Count"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(handleLineColor)), "Handle Lines Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(curveColor)), "Curve Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(anchorColor)), "Anchor Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(anchorHighlightColor)), "Anchor Highlight Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(handleColor)), "Handle Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(normalColor)), "Normal Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(anchorSize)), "Anchor Size"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(handleSize)), "Handle Size"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(lineCountPerSegment)), "Count Per Segment"));
                root.Add(foldout);

                return root;

                void InitCache(EventBase evt)
                {
                    curve.InitCache();
                }
            }

            void OnSceneGUI()
            {
                for (int i = 0; i < curve.AnchorCount; i++)
                {
                    var anchor = curve[i];
                    if (curve.drawNormals) { DrawNormals(); }
                    DrawLerpedNormals();
                    if (!curve.autoSetHandles)
                    {
                        DrawHandleLines(anchor);
                        if (DrawAndEditBackHandle(ref anchor) || DrawAndEditFrontHandle(ref anchor))
                        {
                            PerformStructuralChange("Edit curve handles", () => { curve[i] = anchor; });
                        }
                    }
                    if (DrawAndEditAnchor(ref anchor))
                    {
                        PerformStructuralChange("Edit curve anchor", () => { curve[i] = anchor; });
                    }
                }
                CheckForAdd();
                CheckForDelete();
            }

            void PerformStructuralChange(string actionDescription, System.Action action)
            {
                Undo.RecordObject(target, actionDescription);
                action();
                curve.InitCache();
                SceneView.RepaintAll();
            }

            void DrawHandleLines(Curve.Anchor anchor)
            {
                Handles.color = curve.handleLineColor;
                Handles.DrawLine(anchor.Position, anchor.BackHandle);
                Handles.DrawLine(anchor.Position, anchor.FrontHandle);
            }

            bool DrawAndEditAnchor(ref Anchor anchor)
            {
                Handles.color = curve.anchorColor;
                float worldSize = HandleUtility.GetHandleSize(anchor.Position);
                float3 pos = Handles.FreeMoveHandle(anchor.Position, Quaternion.identity, worldSize * curve.anchorSize, Vector3.zero, Handles.SphereHandleCap);
                if (!anchor.Position.Equals(pos))
                {
                    anchor.Position = pos;
                    return true;
                }
                return false;
            }

            bool DrawAndEditBackHandle(ref Anchor anchor)
            {
                Handles.color = curve.handleColor;
                float worldSize = HandleUtility.GetHandleSize(anchor.BackHandle);
                float3 pos = Handles.FreeMoveHandle(anchor.BackHandle, Quaternion.identity, worldSize * curve.handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (!anchor.BackHandle.Equals(pos))
                {
                    anchor.BackHandle = pos;
                    return true;
                }
                return false;
            }

            bool DrawAndEditFrontHandle(ref Anchor anchor)
            {
                Handles.color = curve.handleColor;
                float worldSize = HandleUtility.GetHandleSize(anchor.FrontHandle);
                float3 pos = Handles.FreeMoveHandle(anchor.FrontHandle, Quaternion.identity, worldSize * curve.handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (!anchor.FrontHandle.Equals(pos))
                {
                    anchor.FrontHandle = pos;
                    return true;
                }
                return false;
            }

            void DrawNormals()
            {
                for (int i = 0; i < curve.Cache.FrameCount; i++)
                {
                    DrawNormal(curve.Cache.GetFrameAtIndex(i));
                }
            }

            void DrawLerpedNormals()
            {
                for (int i = 0; i < curve.lerpFrameCount; i++)
                {
                    float t = (float)i / (curve.lerpFrameCount - 1);
                    DrawNormal(curve.Cache.GetFrameAtTime(t));
                }
            }

            void DrawNormal(Frame frame)
            {
                Handles.color = curve.normalColor;
                Handles.DrawLine(frame.position, frame.position + frame.normal * curve.normalsPreviewLength);
            }

            void CheckForDelete()
            {
                Event guiEvent = Event.current;
                if (curve.AnchorCount > 2 && guiEvent.control && !guiEvent.shift)
                {
                    Handles.color = curve.anchorHighlightColor;
                    Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                    int nearestAnchorIndex = curve.GetIndexOfNearestAnchor(ray);
                    var anchor = curve.anchors[nearestAnchorIndex];
                    float worldSize = HandleUtility.GetHandleSize(anchor.Position);
                    if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                    {
                        Debug.Log("clicked to delete");
                        PerformStructuralChange("Delete anchor", () => { curve.DeleteAnchor(nearestAnchorIndex); });
                    }
                    Handles.FreeMoveHandle(anchor.Position, Quaternion.identity, worldSize * curve.anchorSize, Vector3.zero, Handles.SphereHandleCap);
                    SceneView.RepaintAll();
                }
            }

            void CheckForAdd()
            {
                Event guiEvent = Event.current;
                if (guiEvent.shift && !guiEvent.control)
                {
                    Handles.color = curve.anchorHighlightColor;
                    Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                    var p = curve.ProjectRay(ray, 10);
                    float3 pos;
                    float highlightDistance = HandleUtility.GetHandleSize(p.position) * mouseHighlightDistanceMultiplier;
                    if (p.projectionDistance < highlightDistance)
                    {
                        pos = p.position;
                        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                        {
                            PerformStructuralChange("Add anchor", () => { curve.InsertAnchor(pos, p.curveTime); });
                            guiEvent.Use();
                        }
                    }
                    else
                    {
                        pos = ray.Projection(p.position);
                        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                        {
                            PerformStructuralChange("Add anchor", () => { curve.AddAnchor(pos); });
                            guiEvent.Use();
                        }
                    }
                    float worldSize = HandleUtility.GetHandleSize(pos);
                    Handles.FreeMoveHandle(pos, Quaternion.identity, worldSize * curve.anchorSize, Vector3.zero, Handles.SphereHandleCap);
                    SceneView.RepaintAll();
                }
            }
        }
    }
}