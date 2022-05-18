using UnityEngine;
using Unity.Mathematics;

namespace BezierCurve
{
    public partial class Curve
    {
        [System.Serializable]
        public struct Anchor
        {
            [SerializeField] Vector3 position;
            [SerializeField] Vector3 frontTangentNormalized;
            [SerializeField] float frontLength;
            [SerializeField] float backLength;

            public float3 Position
            {
                get => position;
                set => position = value;
            }
            public float FrontTangentLength
            {
                get => frontLength;
                set => frontLength = value;
            }

            public float BackTangentLength
            {
                get => backLength;
                set => backLength = value;
            }

            public float3 BackTangent
            {
                get => -frontTangentNormalized * backLength;
                set
                {
                    backLength = math.length(value);
                    frontTangentNormalized = -(value / backLength);
                }
            }

            public float3 FrontTangent
            {
                get => frontTangentNormalized * frontLength;
                set
                {
                    frontLength = math.length(value);
                    frontTangentNormalized = value / frontLength;
                }
            }

            public float3 BackTangentNormalized => -frontTangentNormalized;
            public float3 FrontTangentNormalized => frontTangentNormalized;

            public float3 BackHandle
            {
                get => position - frontTangentNormalized * backLength;
                set => BackTangent = value - Position;                
            }
            public float3 FrontHandle
            {
                get => position + frontTangentNormalized * frontLength;
                set => FrontTangent = value - Position;
            }


            public Anchor(float3 position, float3 frontTangent)
            {
                this.position = position;
                frontLength = math.length(frontTangent);
                this.frontTangentNormalized = frontTangent / frontLength;
                this.backLength = frontLength;
            }

            public void AutoSetTangents(float3 priorAnchorPosition, float3 nextAnchorPosition)
            {
                if (position.Equals(priorAnchorPosition) || position.Equals(nextAnchorPosition))
                {
                    position += (Vector3)new float3(.01f);
                }
                if (priorAnchorPosition.Equals(nextAnchorPosition))
                {
                    priorAnchorPosition += new float3(.01f);
                }
                float3 priorOffset = priorAnchorPosition - Position;
                float3 nextOffset = nextAnchorPosition - Position;
                float priorDistance = math.length(priorOffset);
                float nextDistance = math.length(nextOffset);
                frontTangentNormalized = math.normalize(nextOffset / nextDistance - priorOffset / priorDistance);
                frontLength = nextDistance * defaultHandleLengthMultiplier;
                backLength = priorDistance * defaultHandleLengthMultiplier;
            }

            public void AutoSetTangentsForEndAnchor(float3 otherAnchorPosition, bool thisAnchorIsTheFirst)
            {
                float3 offset = otherAnchorPosition - Position;
                float length = math.length(offset);
                float3 tangent = offset / length;
                int multiplier = thisAnchorIsTheFirst ? 1 : -1;
                frontTangentNormalized = tangent * multiplier;
                backLength = length * defaultHandleLengthMultiplier;
                frontLength = length * defaultHandleLengthMultiplier;
            }
        }
    }
}