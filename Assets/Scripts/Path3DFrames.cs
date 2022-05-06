using System.Collections.Generic;
using Unity.Mathematics;

namespace BezierCurve
{
    public class Path3DFrames
    {
        public Bezier.StepSetting stepSetting;
        Bezier.Frame3D[] frames;
        public int Length
        {
            get => frames.Length;
            set
            {
                if (frames == null || frames.Length != value)
                {
                    frames = new Bezier.Frame3D[value];
                }
            }
        }
        public Bezier.Frame3D this[int i] => frames[i];

        public Path3DFrames(int frameCount, Bezier.StepSetting stepSetting)
        {
            this.stepSetting = stepSetting;
            frames = new Bezier.Frame3D[frameCount];
        }

        public void Recalculate(Path3D path)
        {
            if (path == null || path.PointCount <= 1) { return; }
            if (frames == null || frames.Length == 0) { return; }
            if (stepSetting == Bezier.StepSetting.PathTime)
            {
                path.GetFramesAtTimeSteps(ref frames);
            }
            else if (stepSetting == Bezier.StepSetting.Distance)
            {
                path.GetFramesAtDistanceSteps(ref frames);
            }
        }

        public Bezier.Frame3D Lerp(float frameArrayTime)
        {
            frameArrayTime = frameArrayTime == 1f ? frameArrayTime : frameArrayTime % 1;
            float index = frameArrayTime * (frames.Length - 1);
            int indexFloor = (int)math.floor(index);
            int indexCeil = math.clamp(indexFloor + 1, 0, frames.Length - 1);
            float segmentTime = index - indexFloor;
            var frame0 = frames[indexFloor];
            var frame1 = frames[indexCeil];
            float3 tangent = math.normalize(math.lerp(frame0.tangent, frame1.tangent, segmentTime));
            float3 normal = math.normalize(math.lerp(frame0.normal, frame1.normal, segmentTime));
            normal = ProjectVectorOntoPlaneAtOrigin(tangent, normal);
            normal = math.normalize(normal);
            return new Bezier.Frame3D
            {
                position = math.lerp(frame0.position, frame1.position, segmentTime),
                tangent = tangent,
                normal = normal,
                pathTime = math.lerp(frame0.pathTime, frame1.pathTime, segmentTime),
            };

            float3 ProjectVectorOntoPlaneAtOrigin(float3 normalizedPlaneNormal, float3 v)
            {
                return v - normalizedPlaneNormal * math.dot(normalizedPlaneNormal, v);
            }
        }
    }
}