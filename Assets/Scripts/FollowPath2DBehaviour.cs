using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierCurve
{
    public class FollowPath2DBehaviour : MonoBehaviour
    {
        [SerializeField] Path2DSO path;
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
            transform.position = new Vector3(frame.position.x, frame.position.y, 0);
            Vector3 tangent = new Vector3(frame.tangent.x, frame.tangent.y, 0);
            Vector3 normal = new Vector3(frame.normal.x, frame.normal.y, 0);
            transform.rotation = Quaternion.LookRotation(tangent, normal);
        }
    }
}