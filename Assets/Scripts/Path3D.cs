using UnityEngine;
using Unity.Mathematics;

namespace BezierCurve
{
    [System.Serializable]
    public class Path3D
    {
        [SerializeField] Bezier.Point3D[] points;
        [SerializeField] bool isLoop;
        const float defaultTangentLengthMultiplier = .3f;
        public int PointCount => points.Length;
        public int SegmentCount => isLoop ? points.Length : points.Length - 1;
        public bool IsLoop => isLoop;
        public Bezier.Point3D this[int pointIndex] => points[pointIndex];

        public Path3D()
        {
            Reset();
        }

        public void Reset()
        {
            isLoop = false;
            points = new Bezier.Point3D[]
            {
                new Bezier.Point3D
                {
                    backTangent = new float3(0, -2, 0),
                    position = 0,
                    frontTangent = new float3(0, 2, 0),
                    tangentSetting = Bezier.TangentSetting.Aligned,
                },
                new Bezier.Point3D
                {
                    backTangent = new float3(-.5f, 1, 0),
                    position = new float3(1.5f, 2, 0),
                    frontTangent = new float3(.5f, -1f, 0),
                    tangentSetting = Bezier.TangentSetting.Aligned,
                },
            };
        }

        public float3x4 GetSegment(int i)
        {
            var p0 = points[Bezier.GetIndex(i + 0, points.Length, IsLoop)];
            var p1 = points[Bezier.GetIndex(i + 1, points.Length, IsLoop)];
            return new float3x4(
                p0.position,
                p0.FrontHandle,
                p1.BackHandle,
                p1.position);
        }

        public void SetPosition(int i, float3 value)
        {
            var point = points[i];
            point.position = value;
            points[i] = point;
        }

        public void SetStartTangent(int i, float3 value)
        {
            var point = points[i];
            point.backTangent = value;
            if (point.tangentSetting == Bezier.TangentSetting.Aligned)
            {
                AlignTangents(ref point.frontTangent, point.backTangent);
            }
            points[i] = point;
        }

        public void SetEndTangent(int i, float3 value)
        {
            var point = points[i];
            point.frontTangent = value;
            if (point.tangentSetting == Bezier.TangentSetting.Aligned)
            {
                AlignTangents(ref point.backTangent, point.frontTangent);
            }
            points[i] = point;
        }

        static void AlignTangents(ref float3 tangentToAlign, float3 otherTangent)
        {
            float length = math.length(tangentToAlign);
            float3 direction = math.normalize(-otherTangent);
            tangentToAlign = direction * length;
        }

        public void SetStartTangentLength(int i, float length)
        {
            var point = points[i];
            point.backTangent = math.normalize(point.backTangent) * length;
            points[i] = point;
        }

        public void SetEndTangentLength(int i, float length)
        {
            var point = points[i];
            point.frontTangent = math.normalize(point.frontTangent) * length;
            points[i] = point;
        }

        public void AddPoint(float3 position)
        {
            if (isLoop) { return; }
            Bezier.Point3D lastPoint = points[points.Length - 1];
            if (position.Equals(lastPoint.position))
            {
                position.y += .01f;
            }
            float3 offset = (position - lastPoint.FrontHandle) * defaultTangentLengthMultiplier;
            var point = new Bezier.Point3D
            {
                backTangent = -offset,
                position = position,
                frontTangent = offset,
                tangentSetting = Bezier.TangentSetting.Aligned,
            };
            ArrayUtility.Add(ref points, point);
        }

        public void InsertPoint(float3 position, float time)
        {
            int p0Index = Bezier.GetIndex((int)math.floor(time * (points.Length - 1)), points.Length, IsLoop);
            int p2Index = Bezier.GetIndex(p0Index + 1, points.Length, IsLoop);
            Bezier.Point3D p0 = points[p0Index];
            Bezier.Point3D p2 = points[p2Index];
            if (position.Equals(p0.position))
            {
                position.y += .01f;
            }
            if (position.Equals(p2.position))
            {
                position.y += .01f;
            }
            float p0Distance = math.distance(p0.position, position);
            float p2Distance = math.distance(p2.position, position);
            float3 p0Direction = (p0.position - position) / p0Distance;
            float3 p2Direction = (p2.position - position) / p2Distance;
            var point = new Bezier.Point3D
            {
                backTangent = math.normalize(p0Direction - p2Direction) * p0Distance * defaultTangentLengthMultiplier,
                position = position,
                frontTangent = math.normalize(p2Direction - p0Direction) * p2Distance * defaultTangentLengthMultiplier,
                tangentSetting = Bezier.TangentSetting.Aligned,
            };
            ArrayUtility.Insert(ref points, point, p0Index + 1);
        }

        public void DeletePoint(int i)
        {
            ArrayUtility.RemoveAt(ref points, i);
        }

        public void GetFramesAtTimeSteps(ref Bezier.Frame3D[] frames)
        {
            Bezier.GetFramesAtTimeSteps(points, ref frames, IsLoop);
        }

        public void GetFramesAtDistanceSteps(ref Bezier.Frame3D[] frames)
        {
            Bezier.GetFramesAtDistanceSteps(points, ref frames, IsLoop);
        }

        public int GetIndexOfNearestSegmentPoint(in Ray ray)
        {
            return Bezier.GetIndexOfNearestSegmentPoint(points, ray);
        }

        public float3 ProjectPosition(in Ray ray, out float time, int refineCount = 10)
        {
            return Bezier.ProjectRay(points, isLoop, ray, refineCount, out time);
        }
    }
}