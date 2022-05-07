using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    public partial class Path3DBehaviour : MonoBehaviour
    {
        [SerializeField] Path3D path;
        [SerializeField, Min(2)] int frameCount = 32;
        [SerializeField] Bezier.StepSetting stepSetting = Bezier.StepSetting.Distance;
        [System.NonSerialized] Path3DFrames frames;        

        void Awake()
        {
            TryCreateFrames();
        }

        public Bezier.Frame3D GetLerpedFrame(float frameArrayTime)
        {
            return frames.Lerp(frameArrayTime);
        }

        void TryCreateFrames()
        {
            if (frames == null || frames.Length != frameCount)
            {
                frames = new Path3DFrames(frameCount, stepSetting);
                frames.Recalculate(path);
            }
        }
    }
}