using UnityEngine;
using Unity.Mathematics;

namespace BezierCurveDemo
{
    public partial class Path
    {
        [System.Serializable]
        public struct Anchor
        {
            [SerializeField] Vector3 backTangent;
            [SerializeField] Vector3 position;
            [SerializeField] Vector3 frontTangent;
            [SerializeField] HandleType handleSetting;

            public enum HandleType { Aligned = 0, Free = 1 };

            public HandleType HandleSetting
            {
                get => handleSetting;
                set
                {
                    if (value == HandleType.Aligned)
                    {
                        AlignTangents();
                    }
                    handleSetting = value;
                }
            }

            public bool ShouldAlign => HandleSetting == HandleType.Aligned;

            public float3 Position
            {
                get => position;
                set => position = value;
            }

            public float3 BackTangent
            {
                get => backTangent;
                set
                {
                    backTangent = value;
                    if (handleSetting == HandleType.Aligned)
                    {
                        AlignFrontTangent();
                    }
                }
            }

            public float3 FrontTangent
            {
                get => frontTangent;
                set
                {
                    frontTangent = value;
                    if (handleSetting == HandleType.Aligned)
                    {
                        AlignBackTangent();
                    }
                }
            }

            public float3 BackHandle
            {
                get => Position + BackTangent;
                set => BackTangent = value - Position;
            }

            public float3 FrontHandle
            {
                get => Position + FrontTangent;
                set => FrontTangent = value - Position;
            }

            public float FrontTangentLength
            {
                get => math.length(frontTangent);
                set => frontTangent = math.normalize(frontTangent) * value;
            }

            public float BackTangentLength
            {
                get => math.length(backTangent);
                set => backTangent = math.normalize(backTangent) * value;
            }

            public Anchor(float3 backTangent, float3 position, float3 frontTangent, HandleType handleSetting)
            {
                this.backTangent = backTangent;
                this.position = position;
                this.frontTangent = frontTangent;
                this.handleSetting = handleSetting;
                if (handleSetting == HandleType.Aligned)
                {
                    AlignFrontTangent();
                }
            }

            public float3 GetHandle(int i)
            {
                return i == 0 ? BackHandle : FrontHandle;
            }

            public void SetHandle(int i, float3 value)
            {
                if (i == 0)
                {
                    BackHandle = value;
                }
                else
                {
                    FrontHandle = value;
                }
            }

            public float3 GetTangent(int i)
            {
                return i == 0 ? BackTangent : FrontTangent;
            }

            public void SetTangent(int i, float3 value)
            {
                if (i == 0)
                {
                    BackTangent = value;
                }
                else
                {
                    FrontTangent = value;
                }
            }

            public float GetTangentLength(int i)
            {
                return i == 0 ? BackTangentLength : FrontTangentLength;
            }

            public void SetTangentLength(int i, float value)
            {
                if (i == 0)
                {
                    BackTangentLength = value;
                }
                else
                {
                    FrontTangentLength = value;
                }
            }

            public void AlignBackTangent()
            {
                backTangent = math.normalize(-frontTangent) * math.length(backTangent);
            }

            public void AlignFrontTangent()
            {
                frontTangent = math.normalize(-backTangent) * math.length(frontTangent);
            }

            public void AlignTangents()
            {
                AlignFrontTangent();
            }

            public void AutoSetTangents(float3 priorAnchorPosition, float3 nextAnchorPosition)
            {
                if(position.Equals(priorAnchorPosition) || position.Equals(nextAnchorPosition))
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
                backTangent = math.normalize(priorOffset / priorDistance - nextOffset / nextDistance) * priorDistance * defaultHandleLengthMultiplier;
                frontTangent = math.normalize(nextOffset / nextDistance - priorOffset / priorDistance) * nextDistance * defaultHandleLengthMultiplier;
                handleSetting = HandleType.Aligned;
            }

            public void AutoSetTangents(float3 otherAnchorPosition, bool otherAnchorIsNextInPath)
            {
                if (otherAnchorIsNextInPath)
                {
                    float3 tangent = (otherAnchorPosition - Position) * defaultHandleLengthMultiplier;
                    handleSetting = HandleType.Aligned;
                    backTangent = -tangent;
                    frontTangent = tangent;
                }
                else
                {
                    float3 tangent = (otherAnchorPosition - Position) * defaultHandleLengthMultiplier;
                    handleSetting = HandleType.Aligned;
                    frontTangent = -tangent;
                    backTangent = tangent;
                }
            }
        }
    }
}