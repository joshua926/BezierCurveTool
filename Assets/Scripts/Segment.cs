using Unity.Mathematics;
using Unity.Collections;

namespace BezierCurve
{
    public partial class Curve
    {
        public readonly struct Segment
        {
            public readonly float3x4 points;

            public Segment(float3 p0, float3 p1, float3 p2, float3 p3)
            {
                points = new float3x4(p0, p1, p2, p3);
            }

            public Segment(in float3x4 points)
            {
                this.points = points;
            }

            public float3 Position(float t)
            {
                float3 a = math.lerp(points[0], points[1], t);
                float3 b = math.lerp(points[1], points[2], t);
                float3 c = math.lerp(points[2], points[3], t);
                float3 d = math.lerp(a, b, t);
                float3 e = math.lerp(b, c, t);
                return math.lerp(d, e, t);
            }

            public float3 Tangent(float t)
            {
                float3 a = (points[1] - points[0]) * 3;
                float3 b = (points[2] - points[1]) * 3;
                float3 c = (points[3] - points[2]) * 3;
                float3 d = math.lerp(a, b, t);
                float3 e = math.lerp(b, c, t);
                return math.lerp(d, e, t);
            }

            public float3 Acceleration(float t)
            {
                float3 a = (points[1] - points[0]) * 3;
                float3 b = (points[2] - points[1]) * 3;
                float3 c = (points[3] - points[2]) * 3;
                float3 d = (b - a) * 2;
                float3 e = (c - b) * 2;
                return math.lerp(d, e, t);
            }

            public float3 Jerk()
            {
                float3 a = (points[1] - points[0]) * 3;
                float3 b = (points[2] - points[1]) * 3;
                float3 c = (points[3] - points[2]) * 3;
                float3 d = (b - a) * 2;
                float3 e = (c - b) * 2;
                return e - d;
            }

            public (float3 position, float projectionDistance, float segmentTime, float rayTime) ProjectRay(in Ray ray, int iterations)
            {
                iterations = math.max(1, iterations);
                (float5 times, float5 distances) = GetInitialValues(ray);
                for (int i = 0; i < iterations; i++)
                {
                    CalculateInBetweenValues(ref times, ref distances, ray);
                    ZoomIn(ref times, ref distances);
                }
                int indexOfMinDistance = GetIndexOfMinDistance(distances);
                float projectionDistance = math.sqrt(distances[indexOfMinDistance]);
                float segmentTime = times[indexOfMinDistance];
                float3 pos = Position(segmentTime);
                float rayTime = ray.ProjectionTime(pos);
                return (pos, projectionDistance, segmentTime, rayTime);
            }

            (float5 times, float5 distances) GetInitialValues(in Ray ray)
            {
                float5 times = default;
                times[0] = 0;
                times[2] = .5f;
                times[4] = 1;
                float5 distances = default;
                distances[0] = ray.ProjectionDistanceSq(points[0]);
                distances[2] = ray.ProjectionDistanceSq(Position(.5f));
                distances[4] = ray.ProjectionDistanceSq(points[3]);
                return (times, distances);
            }

            void CalculateInBetweenValues(ref float5 times, ref float5 distances, in Ray ray)
            {
                times[1] = (times[0] + times[2]) * .5f;
                times[3] = (times[2] + times[4]) * .5f;
                distances[1] = ray.ProjectionDistanceSq(Position(times[1]));
                distances[3] = ray.ProjectionDistanceSq(Position(times[3]));
            }

            void ZoomIn(ref float5 times, ref float5 distances)
            {
                int centerIndex = math.clamp(GetIndexOfMinDistance(distances), 1, 3);
                distances[0] = distances[centerIndex - 1];
                distances[4] = distances[centerIndex + 1];
                distances[2] = distances[centerIndex];
                times[0] = times[centerIndex - 1];
                times[4] = times[centerIndex + 1];
                times[2] = times[centerIndex];
            }

            int GetIndexOfMinDistance(in float5 distances)
            {
                int indexOfMin = 2;
                indexOfMin = distances[0] < distances[indexOfMin] ? 0 : indexOfMin;
                indexOfMin = distances[1] < distances[indexOfMin] ? 1 : indexOfMin;
                indexOfMin = distances[3] < distances[indexOfMin] ? 3 : indexOfMin;
                indexOfMin = distances[4] < distances[indexOfMin] ? 4 : indexOfMin;
                return indexOfMin;
            }

            unsafe struct float5
            {
                NativeArray<float> values;
                public float this[int i]
                {
                    get
                    {
                        if (!values.IsCreated) { Init(); }
                        return values[i];
                    }
                    set
                    {
                        if (!values.IsCreated) { Init(); }
                        values[i] = value;
                    }
                }

                void Init()
                {
                    values = new NativeArray<float>(5, Allocator.Temp);
                }

                //fixed float f[5];
                //public float this[int i]
                //{
                //    get
                //    {
                //        i = math.clamp(i, 0, 4);
                //        return f[i];
                //    }
                //    set
                //    {
                //        i = math.clamp(i, 0, 4);
                //        f[i] = value;
                //    }
                //}
            }
        }
    }
}