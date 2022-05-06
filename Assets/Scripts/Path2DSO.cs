using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    [CreateAssetMenu(fileName = "Path2D Asset", menuName = "Path2D Asset")]
    public class Path2DSO : ScriptableObject
    {
        [SerializeField] Path2D path;
        [SerializeField, Min(2)] int frameCount;
        [SerializeField] Bezier.StepSetting stepSetting = Bezier.StepSetting.Distance;
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