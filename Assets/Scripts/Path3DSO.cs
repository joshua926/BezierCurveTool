using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    [CreateAssetMenu(fileName = "Path3D Asset", menuName = "Path3D Asset")]
    public class Path3DSO : ScriptableObject
    {
        [SerializeField] Path3D path;
        [SerializeField, Min(2)] int frameCount;
        [SerializeField] Bezier.StepSetting stepSetting = Bezier.StepSetting.Distance;
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