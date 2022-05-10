using UnityEngine;
using UnityEditor;

namespace BezierCurve
{
    public partial class Path3DBehaviour
    {
        private void OnDrawGizmos()
        {            
            for (int i = 0; i < path.SegmentCount; i++)
            {
                var s = path.GetSegmentAtIndex(i);
                Handles.DrawBezier(s.points[0], s.points[3], s.points[1], s.points[2], Color.green, null, 3);
            }
        }

        private void OnEnable()
        {
            path.UpdateCache();
        }
    }        
}