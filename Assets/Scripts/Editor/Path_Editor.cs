using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Mathematics;

namespace BezierCurveDemo
{
    public partial class Path
    {
        [SerializeField] Color handleLineColor = Color.white;
        [SerializeField] Color curveColor = Color.green;
        [SerializeField] Color anchorColor = Color.black;
        [SerializeField] Color anchorHighlightColor = Color.yellow;
        [SerializeField] Color handleColor = Color.white;
        [SerializeField] Color tangentColor = Color.blue;
        [SerializeField] Color normalColor = Color.green;
        [SerializeField, Min(.05f)] float anchorSize = .1f;
        [SerializeField, Min(.05f)] float handleSize = .1f;
        [SerializeField, Min(2)] int lineCountPerSegment = 32;
        [SerializeField] bool drawFramesInCache;
        [SerializeField, Min(0)] int lerpFrameCount = 0;
        [SerializeField, Min(.01f)] float mouseHighlightMinDistance = .1f;

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

        [CustomEditor(typeof(Path))]
        public class Path_Editor : Editor
        {
            Path path;

            private void OnEnable()
            {
                path = target as Path;
            }

            public override VisualElement CreateInspectorGUI()
            {
                var so = serializedObject;
                var root = new VisualElement();

                var loopField = new PropertyField(so.FindProperty(nameof(isLoop)));
                loopField.RegisterValueChangeCallback(InitCache);
                root.Add(loopField);

                var framesPerSegmentField = new PropertyField(so.FindProperty(nameof(cacheFramesPerSegment)));
                framesPerSegmentField.RegisterValueChangeCallback(InitCache);
                root.Add(framesPerSegmentField);

                var resetButton = new Button(() =>
                {
                    PerformStructuralChange("Reset path", path.Reset);
                });
                resetButton.text = "Reset path";
                root.Add(resetButton);

                var autoSetHandlesButton = new Button(() =>
                {
                    PerformStructuralChange("Auto set handles", path.AutoSetHandles);
                });
                autoSetHandlesButton.text = "Auto Set Handles";
                root.Add(autoSetHandlesButton);

                //var anchorsArrayField = new PropertyField(so.FindProperty(nameof(anchors)));
                //root.Add(anchorsArrayField);

                var foldout = new Foldout();
                foldout.value = false;
                foldout.text = "Preview Settings";
                foldout.Add(new PropertyField(so.FindProperty(nameof(handleLineColor)), "Handle Lines Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(curveColor)), "Curve Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(anchorColor)), "Anchor Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(anchorHighlightColor)), "Anchor Highlight Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(handleColor)), "Handle Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(tangentColor)), "Tangent Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(normalColor)), "Normal Color"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(anchorSize)), "Anchor Size"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(handleSize)), "Handle Size"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(lineCountPerSegment)), "Count Per Segment"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(drawFramesInCache)), "Draw Frames In Cache"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(lerpFrameCount)), "Lerp Frame Count"));
                foldout.Add(new PropertyField(so.FindProperty(nameof(mouseHighlightMinDistance)), "Mouse Highlight Distance"));
                root.Add(foldout);

                return root;

                void InitCache(EventBase evt)
                {
                    path.InitCache();
                }
            }

            void OnSceneGUI()
            {
                for (int i = 0; i < path.AnchorCount; i++)
                {
                    var anchor = path[i];
                    DrawHandleLines(anchor);
                    if (path.drawFramesInCache) { DrawFrames(); }
                    DrawLerpedFrames();
                    if (DrawAndEditAnchor(ref anchor) ||
                        DrawAndEditBackHandle(ref anchor) ||
                        DrawAndEditFrontHandle(ref anchor))
                    {
                        Undo.RecordObject(target, $"Edit path anchor");
                        path[i] = anchor;
                        path.InitCache();
                    }
                    CheckForAndPerformAnchorDelete();
                    CheckForAndPerformAnchorAdd();
                }
            }

            void PerformStructuralChange(string actionDescription, System.Action action)
            {
                Undo.RecordObject(target, actionDescription);
                action();
                path.InitCache();
                SceneView.RepaintAll();
            }

            void PerformStructuralChange<T>(string actionDescription, T value, System.Action<T> action)
            {
                Undo.RecordObject(target, actionDescription);
                action(value);
                path.InitCache();
                SceneView.RepaintAll();
            }

            void DrawHandleLines(Path.Anchor anchor)
            {
                Handles.color = path.handleLineColor;
                Handles.DrawLine(anchor.Position, anchor.BackHandle);
                Handles.DrawLine(anchor.Position, anchor.FrontHandle);
            }

            bool DrawAndEditAnchor(ref Anchor anchor)
            {
                Handles.color = path.anchorColor;
                float worldSize = HandleUtility.GetHandleSize(anchor.Position);
                float3 pos = Handles.FreeMoveHandle(anchor.Position, Quaternion.identity, worldSize * path.anchorSize, Vector3.zero, Handles.SphereHandleCap);
                if (!anchor.Position.Equals(pos))
                {
                    anchor.Position = pos;
                    return true;
                }
                return false;
            }

            bool DrawAndEditBackHandle(ref Anchor anchor)
            {
                Handles.color = path.handleColor;
                float worldSize = HandleUtility.GetHandleSize(anchor.BackHandle);
                float3 pos = Handles.FreeMoveHandle(anchor.BackHandle, Quaternion.identity, worldSize * path.handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (!anchor.BackHandle.Equals(pos))
                {
                    anchor.BackHandle = pos;
                    return true;
                }
                return false;
            }

            bool DrawAndEditFrontHandle(ref Anchor anchor)
            {
                Handles.color = path.handleColor;
                float worldSize = HandleUtility.GetHandleSize(anchor.FrontHandle);
                float3 pos = Handles.FreeMoveHandle(anchor.FrontHandle, Quaternion.identity, worldSize * path.handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (!anchor.FrontHandle.Equals(pos))
                {
                    anchor.FrontHandle = pos;
                    return true;
                }
                return false;
            }

            void DrawFrames()
            {
                for (int i = 0; i < path.Cache.FrameCount; i++)
                {
                    DrawFrame(path.Cache.GetFrameAtIndex(i));
                }
            }

            void DrawLerpedFrames()
            {
                for (int i = 0; i < path.lerpFrameCount; i++)
                {
                    float t = (float)i / (path.lerpFrameCount - 1);
                    DrawFrame(path.Cache.GetFrameAtTime(t));
                }
            }

            void DrawFrame(Frame frame)
            {
                Handles.color = path.tangentColor;
                Handles.DrawLine(frame.position, frame.position + frame.tangent);
                Handles.color = path.normalColor;
                Handles.DrawLine(frame.position, frame.position + frame.normal);
            }

            void CheckForAndPerformAnchorDelete()
            {
                Event guiEvent = Event.current;
                if (guiEvent.control && !guiEvent.shift)
                {
                    Handles.color = path.anchorHighlightColor;
                    Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                    int nearestAnchorIndex = path.GetIndexOfNearestAnchor(ray);
                    var anchor = path.anchors[nearestAnchorIndex];
                    float worldSize = HandleUtility.GetHandleSize(anchor.Position);
                    Handles.FreeMoveHandle(anchor.Position, Quaternion.identity, worldSize * path.anchorSize, Vector3.zero, Handles.SphereHandleCap);
                    if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                    {
                        Debug.Log("clicked to delete");
                        PerformStructuralChange("Delete anchor", nearestAnchorIndex, path.DeleteAnchor);
                    }
                    SceneView.RepaintAll();
                }
            }

            // todo fix this
            void CheckForAndPerformAnchorAdd()
            {
                Event guiEvent = Event.current;
                if (guiEvent.shift && !guiEvent.control)
                {
                    Handles.color = path.anchorHighlightColor;
                    Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                    var rayProjection = path.ProjectRay(ray);
                    float3 pos;
                    if (rayProjection.rayDistance < path.mouseHighlightMinDistance)
                    {
                        pos = rayProjection.position;
                    }
                    else
                    {
                        pos = ray.Projection(rayProjection.position);
                    }
                    float worldSize = HandleUtility.GetHandleSize(pos);
                    Handles.FreeMoveHandle(pos, Quaternion.identity, worldSize * path.anchorSize, Vector3.zero, Handles.SphereHandleCap);
                    if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                    {
                        Debug.Log("clicked to delete");
                        PerformStructuralChange("Add anchor", pos, path.AddAnchor);
                    }
                    SceneView.RepaintAll();
                }
            }
        }
    }
}