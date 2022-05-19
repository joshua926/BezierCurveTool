using Unity.Mathematics;

namespace BezierCurve
{
    [System.Serializable]
    public struct Ray
    {
        public float3 origin;
        float3 directionNormalized;
        public float3 Direction
        {
            get => directionNormalized;
            set => directionNormalized = math.normalize(value);
        }

        public Ray(float3 origin, float3 direction)
        {
            this.origin = origin;
            this.directionNormalized = math.normalize(direction);
        }

        public static implicit operator Ray(UnityEngine.Ray ray)
        {            
            return new Ray(ray.origin, ray.direction);
        }

        public float3 Projection(float3 point)
        {
            return origin + Direction * ProjectionTime(point);
        }

        public float ProjectionTime(float3 point)
        {
            return math.dot(point - origin, directionNormalized);
        }

        public float ProjectionDistanceSq(float3 point)
        {
            return math.distancesq(Projection(point), point);
        }

        /// <summary>
        /// Ray direction is normalized, so ray time and distance along ray are equal.
        /// </summary>
        public float3 GetPoint(float time)
        {
            return origin + Direction * time;
        }
    }
}