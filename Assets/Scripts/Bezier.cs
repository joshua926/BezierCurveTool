using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace BezierCurve
{
    public static class Bezier
    {
        #region Structs
        public enum TangentSetting { Aligned = 0, Free = 1 }

        [System.Serializable]
        public struct Point3D
        {
            public float3 backTangent;
            public float3 position;
            public float3 frontTangent;
            public float3 BackHandle => position + backTangent;
            public float3 FrontHandle => position + frontTangent;
        }

        [System.Serializable]
        public struct Point2D
        {
            public float2 backTangent;
            public float2 position;
            public float2 frontTangent;
            public float2 BackHandle => position + backTangent;
            public float2 FrontHandle => position + frontTangent;
        }

        [System.Serializable]
        public struct Frame3D
        {
            public float3 position;
            public float3 tangent;
            public float3 normal;
            public float pathTime;
        }

        [System.Serializable]
        public struct Frame2D
        {
            public float2 position;
            public float2 tangent;
            public float2 normal;
            public float pathTime;
        }
        #endregion

        #region Math
        public static float3 Position(in Point3D[] points, float pathTime, bool isLoop)
        {
            GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
            return Position(p0, p1, segmentTime);
        }

        public static float3 Position(in NativeArray<Point3D> points, float pathTime, bool isLoop)
        {
            GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
            return Position(p0, p1, segmentTime);
        }

        public static float3 Position(in Point3D p0, in Point3D p1, float segmentTime)
        {
            float3 a = math.lerp(p0.position, p0.FrontHandle, segmentTime);
            float3 b = math.lerp(p0.FrontHandle, p1.BackHandle, segmentTime);
            float3 c = math.lerp(p1.BackHandle, p1.position, segmentTime);
            float3 d = math.lerp(a, b, segmentTime);
            float3 e = math.lerp(b, c, segmentTime);
            return math.lerp(d, e, segmentTime);
        }

        public static float2 Position(in Point2D[] points, float pathTime, bool isLoop)
        {
            GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
            return Position(p0, p1, segmentTime);
        }

        public static float2 Position(in NativeArray<Point2D> points, float pathTime, bool isLoop)
        {
            GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
            return Position(p0, p1, segmentTime);
        }

        public static float2 Position(in Point2D p0, in Point2D p1, float segmentTime)
        {
            float2 a = math.lerp(p0.position, p0.FrontHandle, segmentTime);
            float2 b = math.lerp(p0.FrontHandle, p1.BackHandle, segmentTime);
            float2 c = math.lerp(p1.BackHandle, p1.position, segmentTime);
            float2 d = math.lerp(a, b, segmentTime);
            float2 e = math.lerp(b, c, segmentTime);
            return math.lerp(d, e, segmentTime);
        }

        public static float3 Derivative(in Point3D p0, in Point3D p1, float segmentTime)
        {
            float3 a = (p0.FrontHandle - p0.position) * 3;
            float3 b = (p1.BackHandle - p0.FrontHandle) * 3;
            float3 c = (p1.position - p1.BackHandle) * 3;
            float3 A = math.lerp(a, b, segmentTime);
            float3 B = math.lerp(b, c, segmentTime);
            return math.lerp(A, B, segmentTime);
        }

        public static float2 Derivative(in Point2D p0, in Point2D p1, float segmentTime)
        {
            float2 a = (p0.FrontHandle - p0.position) * 3;
            float2 b = (p1.BackHandle - p0.FrontHandle) * 3;
            float2 c = (p1.position - p1.BackHandle) * 3;
            float2 A = math.lerp(a, b, segmentTime);
            float2 B = math.lerp(b, c, segmentTime);
            return math.lerp(A, B, segmentTime);
        }

        public static float3 SecondDerivative(in Point3D p0, in Point3D p1, float segmentTime)
        {
            float3 a = (p0.FrontHandle - p0.position) * 3;
            float3 b = (p1.BackHandle - p0.FrontHandle) * 3;
            float3 c = (p1.position - p1.BackHandle) * 3;
            float3 A = (b - a) * 2;
            float3 B = (c - b) * 2;
            return math.lerp(A, B, segmentTime);
        }

        public static float2 SecondDerivative(in Point2D p0, in Point2D p1, float segmentTime)
        {
            float2 a = (p0.FrontHandle - p0.position) * 3;
            float2 b = (p1.BackHandle - p0.FrontHandle) * 3;
            float2 c = (p1.position - p1.BackHandle) * 3;
            float2 A = (b - a) * 2;
            float2 B = (c - b) * 2;
            return math.lerp(A, B, segmentTime);
        }

        public static float3 ThirdDerivative(in Point3D p0, in Point3D p1)
        {
            float3 a = (p0.FrontHandle - p0.position) * 3;
            float3 b = (p1.BackHandle - p0.FrontHandle) * 3;
            float3 c = (p1.position - p1.BackHandle) * 3;
            float3 A = (b - a) * 2;
            float3 B = (c - b) * 2;
            return B - A;
        }

        public static float2 ThirdDerivative(in Point2D p0, in Point2D p1)
        {
            float2 a = (p0.FrontHandle - p0.position) * 3;
            float2 b = (p1.BackHandle - p0.FrontHandle) * 3;
            float2 c = (p1.position - p1.BackHandle) * 3;
            float2 A = (b - a) * 2;
            float2 B = (c - b) * 2;
            return B - A;
        }

        public static float ArcLength(in Point3D[] points, bool isLoop, int samplesPerSegment = 8)
        {
            int sampleCount = GetSampleCount(points.Length, samplesPerSegment, isLoop);
            float arcLength = 0;
            float3 priorPosition = points[0].position;
            for (int i = 1; i < sampleCount; i++)
            {
                float pathTime = (float)i / (sampleCount - 1);
                GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segementTime);
                float3 position = Position(p0, p1, segementTime);
                arcLength += math.distance(position, priorPosition);
            }
            return arcLength;
        }

        public static void GetPointsAtTime(in Point3D[] points, float pathTime, bool isLoop, out Point3D p0, out Point3D p1, out float segmentTime)
        {
            GetIndexAndSegmentTime(pathTime, points.Length, isLoop, out int p0Index, out segmentTime);
            p0 = points[GetIndex(p0Index + 0, points.Length, isLoop)];
            p1 = points[GetIndex(p0Index + 1, points.Length, isLoop)];
        }

        public static void GetPointsAtTime(in NativeArray<Point3D> points, float pathTime, bool isLoop, out Point3D p0, out Point3D p1, out float segmentTime)
        {
            GetIndexAndSegmentTime(pathTime, points.Length, isLoop, out int p0Index, out segmentTime);
            p0 = points[GetIndex(p0Index + 0, points.Length, isLoop)];
            p1 = points[GetIndex(p0Index + 1, points.Length, isLoop)];
        }

        public static void GetPointsAtTime(in Point2D[] points, float pathTime, bool isLoop, out Point2D p0, out Point2D p1, out float segmentTime)
        {
            GetIndexAndSegmentTime(pathTime, points.Length, isLoop, out int p0Index, out segmentTime);
            p0 = points[GetIndex(p0Index + 0, points.Length, isLoop)];
            p1 = points[GetIndex(p0Index + 1, points.Length, isLoop)];
        }

        public static void GetPointsAtTime(in NativeArray<Point2D> points, float pathTime, bool isLoop, out Point2D p0, out Point2D p1, out float segmentTime)
        {
            GetIndexAndSegmentTime(pathTime, points.Length, isLoop, out int p0Index, out segmentTime);
            p0 = points[GetIndex(p0Index + 0, points.Length, isLoop)];
            p1 = points[GetIndex(p0Index + 1, points.Length, isLoop)];
        }

        public static void GetIndexAndSegmentTime(float pathTime, int dataLength, bool isLoop, out int index, out float segmentTime)
        {
            pathTime = isLoop ? pathTime % 1 : math.clamp(pathTime, 0, 1);
            int finalIndex = isLoop ? dataLength : dataLength - 1;
            float indexValue = pathTime * finalIndex;
            index = (int)math.floor(indexValue);
            segmentTime = indexValue - index;
        }

        public static int GetIndex(int i, int dataLength, bool isLoop)
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

        public static int GetSampleCount(int pointsLength, int samplesPerSegment, bool isLoop)
        {
            int segmentCount = isLoop ? pointsLength : pointsLength - 1;
            return segmentCount * samplesPerSegment + 1;
        }
        #endregion

        #region Frames3D        
        public static void GetFramesAtTimeSteps(in Point3D[] points, ref Frame3D[] frames, bool isLoop)
        {
            var job = new Frames3DAtTimeStepsJob
            {
                points = ArrayUtility.Pin(points, out ulong pointsHandle),
                frames = ArrayUtility.Pin(frames, out ulong framesHandle),
                isLoop = isLoop
            };
            job.Schedule().Complete();
            ArrayUtility.Release(pointsHandle);
            ArrayUtility.Release(framesHandle);
        }

        public static void GetFramesAtDistanceSteps(in Point3D[] points, ref Frame3D[] frames, bool isLoop)
        {
            var job = new Frames3DAtDistanceStepsJob
            {
                points = ArrayUtility.Pin(points, out ulong pointsHandle),
                frames = ArrayUtility.Pin(frames, out ulong framesHandle),
                isLoop = isLoop
            };
            job.Schedule().Complete();
            ArrayUtility.Release(pointsHandle);
            ArrayUtility.Release(framesHandle);
        }        

        [BurstCompile(CompileSynchronously = true)]
        struct Frames3DAtTimeStepsJob : IJob
        {
            [ReadOnly] public NativeArray<Point3D> points;
            public NativeArray<Frame3D> frames;
            public bool isLoop;
            public void Execute()
            {
                Frame3D frame = InitialFrame(points[0], points[1]);
                frames[0] = frame;
                for (int i = 1; i < frames.Length; i++)
                {
                    float pathTime = (float)i / (frames.Length - 1);
                    GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
                    frame = DoubleReflectionFrame(frame, p0, p1, pathTime, segmentTime);
                    frames[i] = frame;
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct Frames3DAtDistanceStepsJob : IJob
        {
            [ReadOnly] public NativeArray<Point3D> points;
            public NativeArray<Frame3D> frames;
            public bool isLoop;
            public void Execute()
            {
                int distanceSampleCount = GetSampleCount(points.Length, 8, isLoop);
                distanceSampleCount = math.max(distanceSampleCount, frames.Length * 4);
                var distances = new NativeArray<float>(distanceSampleCount, Allocator.Temp);
                var times = new NativeArray<float>(frames.Length, Allocator.Temp);
                GetDistancesAtTimeSteps(points, ref distances, isLoop);
                GetTimesAtDistanceSteps(distances, ref times);
                Frame3D frame = InitialFrame(points[0], points[1]);
                frames[0] = frame;
                int timeIndex = 0;
                int distanceIndex = 0;
                while (distanceIndex <= frames.Length - 2)
                {
                    float pathTime;
                    bool distanceIncrement;
                    float timeTime = (float)(timeIndex + 1) / (distanceSampleCount - 1);
                    float distanceTime = frames[distanceIndex + 1].pathTime;
                    if (timeTime < distanceTime)
                    {
                        pathTime = timeTime;
                        timeIndex++;
                        distanceIncrement = false;
                    }
                    else
                    {
                        pathTime = distanceTime;
                        distanceIndex++;
                        distanceIncrement = true;
                    }
                    GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
                    frame = DoubleReflectionFrame(frame, p0, p1, pathTime, segmentTime);
                    if (distanceIncrement)
                    {
                        frames[distanceIndex] = frame;
                    }
                }
            }
        }

        static Frame3D InitialFrame(Point3D p0, Point3D p1)
        {
            float3 position = p0.position;
            float3 tangent = math.normalize(p0.frontTangent);
            float3 secondDerivative = SecondDerivative(p0, p1, 0);
            float3 b = math.normalize(tangent + secondDerivative);
            float3 binormal = math.normalize(math.cross(b, tangent));
            float3 normal = math.normalize(math.cross(binormal, tangent));
            return new Frame3D
            {
                position = position,
                tangent = tangent,
                normal = normal,
                pathTime = 0
            };
        }

        static Frame3D DoubleReflectionFrame(in Frame3D priorFrame, Point3D p0, Point3D p1, float pathTime, float segmentTime)
        {
            float3 position = Position(p0, p1, segmentTime);
            float3 tangent = math.normalize(Derivative(p0, p1, segmentTime));
            return new Frame3D
            {
                position = position,
                tangent = tangent,
                normal = DoubleReflectionNormal(priorFrame, position, tangent),
                pathTime = pathTime
            };

            static float3 DoubleReflectionNormal(in Frame3D priorFrame, float3 position, float3 tangent)
            {
                float3 reflectionNormal0 = position - priorFrame.position;
                if (reflectionNormal0.Equals(0)) { return priorFrame.normal; }
                float3 binormalReflection = ReflectAlongNormal(reflectionNormal0, priorFrame.normal);
                float3 tangentReflection = ReflectAlongNormal(reflectionNormal0, priorFrame.tangent);
                float3 reflectionNormal1 = tangent - tangentReflection;
                float3 normal = ReflectAlongNormal(reflectionNormal1, binormalReflection);
                return normal;

                float3 ReflectAlongNormal(float3 normal, float3 vectorToReflect)
                {
                    normal = math.normalize(normal);
                    float distanceToPlane = math.dot(normal, vectorToReflect);
                    return vectorToReflect - 2 * distanceToPlane * normal;
                }
                //float3 ReflectAlongNormal(float3 normal, float3 vectorToReflect)
                //{
                //    return vectorToReflect - (2 / math.lengthsq(normal)) * math.dot(normal, vectorToReflect) * normal;
                //}
            }
        }

        static void GetDistancesAtTimeSteps(in NativeArray<Point3D> points, ref NativeArray<float> distances, bool isLoop)
        {
            float3 pos0 = points[0].position;
            distances[0] = 0;
            float3 pos1;
            for (int i = 1; i < distances.Length; i++)
            {
                float pathTime = (float)i / (distances.Length - 1);
                pos1 = Position(points, pathTime, isLoop);
                distances[i] = distances[i - 1] + math.length(pos1 - pos0);
                pos0 = pos1;
            }
        }

        static void GetTimesAtDistanceSteps(in NativeArray<float> distances, ref NativeArray<float> times)
        {
            float totalDistance = distances[distances.Length - 1];
            times[0] = 0;
            int distanceIndex = 0;
            for (int i = 1; i < times.Length; i++)
            {
                float percentage = (float)i / (times.Length - 1);
                float distance = totalDistance * percentage;
                while (distance > distances[distanceIndex + 1])
                {
                    distanceIndex++;
                }
                float distanceMin = distances[distanceIndex];
                float distanceMax = distances[distanceIndex + 1];
                float timeMin = (float)(distanceIndex + 0) / (distances.Length - 1);
                float timeMax = (float)(distanceIndex + 1) / (distances.Length - 1);
                times[i] = GetTimeAtDistance(distance, distanceMin, distanceMax, timeMin, timeMax);
            }

            static float GetTimeAtDistance(float distance, float distanceMin, float distanceMax, float timeMin, float timeMax)
            {
                float distancePercent = (distance - distanceMin) / (distanceMax - distanceMin);
                float timeOffset = distancePercent * (timeMax - timeMin);
                float time = timeMin + timeOffset;
                return time;
            }
        }
        #endregion

        #region Frames2D
        public static Frame2D GetFrameAtTime(in Point2D[] points, float pathTime, bool isLoop)
        {
            GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
            return GetFrameAtTime(p0, p1, pathTime, segmentTime);
        }

        public static Frame2D GetFrameAtTime(in NativeArray<Point2D> points, float pathTime, bool isLoop)
        {
            GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
            return GetFrameAtTime(p0, p1, pathTime, segmentTime);
        }

        public static void GetFramesAtTimeSteps(in Point2D[] points, ref Frame2D[] frames, bool isLoop)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                float pathTime = (float)i / (frames.Length - 1);
                frames[i] = GetFrameAtTime(points, pathTime, isLoop);
            }
        }

        public static void GetFramesAtDistanceSteps(in Point2D[] points, ref Frame2D[] frames, bool isLoop)
        {
            var job = new Frames2DAtDistanceStepsJob
            {
                points = ArrayUtility.Pin(points, out ulong pointsHandle),
                frames = ArrayUtility.Pin(frames, out ulong framesHandle),
                isLoop = isLoop
            };
            job.Schedule().Complete();
            ArrayUtility.Release(pointsHandle);
            ArrayUtility.Release(framesHandle);
        }

        static Frame2D GetFrameAtTime(in Point2D p0, in Point2D p1, float pathTime, float segmentTime)
        {
            float2 position = Position(p0, p1, segmentTime);
            float2 tangent = Derivative(p0, p1, segmentTime);
            float2 normal = new float2(tangent.y, -tangent.x); // this rotates the tangent 90 degrees clockwise
            return new Frame2D
            {
                position = position,
                tangent = tangent,
                normal = normal,
                pathTime = pathTime,
            };
        }

        [BurstCompile(CompileSynchronously = true)]
        struct Frames2DAtDistanceStepsJob : IJob
        {
            [ReadOnly] public NativeArray<Point2D> points;
            public NativeArray<Frame2D> frames;
            public bool isLoop;
            public void Execute()
            {
                int distanceSampleCount = GetSampleCount(points.Length, 8, isLoop);
                distanceSampleCount = math.max(distanceSampleCount, frames.Length * 4);
                var distances = new NativeArray<float>(distanceSampleCount, Allocator.Temp);
                var pathTimes = new NativeArray<float>(frames.Length, Allocator.Temp);
                GetDistancesAtTimeSteps(points, ref distances, isLoop);
                GetTimesAtDistanceSteps(distances, ref pathTimes);
                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i] = GetFrameAtTime(points, pathTimes[i], isLoop);
                }
            }
        }

        static void GetDistancesAtTimeSteps(in NativeArray<Point2D> points, ref NativeArray<float> distances, bool isLoop)
        {
            float2 pos0 = points[0].position;
            distances[0] = 0;
            float2 pos1;
            for (int i = 1; i < distances.Length; i++)
            {
                float pathTime = (float)i / (distances.Length - 1);
                GetPointsAtTime(points, pathTime, isLoop, out var p0, out var p1, out float segmentTime);
                pos1 = Position(p0, p1, segmentTime);
                distances[i] = distances[i - 1] + math.length(pos1 - pos0);
                pos0 = pos1;
            }
        }
        #endregion

        #region 3D Ray Projection
        public static int GetIndexOfNearestSegmentPoint(Point3D[] points, in Ray ray)
        {
            int nearestIndex = 0;
            float distanceSqMin = float.PositiveInfinity;
            for (int i = 0; i < points.Length; i++)
            {
                float distancesq = ray.Distancesq(points[i].position);
                if (distancesq < distanceSqMin)
                {
                    nearestIndex = i;
                    distanceSqMin = distancesq;
                }
            }
            return nearestIndex;
        }

        public static float3 ProjectRay(Point3D[] points, bool isLoop, Ray ray, int refineCount, out float pathTime)
        {
            int sampleCount = GetSampleCount(points.Length, 4, isLoop);
            float timeOffset = 1f / (sampleCount - 1);
            DistanceTime distanceTime = GetInitialDistanceTime(points, isLoop, ray, sampleCount);
            DistanceTime5 distanceTimes = GetInitialValues(points, isLoop, ray, timeOffset, distanceTime);
            for (int i = 0; i < refineCount - 1; i++)
            {
                CalculateInBetweenValues(points, isLoop, ray, ref distanceTimes);
                distanceTimes.ZoomIn();
            }
            int indexOfMin = distanceTimes.GetIndexOfMinDistance();
            pathTime = distanceTimes[indexOfMin].time;
            return Position(points, pathTime, isLoop);

            static DistanceTime GetInitialDistanceTime(in Point3D[] points, bool isLoop, in Ray ray, int sampleCount)
            {
                float time = 0;
                float distance = float.PositiveInfinity;
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / (sampleCount - 1);
                    float d = ray.Distancesq(Position(points, t, isLoop));
                    if (d < distance)
                    {
                        distance = d;
                        time = t;
                    }
                }
                return new DistanceTime(distance, time);
            }

            static DistanceTime5 GetInitialValues(in Point3D[] points, bool isLoop, in Ray ray, float timeOffset, DistanceTime distanceTime)
            {
                var distanceTimes = new DistanceTime5();
                float v0Time = math.clamp(distanceTime.time - timeOffset, 0, 1);
                float v4Time = math.clamp(distanceTime.time + timeOffset, 0, 1);
                distanceTimes[0] = new DistanceTime(
                    ray.Distancesq(Position(points, v0Time, isLoop)),
                    v0Time);
                distanceTimes[2] = distanceTime;
                distanceTimes[4] = new DistanceTime(
                    ray.Distancesq(Position(points, v4Time, isLoop)),
                    v4Time);
                return distanceTimes;
            }

            static void CalculateInBetweenValues(in Point3D[] points, bool isLoop, in Ray ray, ref DistanceTime5 distanceTimes)
            {
                float v1Time = (distanceTimes[0].time + distanceTimes[2].time) / 2;
                float v3Time = (distanceTimes[2].time + distanceTimes[4].time) / 2;
                float v1Distance = ray.Distancesq(Position(points, v1Time, isLoop));
                float v3Distance = ray.Distancesq(Position(points, v3Time, isLoop));
                distanceTimes[1] = new DistanceTime(v1Distance, v1Time);
                distanceTimes[3] = new DistanceTime(v3Distance, v3Time);
            }
        }

        unsafe struct DistanceTime5
        {
            fixed float values[10];
            public DistanceTime this[int i]
            {
                get
                {
                    return new DistanceTime(
                        values[i * 2 + 0],
                        values[i * 2 + 1]);
                }
                set
                {
                    values[i * 2 + 0] = value.distance;
                    values[i * 2 + 1] = value.time;
                }
            }

            public int GetIndexOfMinDistance()
            {
                int indexOfMin = 2; // index must start at 2 to ensure in between values get a chance
                for (int i = 0; i < 5; i++)
                {

                    if (this[i].distance < this[indexOfMin].distance)
                    {
                        indexOfMin = i;
                    }
                }
                return indexOfMin;
            }

            public void ZoomIn()
            {
                int centerIndex = math.clamp(GetIndexOfMinDistance(), 1, 3);
                this[0] = this[centerIndex - 1];
                this[4] = this[centerIndex + 1];
                this[2] = this[centerIndex];
            }
        }

        struct DistanceTime
        {
            public float distance;
            public float time;
            public DistanceTime(float distance, float time)
            {
                this.distance = distance;
                this.time = time;
            }
        }
        #endregion

        #region 2D Position Projection
        public static int GetIndexOfNearestSegmentPoint(Point2D[] points, float2 position)
        {
            int nearestIndex = 0;
            float distanceSqMin = float.PositiveInfinity;
            for (int i = 0; i < points.Length; i++)
            {
                float distancesq = math.distancesq(points[i].position, position);
                if (distancesq < distanceSqMin)
                {
                    nearestIndex = i;
                    distanceSqMin = distancesq;
                }
            }
            return nearestIndex;
        }

        public static float2 ProjectPosition(Point2D[] points, bool isLoop, float2 position, int refineCount, out float pathTime)
        {
            int sampleCount = GetSampleCount(points.Length, 4, isLoop);
            float timeOffset = 1f / (sampleCount - 1);
            DistanceTime distanceTime = GetInitialDistanceTime(points, isLoop, position, sampleCount);
            DistanceTime5 distanceTimes = GetInitialValues(points, isLoop, position, timeOffset, distanceTime);
            for (int i = 0; i < refineCount - 1; i++)
            {
                CalculateInBetweenValues(points, isLoop, position, ref distanceTimes);
                distanceTimes.ZoomIn();
            }
            int indexOfMin = distanceTimes.GetIndexOfMinDistance();
            pathTime = distanceTimes[indexOfMin].time;
            return Position(points, pathTime, isLoop);

            static DistanceTime GetInitialDistanceTime(in Point2D[] points, bool isLoop, float2 position, int sampleCount)
            {
                float time = 0;
                float distance = float.PositiveInfinity;
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / (sampleCount - 1);
                    float d = math.distancesq(position, Position(points, t, isLoop));
                    if (d < distance)
                    {
                        distance = d;
                        time = t;
                    }
                }
                return new DistanceTime(distance, time);
            }

            static DistanceTime5 GetInitialValues(in Point2D[] points, bool isLoop, float2 position, float timeOffset, DistanceTime distanceTime)
            {
                var distanceTimes = new DistanceTime5();
                float v0Time = math.clamp(distanceTime.time - timeOffset, 0, 1);
                float v4Time = math.clamp(distanceTime.time + timeOffset, 0, 1);
                distanceTimes[0] = new DistanceTime(
                    math.distancesq(position, Position(points, v0Time, isLoop)),
                    v0Time);
                distanceTimes[2] = distanceTime;
                distanceTimes[4] = new DistanceTime(
                    math.distancesq(position, Position(points, v4Time, isLoop)),
                    v4Time);
                return distanceTimes;
            }

            static void CalculateInBetweenValues(in Point2D[] points, bool isLoop, float2 position, ref DistanceTime5 distanceTimes)
            {
                float v1Time = (distanceTimes[0].time + distanceTimes[2].time) / 2;
                float v3Time = (distanceTimes[2].time + distanceTimes[4].time) / 2;
                float v1Distance = math.distancesq(position, Position(points, v1Time, isLoop));
                float v3Distance = math.distancesq(position, Position(points, v3Time, isLoop));
                distanceTimes[1] = new DistanceTime(v1Distance, v1Time);
                distanceTimes[3] = new DistanceTime(v3Distance, v3Time);
            }
        }
        #endregion
    }
}