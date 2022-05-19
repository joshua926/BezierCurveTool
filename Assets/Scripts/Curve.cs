using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace BezierCurve
{
    /// <summary>
    /// A collection of Bezier segments. Be sure to re-initialize the cache whenever you edit segments.
    /// </summary>
    [System.Serializable]    
    public partial class Curve : MonoBehaviour
    {
        [SerializeField] bool isLoop;
        [SerializeField] bool autoSetHandles;
        [SerializeField, Min(8)] int cacheFramesPerSegment;
        [SerializeField] Anchor[] anchors;
        FrameCache cache;
        const float defaultHandleLengthMultiplier = .4f;
        public int AnchorCount => anchors.Length;
        public int SegmentCount => isLoop ? anchors.Length : anchors.Length - 1;
        public bool IsLoop => isLoop;
        /// <summary>
        /// Initializes cache if needed.
        /// </summary>
        public FrameCache Cache
        {
            get
            {
                if (cache == null)
                {
                    InitCache();
                }
                return cache;
            }
        }

        /// <summary>
        /// Assigns anchor then updates cache.
        /// </summary>
        public Anchor this[int i]
        {
            get => anchors[i];
            set
            {
                anchors[i] = value;
                if (autoSetHandles)
                {
                    AutoSetHandles(i);
                    if (i > 0) { AutoSetHandles(i - 1); }
                    if (i < anchors.Length - 1) { AutoSetHandles(i + 1); }
                }
            }
        }

        public Curve()
        {
            Reset();
        }

        public void Reset()
        {
            anchors = new Anchor[]
            {
                new Anchor(new float3(0, 0, 0), new float3(0, 2, 0)),
                new Anchor(new float3(1.5f, 2, 0), new float3(.5f, -1f, 0)),
            };
            isLoop = false;
            cacheFramesPerSegment = 8;
        }

        public void InitCache()
        {
            cache = new FrameCache(anchors, IsLoop, cacheFramesPerSegment * 2, cacheFramesPerSegment);
            Debug.Log("cache initialized");
        }

        public Segment GetSegmentAtIndex(int i)
        {
            int index1 = GetIndex(i + 1);
            Anchor a0 = anchors[i];
            Anchor a1 = anchors[index1];
            return new Segment(a0.Position, a0.FrontHandle, a1.BackHandle, a1.Position);
        }

        public (Segment segment, float segmentTime) GetSegmentAndSegmentTime(float curveTime)
        {
            var (index0, segmentTime) = GetIndexAndSegmentTime(curveTime, anchors.Length, IsLoop);
            int index1 = GetIndex(index0 + 1);
            var a0 = anchors[index0];
            var a1 = anchors[index1];
            return (new Segment(a0.Position, a0.FrontHandle, a1.BackHandle, a1.Position), segmentTime);
        }

        public void AddAnchor(float3 position)
        {
            if (isLoop) { return; }
            var anchor = new Anchor(position, 1);
            ArrayUtility.Add(ref anchors, anchor);
            AutoSetHandles(anchors.Length - 1);
        }

        public void InsertAnchor(float3 position, float time)
        {
            int p0Index = GetIndex((int)math.floor(time * (anchors.Length - 1)));
            var anchor = new Anchor(position, 1);
            ArrayUtility.Insert(ref anchors, anchor, p0Index + 1);
            AutoSetHandles(p0Index + 1);
        }

        public void DeleteAnchor(int i)
        {
            ArrayUtility.RemoveAt(ref anchors, i);
            if (autoSetHandles)
            {
                if (i > 0) { AutoSetHandles(i - 1); }
                AutoSetHandles(i);
            }
        }

        public int GetIndexOfNearestAnchor(in Ray ray)
        {
            int indexOfNearest = 0;
            float distancesqMin = float.PositiveInfinity;
            for (int i = 0; i < anchors.Length; i++)
            {
                float distancesq = ray.ProjectionDistanceSq(anchors[i].Position);
                if (distancesq < distancesqMin)
                {
                    distancesqMin = distancesq;
                    indexOfNearest = i;
                }
            }
            return indexOfNearest;
        }

        public (float3 position, float projectionDistance, float curveTime, float rayTime) ProjectRay(in Ray ray, int refineCount = 10)
        {
            int index = 0;
            float distanceMin = float.PositiveInfinity;
            for (int i = 0; i < SegmentCount; i++)
            {
                var segment = GetSegmentAtIndex(i);
                var projection = segment.ProjectRay(ray, 1);
                if (projection.projectionDistance < distanceMin)
                {
                    distanceMin = projection.projectionDistance;
                    index = i;
                }
            }
            var p = GetSegmentAtIndex(index).ProjectRay(ray, refineCount);
            float segmentTimeRange = 1f / SegmentCount;
            float curveTimeFloor = (float)index / SegmentCount;
            float curveTime = curveTimeFloor + segmentTimeRange * p.segmentTime;
            return (p.position, p.projectionDistance, curveTime, p.rayTime);
        }

        public void AutoSetHandles()
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                AutoSetHandles(i);
            }
        }

        public void AutoSetHandles(int i)
        {
            if (anchors.Length <= 2) { return; }
            var anchor = anchors[i];
            bool twoNeighbors = IsLoop || (i > 0 && i < anchors.Length - 1);            
            if (twoNeighbors)
            {
                var prior = anchors[GetIndex(i - 1)];
                var next = anchors[GetIndex(i + 1)];                
                anchor.AutoSetTangents(prior.Position, next.Position);
            }
            else
            {
                int otherIndex = i == 0 ? 1 : i - 1;
                bool otherAnchorIsNextInCurve = i == 0;
                anchor.AutoSetTangentsForEndAnchor(anchors[otherIndex].Position, otherAnchorIsNextInCurve);
            }
            anchors[i] = anchor;
        }

        public static (Segment segment, float segmentTime) GetSegmentAndSegmentTime(
            in NativeArray<Anchor> anchors,
            bool isLoop,
            float curveTime)
        {
            (int index0, float segmentTime) = GetIndexAndSegmentTime(curveTime, anchors.Length, isLoop);
            int index1 = GetIndex(index0 + 1, anchors.Length, isLoop);
            var a0 = anchors[index0];
            var a1 = anchors[index1];
            return (new Segment(a0.Position, a0.FrontHandle, a1.BackHandle, a1.Position), segmentTime);
        }

        // todo try to make anchors having equal positions be ok instead of preventing it
        Anchor ValidateAnchor(Anchor proposedAnchor, int index)
        {
            if (IsLoop || index > 0)
            {
                var anchor = anchors[GetIndex(index - 1)];
                if (proposedAnchor.Position.Equals(anchor.Position))
                {
                    // todo adjust position
                }
            }
            if (IsLoop || index < anchors.Length - 1)
            {
                var anchor = anchors[GetIndex(index + 1)];
                if (proposedAnchor.Position.Equals(anchor.Position))
                {
                    // todo adjust position
                }
            }
            return proposedAnchor;
        }

        int GetIndex(int i)
        {
            return GetIndex(i, anchors.Length, IsLoop);
        }

        static int GetIndex(int i, int length, bool isLoop)
        {
            if (isLoop)
            {
                return (i % length + length) % length; // this handles negative indexes
            }
            else
            {
                return math.clamp(i, 0, length - 1);
            }
        }

        static (int index, float segmentTime) GetIndexAndSegmentTime(float curveTime, int dataLength, bool isLoop)
        {
            curveTime = isLoop ? curveTime % 1 : math.clamp(curveTime, 0, 1);
            int finalIndex = isLoop ? dataLength : dataLength - 1; // the first and final anchors are not the same even if isLoop
            float indexValue = curveTime * finalIndex;
            int index = (int)math.floor(indexValue);
            float segmentTime = indexValue - index;
            return (index, segmentTime);
        }
    }
}