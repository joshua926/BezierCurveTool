using Unity.Mathematics;

namespace BezierCurveDemo
{
    public partial class Path
    {
        public struct Frame
        {
            public float3 position;
            public float3 tangent;
            public float3 normal;

            public Frame(in Segment segment, float segmentTime)
            {
                position = segment.Position(segmentTime);
                tangent = math.normalize(segment.Tangent(segmentTime));
                float3 acceleration = segment.Acceleration(segmentTime);
                normal = FrenetNormal(tangent, acceleration);

                static float3 FrenetNormal(in float3 normalizedHandle, in float3 acceleration)
                {
                    float3 nextHandleIfCurveKeepsSameAcceleration = math.normalize(normalizedHandle + acceleration);
                    float3 binormal = math.normalize(math.cross(nextHandleIfCurveKeepsSameAcceleration, normalizedHandle));
                    return math.normalize(math.cross(binormal, normalizedHandle));
                }
            }

            public Frame(in Frame priorFrame, in Segment segment, float segmentTime)
            {
                position = segment.Position(segmentTime);
                tangent = math.normalize(segment.Tangent(segmentTime));
                normal = DoubleReflectionNormal(priorFrame, position, tangent);

                static float3 DoubleReflectionNormal(in Frame priorFrame, in float3 position, in float3 tangent)
                {
                    float3 reflectionNormal0 = position - priorFrame.position;
                    if (reflectionNormal0.Equals(0)) { return priorFrame.normal; }
                    float3 binormalReflection = ReflectAlongNormal(reflectionNormal0, priorFrame.normal);
                    float3 handleReflection = ReflectAlongNormal(reflectionNormal0, priorFrame.tangent);
                    float3 reflectionNormal1 = tangent - handleReflection;
                    float3 normal = ReflectAlongNormal(reflectionNormal1, binormalReflection);
                    return normal;

                    float3 ReflectAlongNormal(float3 normal, float3 vectorToReflect)
                    {
                        normal = math.normalize(normal);
                        float distanceToPlane = math.dot(normal, vectorToReflect);
                        return vectorToReflect - 2 * distanceToPlane * normal;
                    }
                    //float3 ReflectAlongNormal(float3 normal, float3 vectorToReflect)
                    //{
                    //    return vectorToReflect - (2 / math.lengthsq(normal)) * math.dot(normal, vectorToReflect) * normal;
                    //}
                }
            }

            public static Frame Interpolate(in Frame f0, in Frame f1, float t)
            {
                float3 position = math.lerp(f0.position, f1.position, t);
                quaternion q0 = quaternion.LookRotation(f0.tangent, f0.normal);
                quaternion q1 = quaternion.LookRotation(f1.tangent, f1.normal);
                quaternion q = math.slerp(q0, q1, t);
                return new Frame
                {
                    position = position,
                    normal = math.mul(q, math.up()),
                    tangent = math.mul(q, math.forward()),
                };
            }
        }
    }
}