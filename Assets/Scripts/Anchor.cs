using UnityEngine;
using Unity.Mathematics;

namespace BezierCurve
{
    [System.Serializable]
    public struct Anchor
    {
        [SerializeField] float3 backTangent;
        [SerializeField] float3 position;
        [SerializeField] float3 frontTangent;
        [SerializeField] HandleSetting handleSetting;

        public enum HandleSetting { Aligned = 0, Free = 1 };

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
                if (handleSetting == HandleSetting.Aligned)
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
                if (handleSetting == HandleSetting.Aligned)
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

        public Anchor(float3 backTangent, float3 position, float3 frontTangent, HandleSetting handleSetting)
        {
            this.backTangent = backTangent;
            this.position = position;
            this.frontTangent = frontTangent;
            this.handleSetting = handleSetting;
            if (handleSetting == HandleSetting.Aligned)
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
    }
}