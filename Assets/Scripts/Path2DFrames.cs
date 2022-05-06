using System.Collections.Generic;
using Unity.Mathematics;

namespace BezierCurve
{
    public class Path2DFrames
    {
        public Bezier.StepSetting stepSetting;
        Bezier.Frame2D[] frames;
        public int Length
        {
            get => frames.Length;
            set
            {
                if (frames == null || frames.Length != value)
                {
                    frames = new Bezier.Frame2D[value];
                }
            }
        }
        public Bezier.Frame2D this[int i] => frames[i];

        public Path2DFrames(int frameCount, Bezier.StepSetting stepSetting)
        {
            this.stepSetting = stepSetting;
            frames = new Bezier.Frame2D[frameCount];
        }

        public void Recalculate(Path2D path)
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

        public Bezier.Frame2D Lerp(float frameArrayTime)
        {
            frameArrayTime = frameArrayTime == 1f ? frameArrayTime : frameArrayTime % 1;
            float index = frameArrayTime * (frames.Length - 1);
            int indexFloor = (int)math.floor(index);
            int indexCeil = math.clamp(indexFloor + 1, 0, frames.Length - 1);
            float segmentTime = index - indexFloor;
            var frame0 = frames[indexFloor];
            var frame1 = frames[indexCeil];
            float2 tangent = math.normalize(math.lerp(frame0.tangent, frame1.tangent, segmentTime));
            return new Bezier.Frame2D
            {
                position = math.lerp(frame0.position, frame1.position, segmentTime),
                normal = new float2(tangent.y, -tangent.x), // this rotates a vector 90 degrees clockwise
                pathTime = math.lerp(frame0.pathTime, frame1.pathTime, segmentTime),
            };
        }
    }
}