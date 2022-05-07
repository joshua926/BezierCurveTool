using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    public class Path3DFollowBehaviour : MonoBehaviour
    {
        [SerializeField] Path3DBehaviour path;
        [Tooltip("Speed in path percent per second.")]
        [SerializeField] float speed = .01f;
        [SerializeField, Range(0, 1)] float startPercentage = 0;
        float currentPercentage;

        private void OnValidate()
        {
            SetTransform(startPercentage);            
        }

        private void Start()
        {
            currentPercentage = startPercentage;
            SetTransform(startPercentage);
        }

        private void Update()
        {
            currentPercentage += speed * Time.deltaTime;
            SetTransform(currentPercentage);
        }

        void SetTransform(float pathPercent)
        {
            var frame = path.GetLerpedFrame(pathPercent);
            transform.position = frame.position;
            transform.rotation = Quaternion.LookRotation(frame.tangent, frame.normal);
        }
    }
}