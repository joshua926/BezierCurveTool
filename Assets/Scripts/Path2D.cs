using UnityEngine;
using Unity.Mathematics;

namespace BezierCurve
{
    [System.Serializable]
    public class Path2D
    {
        [SerializeField] Bezier.Point2D[] points;
        [SerializeField] bool isLoop;
        const float defaultTangentLengthMultiplier = .3f;
        public int PointCount => points.Length;
        public int SegmentCount => isLoop ? points.Length : points.Length - 1;
        public bool IsLoop => isLoop;
        public Bezier.Point2D this[int pointIndex] => points[pointIndex];

        public Path2D()
        {
            Reset();
        }

        public void Reset()
        {
            isLoop = false;
            points = new Bezier.Point2D[]
            {
                new Bezier.Point2D
                {
                    backTangent = new float2(0, -.25f),
                    position = 0,
                    frontTangent = new float2(0, .25f),
                    tangentSetting = Bezier.TangentSetting.Aligned,
                },
                new Bezier.Point2D
                {
                    backTangent = new float2(0, .25f),
                    position = new float2(.5f, 0),
                    frontTangent = new float2(0, -.25f),
                    tangentSetting = Bezier.TangentSetting.Aligned,
                },
            };
        }

        public float2x4 GetSegment(int i)
        {
            var p0 = points[Bezier.GetIndex(i + 0, points.Length, IsLoop)];
            var p1 = points[Bezier.GetIndex(i + 1, points.Length, IsLoop)];
            return new float2x4(
                p0.position,
                p0.FrontHandle,
                p1.BackHandle,
                p1.position);
        }

        public void SetPosition(int i, float2 value)
        {
            var point = points[i];
            point.position = value;
            points[i] = point;
        }

        public void SetStartTangent(int i, float2 value)
        {
            var point = points[i];
            point.backTangent = value;
            if (point.tangentSetting == Bezier.TangentSetting.Aligned)
            {
                AlignTangents(ref point.frontTangent, point.backTangent);
            }
            points[i] = point;
        }

        public void SetEndTangent(int i, float2 value)
        {
            var point = points[i];
            point.frontTangent = value;
            if (point.tangentSetting == Bezier.TangentSetting.Aligned)
            {
                AlignTangents(ref point.backTangent, point.frontTangent);
            }
            points[i] = point;
        }

        static void AlignTangents(ref float2 tangentToAlign, float2 otherTangent)
        {
            float length = math.length(tangentToAlign);
            float2 direction = math.normalize(-otherTangent);
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

        public void AddPoint(float2 position)
        {
            if (isLoop) { return; }
            Bezier.Point2D lastPoint = points[points.Length - 1];
            if (position.Equals(lastPoint.position))
            {
                position.y += .01f;
            }
            float2 offset = (position - lastPoint.FrontHandle) * defaultTangentLengthMultiplier;
            var point = new Bezier.Point2D
            {
                backTangent = -offset,
                position = position,
                frontTangent = offset,
                tangentSetting = Bezier.TangentSetting.Aligned,
            };
            ArrayUtility.Add(ref points, point);
        }

        public void InsertPoint(float2 position, float time)
        {
            int p0Index = Bezier.GetIndex((int)math.floor(time * (points.Length - 1)), points.Length, IsLoop);
            int p2Index = Bezier.GetIndex(p0Index + 1, points.Length, IsLoop);
            Bezier.Point2D p0 = points[p0Index];
            Bezier.Point2D p2 = points[p2Index];
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
            float2 p0Direction = (p0.position - position) / p0Distance;
            float2 p2Direction = (p2.position - position) / p2Distance;
            var point = new Bezier.Point2D
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

        public void GetFramesAtTimeSteps(ref Bezier.Frame2D[] frames)
        {
            Bezier.GetFramesAtTimeSteps(points, ref frames, IsLoop);
        }

        public void GetFramesAtDistanceSteps(ref Bezier.Frame2D[] frames)
        {
            Bezier.GetFramesAtDistanceSteps(points, ref frames, IsLoop);
        }

        public Bezier.Frame2D GetFrameAtTime(float time)
        {
            return Bezier.GetFrameAtTime(points, time, IsLoop);
        }

        public int GetIndexOfNearestSegmentPoint(float2 position)
        {
            return Bezier.GetIndexOfNearestSegmentPoint(points, position);
        }

        public float2 ProjectPosition(float2 position, out float time, int refineCount = 10)
        {
            return Bezier.ProjectPosition(points, isLoop, position, refineCount, out time);
        }
    }
}