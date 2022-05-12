using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace BezierCurveDemo
{
    public partial class PathBehaviour
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawGizmos(PathBehaviour behaviour, GizmoType gizmoType)
        {
            Path path = behaviour.path;
            for (int i = 0; i < path.SegmentCount; i++)
            {
                var s = path.GetSegmentAtIndex(i);
                Handles.DrawBezier(s.points[0], s.points[3], s.points[1], s.points[2], Color.green, null, 3);
            }
        }

        [CustomEditor(typeof(PathBehaviour))]
        public class PathBehaviour_Editor : Editor
        {
            Path path;

            private void OnEnable()
            {
                path = (target as PathBehaviour).path;
            }

            public override VisualElement CreateInspectorGUI()
            {
                var root = new VisualElement();
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }
        }
    }
}