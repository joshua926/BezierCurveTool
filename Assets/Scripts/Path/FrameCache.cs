using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace BezierCurveDemo
{
    public partial class Path
    {
        public class FrameCache
        {
            Frame[] frames;
            float[] timesAtDistanceSteps;
            float totalDistance;
            bool isLoop; // if isLoop, then the first and final frames should be identical
            public float TotalDistance => totalDistance;
            public int FrameCount => frames == null ? 0 : frames.Length;
            public int DistanceSamplesCount => timesAtDistanceSteps == null ? 0 : timesAtDistanceSteps.Length;

            internal FrameCache(Anchor[] anchors, bool isLoop, int distanceSamplesPerSegment, int framesPerSegment)
            {
                Init(anchors, isLoop, distanceSamplesPerSegment, framesPerSegment);
            }

            internal void Init(Anchor[] anchors, bool isLoop, int distanceSamplesPerSegment, int framesPerSegment)
            {
                distanceSamplesPerSegment = math.max(1, distanceSamplesPerSegment);
                framesPerSegment = math.max(1, framesPerSegment);
                int segmentCount = isLoop ? anchors.Length : anchors.Length - 1;
                int frameCount = framesPerSegment * segmentCount + 1;
                int distanceSamplesCount = distanceSamplesPerSegment * segmentCount + 1;
                if (frames == null || frames.Length != frameCount)
                {
                    frames = new Frame[frameCount];
                }
                if (timesAtDistanceSteps == null || timesAtDistanceSteps.Length != distanceSamplesCount)
                {
                    timesAtDistanceSteps = new float[distanceSamplesCount];
                }
                var anchorsNative = ArrayUtility.Pin(anchors, out ulong anchorsHandle);
                var framesNative = ArrayUtility.Pin(frames, out ulong framesHandle);
                var TimesNative = ArrayUtility.Pin(timesAtDistanceSteps, out ulong timesHandle);
                var totalDistanceArray = new NativeArray<float>(1, Allocator.TempJob);
                var framesJob = new FramesJob
                {
                    anchors = anchorsNative,
                    framesAtTimeSteps = framesNative,
                    isLoop = isLoop,
                };
                var timesJob = new TimesAtDistanceStepsJob
                {
                    anchors = anchorsNative,
                    timesAtDistanceSteps = TimesNative,
                    totalDistanceArray = totalDistanceArray,
                    isLoop = isLoop,
                };
                var framesJobHandle = framesJob.Schedule();
                var timesJobHandle = timesJob.Schedule();
                JobHandle.CombineDependencies(framesJobHandle, timesJobHandle).Complete();
                totalDistance = timesJob.totalDistanceArray[0];
                ArrayUtility.Release(anchorsHandle);
                ArrayUtility.Release(framesHandle);
                ArrayUtility.Release(timesHandle);
                totalDistanceArray.Dispose();
            }

            public Frame GetFrameAtIndex(int i)
            {
                return frames[i];
            }

            public Frame GetFrameAtTime(float pathTime)
            {
                pathTime = isLoop ? pathTime % 1 : math.clamp(pathTime, 0, 1);
                float index = pathTime * (frames.Length - 1);
                int floor = (int)math.floor(index);
                int ceil = (int)math.ceil(index);
                float segmentTime = index - floor;
                return Frame.Interpolate(frames[floor], frames[ceil], segmentTime);
            }

            public Frame GetFrameAtDistance(float distance)
            {
                distance = isLoop ? distance % TotalDistance : math.clamp(distance, 0, TotalDistance);
                float distancePercent = distance / TotalDistance;
                float index = distancePercent * (frames.Length - 1);
                int floor = (int)math.floor(index);
                int ceil = (int)math.floor(index);
                float segmentTime = index - floor;
                float pathTime = math.lerp(timesAtDistanceSteps[floor], timesAtDistanceSteps[ceil], segmentTime);
                return GetFrameAtTime(pathTime);
            }

            public Frame GetFrameAtDistancePercent(float distancePercent)
            {
                distancePercent = isLoop ? distancePercent % 1 : math.clamp(distancePercent, 0, 1);
                float distance = TotalDistance * distancePercent;
                return GetFrameAtDistance(distance);
            }

            struct FramesJob : IJob
            {
                [ReadOnly] public NativeArray<Anchor> anchors;
                public NativeArray<Frame> framesAtTimeSteps;
                public bool isLoop;

                public void Execute()
                {
                    Segment segment0 = Path.GetSegmentAndSegmentTime(anchors, isLoop, 0).segment;
                    Frame frame = new Frame(segment0, 0);
                    framesAtTimeSteps[0] = frame;
                    for (int i = 1; i < framesAtTimeSteps.Length; i++)
                    {
                        float pathTime = (float)i / (framesAtTimeSteps.Length - 1);
                        (Segment segment, float segmentTime) = Path.GetSegmentAndSegmentTime(anchors, isLoop, pathTime);
                        frame = new Frame(frame, segment, segmentTime);
                        framesAtTimeSteps[i] = frame;
                    }
                    //LineUpNormals(ref frames);
                }
            }

            struct TimesAtDistanceStepsJob : IJob
            {
                [ReadOnly] public NativeArray<Anchor> anchors;
                public NativeArray<float> timesAtDistanceSteps;
                public NativeArray<float> totalDistanceArray;
                public bool isLoop;

                public void Execute()
                {
                    var distancesAtTimeSteps = new NativeArray<float>(timesAtDistanceSteps.Length * 4, Allocator.Temp);
                    GetDistancesAtTimeSteps(anchors, isLoop, ref distancesAtTimeSteps);
                    GetTimesAtDistanceSteps(distancesAtTimeSteps, ref timesAtDistanceSteps);
                    totalDistanceArray[0] = distancesAtTimeSteps[distancesAtTimeSteps.Length - 1];
                }
            }

            static void GetDistancesAtTimeSteps(in NativeArray<Anchor> anchors, bool isLoop, ref NativeArray<float> distances)
            {
                float3 pos0 = anchors[0].Position;
                distances[0] = 0;
                float3 pos1;
                for (int i = 1; i < distances.Length; i++)
                {
                    float pathTime = (float)i / (distances.Length - 1);
                    (Segment segment, float segmentTime) = Path.GetSegmentAndSegmentTime(anchors, isLoop, pathTime);
                    pos1 = segment.Position(segmentTime);
                    distances[i] = distances[i - 1] + math.length(pos1 - pos0);
                    pos0 = pos1;
                }
            }

            static void GetTimesAtDistanceSteps(in NativeArray<float> distancesAtTimeSteps, ref NativeArray<float> timesAtDistanceSteps)
            {
                float totalDistance = distancesAtTimeSteps[distancesAtTimeSteps.Length - 1];
                timesAtDistanceSteps[0] = 0;
                int distanceIndex = 0;
                for (int i = 1; i < timesAtDistanceSteps.Length; i++)
                {
                    float percentage = (float)i / (timesAtDistanceSteps.Length - 1);
                    float distance = totalDistance * percentage;
                    while (distance > distancesAtTimeSteps[distanceIndex + 1])
                    {
                        distanceIndex++;
                    }
                    timesAtDistanceSteps[i] = GetTimeAtDistance(distance, distanceIndex, distancesAtTimeSteps);
                }

                static float GetTimeAtDistance(float distance, int distanceIndex, in NativeArray<float> distancesAtTimeSteps)
                {
                    float distanceMin = distancesAtTimeSteps[distanceIndex];
                    float distanceMax = distancesAtTimeSteps[distanceIndex + 1];
                    float timeMin = (float)(distanceIndex + 0) / (distancesAtTimeSteps.Length - 1);
                    float timeMax = (float)(distanceIndex + 1) / (distancesAtTimeSteps.Length - 1);
                    float distancePercent = (distance - distanceMin) / (distanceMax - distanceMin);
                    float timeOffset = distancePercent * (timeMax - timeMin);
                    float time = timeMin + timeOffset;
                    return time;
                }
            }


            static void LineUpNormals(ref NativeArray<Frame> framesAtTimeSteps)
            {
                float radiansOffset = AngleBetween(framesAtTimeSteps[0].normal, framesAtTimeSteps[framesAtTimeSteps.Length - 1].normal);
                for (int i = 1; i < framesAtTimeSteps.Length; i++)
                {
                    float pathTime = (float)i / (framesAtTimeSteps.Length - 1);
                    Frame frame = framesAtTimeSteps[i];
                    float radians = math.lerp(0, radiansOffset, pathTime);
                    quaternion rotation = quaternion.AxisAngle(frame.tangent, radians);
                    frame.normal = math.mul(rotation, frame.normal);
                    framesAtTimeSteps[i] = frame;
                }
            }

            static float AngleBetween(float3 v0, float3 v1)
            {
                float3 n = math.cross(v0, v1);
                float dot = math.dot(v0, v1);
                float determinant = math.dot(math.normalize(n), n);
                return math.atan2(determinant, dot);
            }

            //static float AngleRadiansBetween2Vectors(float3 v0, float3 v1)
            //{
            //    float dot = math.dot(v0, v1);
            //    float lengthProduct = math.sqrt(math.lengthsq(v0) * math.lengthsq(v1));
            //    return math.acos(dot / lengthProduct);
            //}
        }
    }
}