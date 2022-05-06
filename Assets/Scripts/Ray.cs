using Unity.Mathematics;

namespace BezierCurve
{
    [System.Serializable]
    public struct Ray
    {
        public readonly float3 origin;
        public readonly float3 direction;

        public Ray(float3 origin, float3 direction)
        {
            this.origin = origin;
            this.direction = math.normalize(direction);
        }

        public static implicit operator Ray(UnityEngine.Ray ray)
        {
            return new Ray(ray.origin, ray.direction);
        }

        public float3 Projection(float3 point)
        {
            float projectionDistanceFromOrigin = math.dot(point - origin, direction);
            return origin + direction * projectionDistanceFromOrigin;
        }

        public float Distancesq(float3 point)
        {
            return math.distancesq(Projection(point), point);
        }

        public float3 GetPoint(float distance)
        {
            return origin + direction * distance;
        }
    }
}