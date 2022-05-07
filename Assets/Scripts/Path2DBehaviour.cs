using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    public class Path2DBehaviour : MonoBehaviour
    {
        [SerializeField] Path2D path;
        [SerializeField, Min(2)] int frameCount = 32;
        [SerializeField] Bezier.StepSetting stepSetting = Bezier.StepSetting.Distance;
        [System.NonSerialized] Path2DFrames frames;

        void Awake()
        {
            TryCreateFrames();
        }

        public Bezier.Frame2D GetLerpedFrame(float frameArrayTime)
        {
            return frames.Lerp(frameArrayTime);
        }

        void TryCreateFrames()
        {
            if (frames == null || frames.Length != frameCount)
            {
                frames = new Path2DFrames(frameCount, stepSetting);
                frames.Recalculate(path);
            }
        }
    }
}