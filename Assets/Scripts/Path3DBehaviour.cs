using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    public class Path3DBehaviour : MonoBehaviour
    {
        [SerializeField] Path3D path;
        [SerializeField, Min(2)] int frameCount;
        [SerializeField] Bezier.StepSetting stepSetting;
        [System.NonSerialized] Path3DFrames frames;

        void OnEnable()
        {
            if (frames == null || frames.Length <= 1)
            {
                CreateFrames();
            }
        }

        public Bezier.Frame3D GetLerpedFrame(float frameArrayTime)
        {
            if (frames == null || frames.Length <= 1)
            {
                CreateFrames();
            }
            return frames.Lerp(frameArrayTime);
        }

        void CreateFrames()
        {
            frames = new Path3DFrames(frameCount, stepSetting);
            frames.Recalculate(path);
        }
    }
}