using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    public class Path2DBehaviour : MonoBehaviour
    {
        [SerializeField] Path2D path;
        [SerializeField, Min(2)] int frameCount;
        [SerializeField] Bezier.StepSetting stepSetting;
        [System.NonSerialized] Path2DFrames frames;        

        void OnEnable()
        {
            if (frames == null || frames.Length <= 1)
            {
                CreateFrames();
            }
        }

        public Bezier.Frame2D GetLerpedFrame(float frameArrayTime)
        {
            if (frames == null || frames.Length <= 1)
            {
                CreateFrames();
            }
            return frames.Lerp(frameArrayTime);
        }

        void CreateFrames()
        {
            frames = new Path2DFrames(frameCount, stepSetting);
            frames.Recalculate(path);
        }
    }
}