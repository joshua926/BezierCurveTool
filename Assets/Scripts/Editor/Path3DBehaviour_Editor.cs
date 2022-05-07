using UnityEngine;
using UnityEditor;

namespace BezierCurve
{
    public partial class Path3DBehaviour
    {
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        public static void DrawGizmo(Path3DBehaviour b, GizmoType gizmoType)
        {
            b.TryCreateFrames();
            var frames = b.frames;
            for (int i = 0; i < frames.Length - 1; i++)
            {
                Gizmos.DrawLine(
                    frames[i].position,
                    frames[i + 1].position);
            }
        }

        private void OnValidate()
        {
            TryCreateFrames();
        }
    }        
}