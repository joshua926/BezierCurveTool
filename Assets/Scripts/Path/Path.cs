using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace BezierCurveDemo
{
    /// <summary>
    /// A collection of Bezier segments. Be sure to re-initialize the cache whenever you edit segments.
    /// </summary>
    [System.Serializable]    
    public partial class Path : MonoBehaviour
    {
        [SerializeField] bool isLoop;
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
            set => anchors[i] = value; // todo make sure anchors with same position don't cause problems
        }

        public Path()
        {
            Reset();
        }

        public void Reset()
        {
            anchors = new Anchor[]
            {
                new Anchor(
                    new float3(0, -2, 0),
                    0,
                    new float3(0, 2, 0),
                    Anchor.HandleType.Aligned),
                new Anchor(
                    new float3(-.5f, 1, 0),
                    new float3(1.5f, 2, 0),
                    new float3(.5f, -1f, 0),
                    Anchor.HandleType.Aligned),
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

        public (Segment segment, float segmentTime) GetSegmentAndSegmentTime(float pathTime)
        {
            var (index0, segmentTime) = GetIndexAndSegmentTime(pathTime, anchors.Length, IsLoop);
            int index1 = GetIndex(index0 + 1);
            var a0 = anchors[index0];
            var a1 = anchors[index1];
            return (new Segment(a0.Position, a0.FrontHandle, a1.BackHandle, a1.Position), segmentTime);
        }

        public void AddAnchor(float3 position)
        {
            if (isLoop) { return; }
            Anchor lastAnchor = anchors[anchors.Length - 1];
            float3 offset = (position - lastAnchor.FrontHandle) * defaultHandleLengthMultiplier;
            var anchor = new Anchor(-offset, position, offset, Anchor.HandleType.Aligned);
            ArrayUtility.Add(ref anchors, anchor);
        }

        public void InsertAnchor(float3 position, float time)
        {
            int p0Index = GetIndex((int)math.floor(time * (anchors.Length - 1)));
            int p2Index = GetIndex(p0Index + 1);
            Anchor p0 = anchors[p0Index];
            Anchor p2 = anchors[p2Index];
            float p0Distance = math.distance(p0.Position, position);
            float p2Distance = math.distance(p2.Position, position);
            float3 p0Direction = (p0.Position - position) / p0Distance;
            float3 p2Direction = (p2.Position - position) / p2Distance;
            var anchor = new Anchor(
                math.normalize(p0Direction - p2Direction) * p0Distance * defaultHandleLengthMultiplier,
                position,
                math.normalize(p2Direction - p0Direction) * p2Distance * defaultHandleLengthMultiplier,
                Anchor.HandleType.Aligned);
            ArrayUtility.Insert(ref anchors, anchor, p0Index + 1);
        }

        public void DeleteAnchor(int i)
        {
            ArrayUtility.RemoveAt(ref anchors, i);
        }

        public int GetIndexOfNearestAnchor(in Ray ray)
        {
            int indexOfNearest = 0;
            float distancesqMin = float.PositiveInfinity;
            for (int i = 0; i < anchors.Length; i++)
            {
                float distancesq = ray.Distancesq(anchors[i].Position);
                if (distancesq < distancesqMin)
                {
                    distancesqMin = distancesq;
                    indexOfNearest = i;
                }
            }
            return indexOfNearest;
        }

        public (float3 position, float rayDistance, float time) ProjectRay(in Ray ray, int refineCount = 10)
        {
            Segment nearestSegment = default;
            float distanceMin = float.PositiveInfinity;
            for (int i = 0; i < SegmentCount; i++)
            {
                var segment = GetSegmentAtIndex(i);
                var projection = segment.ProjectRay(ray, 1);
                if (projection.rayDistance < distanceMin)
                {
                    distanceMin = projection.rayDistance;
                    nearestSegment = segment;
                }
            }
            return nearestSegment.ProjectRay(ray, refineCount);
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
                bool otherAnchorIsNextInPath = i == 0;
                anchor.AutoSetTangents(anchors[otherIndex].Position, otherAnchorIsNextInPath);
            }
            anchors[i] = anchor;
        }

        public static (Segment segment, float segmentTime) GetSegmentAndSegmentTime(
            in NativeArray<Anchor> anchors,
            bool isLoop,
            float pathTime)
        {
            (int index0, float segmentTime) = GetIndexAndSegmentTime(pathTime, anchors.Length, isLoop);
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

        static (int index, float segmentTime) GetIndexAndSegmentTime(float pathTime, int dataLength, bool isLoop)
        {
            pathTime = isLoop ? pathTime % 1 : math.clamp(pathTime, 0, 1);
            int finalIndex = isLoop ? dataLength : dataLength - 1; // the first and final anchors are not the same even if isLoop
            float indexValue = pathTime * finalIndex;
            int index = (int)math.floor(indexValue);
            float segmentTime = indexValue - index;
            return (index, segmentTime);
        }
    }
}