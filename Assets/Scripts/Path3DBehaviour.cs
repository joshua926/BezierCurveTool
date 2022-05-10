using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    [ExecuteAlways]
    public partial class Path3DBehaviour : MonoBehaviour
    {
        [SerializeField] Path path;

        public int FrameCount => path.FrameCount;

        void Awake()
        {
            path.UpdateCache();
        }

        public Frame GetFrameAtIndex(int i)
        {
            return path.Cache.GetFrameAtIndex(i);
        }

        public Frame GetFrameAtTime(float pathTime)
        {
            return path.Cache.GetFrameAtTime(pathTime);
        }

        public Frame GetFrameAtDistance(float distance)
        {
            return path.Cache.GetFrameAtDistance(distance);
        }

        public Frame GetFrameAtDistancePercent(float distancePercent)
        {
            return path.Cache.GetFrameAtDistancePercent(distancePercent);
        }
    }
}