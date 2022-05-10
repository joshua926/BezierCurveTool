using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace BezierCurve
{
    [System.Serializable]
    public class Path
    {
        [SerializeField] Anchor[] anchors;
        [SerializeField] bool isLoop;
        [SerializeField] int cacheFramesPerSegment;
        public Cache Cache { get; private set; }
        const float defaultHandleLengthMultiplier = .3f;
        public int AnchorCount => anchors.Length;
        public int SegmentCount => isLoop ? anchors.Length : anchors.Length - 1;
        public bool IsLoop => isLoop;
        public float TotalDistance => Cache.TotalDistance;
        public int FrameCount => Cache.FrameCount;
        /// <summary>
        /// Assigns anchor then updates cache.
        /// </summary>
        public Anchor this[int i]
        {
            get => anchors[i];
            set
            {
                anchors[i] = value;
                // todo ensure it isn't too close to prior or next anchor
                UpdateCache();
            }
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
                    Anchor.HandleSetting.Aligned),
                new Anchor(
                    new float3(-.5f, 1, 0),
                    new float3(1.5f, 2, 0),
                    new float3(.5f, -1f, 0),
                    Anchor.HandleSetting.Aligned),
            };
            isLoop = false;
            cacheFramesPerSegment = 8;
        }

        public Segment GetSegmentAtIndex(int i)
        {
            int index1 = GetIndex(i + 1, anchors.Length, IsLoop);
            Anchor a0 = anchors[i];
            Anchor a1 = anchors[index1];
            return new Segment(a0.Position, a0.FrontHandle, a1.BackHandle, a1.Position);
        }

        public (Segment segment, float segmentTime) GetSegmentAndSegmentTime(float pathTime)
        {
            var (index0, segmentTime) = GetIndexAndSegmentTime(pathTime, anchors.Length, IsLoop);
            int index1 = GetIndex(index0 + 1, anchors.Length, IsLoop);
            var a0 = anchors[index0];
            var a1 = anchors[index1];
            return (new Segment(a0.Position, a0.FrontHandle, a1.BackHandle, a1.Position), segmentTime);
        }

        public void AddAnchor(float3 position)
        {
            if (isLoop) { return; }
            Anchor lastAnchor = anchors[anchors.Length - 1];           
            float3 offset = (position - lastAnchor.FrontHandle) * defaultHandleLengthMultiplier;
            var anchor = new Anchor(-offset, position, offset, Anchor.HandleSetting.Aligned);
            ArrayUtility.Add(ref anchors, anchor);
        }

        public void InsertAnchor(float3 position, float time)
        {
            int p0Index = GetIndex((int)math.floor(time * (anchors.Length - 1)), anchors.Length, IsLoop);
            int p2Index = GetIndex(p0Index + 1, anchors.Length, IsLoop);
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
                Anchor.HandleSetting.Aligned);            
            ArrayUtility.Insert(ref anchors, anchor, p0Index + 1);
        }

        public void DeleteAnchor(int i)
        {
            ArrayUtility.RemoveAt(ref anchors, i);
        }
        
        public void UpdateCache()
        {
            if (Cache == null) { Cache = new Cache(); }
            Cache.Init(anchors, IsLoop, cacheFramesPerSegment * 2, cacheFramesPerSegment);
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

        public float3 ProjectRay(in Ray ray, out float time, int refineCount = 10)
        {
            Segment nearestSegment = default;
            float distanceMin = float.PositiveInfinity;
            float distance;
            for (int i = 0; i < SegmentCount; i++)
            {
                var segment = GetSegmentAtIndex(i);
                segment.ProjectRay(ray, 1, out distance, out float t);
                if (distance < distanceMin)
                {
                    distanceMin = distance;
                    nearestSegment = segment;
                }
            }
            return nearestSegment.ProjectRay(ray, refineCount, out distance, out time);
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

        static (int index, float segmentTime) GetIndexAndSegmentTime(float pathTime, int dataLength, bool isLoop)
        {
            pathTime = isLoop ? pathTime % 1 : math.clamp(pathTime, 0, 1);
            int finalIndex = isLoop ? dataLength : dataLength - 1; // the first and final anchors are not the same even if isLoop
            float indexValue = pathTime * finalIndex;
            int index = (int)math.floor(indexValue);
            float segmentTime = indexValue - index;
            return (index, segmentTime);
        }

        static int GetIndex(int i, int dataLength, bool isLoop)
        {
            if (isLoop)
            {
                return (i % dataLength + dataLength) % dataLength; // this handles negative indexes
            }
            else
            {
                return math.clamp(i, 0, dataLength - 1);
            }
        }
    }
}