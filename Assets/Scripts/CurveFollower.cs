using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    public class CurveFollower : MonoBehaviour
    {
        [SerializeField] Curve curve;
        [Tooltip("Speed in curve time per second.")]
        [SerializeField] float speed = .01f;
        [SerializeField] float startPercentage = 0;
        float currentPercentage;

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.Undo.RecordObject(transform, "Set position to curve");
            SetTransform(startPercentage);
        }
#endif

        private void Start()
        {
            currentPercentage = startPercentage;
            SetTransform(currentPercentage);
        }

        private void Update()
        {
            currentPercentage += speed * Time.deltaTime;
            SetTransform(currentPercentage);
        }

        void SetTransform(float curveTime)
        {
            if (curve)
            {
                float t = curveTime % 1f;
                var frame = curve.Cache.GetFrameAtTime(t);
                transform.position = frame.position;
                transform.rotation = Quaternion.LookRotation(frame.tangent, frame.normal);
            }
        }
    }
}