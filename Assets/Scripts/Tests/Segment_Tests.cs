using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;

namespace BezierCurve
{
    public class Segment_Tests
    {
        [Test]
        public void Create()
        {
            var segment = new Curve.Segment(0, 1, 2, 3);
            Assert.AreEqual(segment.points[0], new float3(0));
            Assert.AreEqual(segment.points[1], new float3(1));
            Assert.AreEqual(segment.points[2], new float3(2));
            Assert.AreEqual(segment.points[3], new float3(3));

            segment = new Curve.Segment(new float3x4(
                new float3(0, 0, 0),
                new float3(1, 1, 1),
                new float3(2, 2, 2), 
                new float3(3, 3, 3)));
            Assert.AreEqual(segment.points[0], new float3(0));
            Assert.AreEqual(segment.points[1], new float3(1));
            Assert.AreEqual(segment.points[2], new float3(2));
            Assert.AreEqual(segment.points[3], new float3(3));
        }

        [Test]
        public void Position()
        {
            var segment = new Curve.Segment(0, 1, 2, 3);
            var expected = new float3(1.5f);
            var actual = segment.Position(.5f);
            AssertFloat3s(expected, actual);

            segment = new Curve.Segment(
                new float3(3, 1, 0),
                new float3(4, 3, 0),
                new float3(6, 3, 0),
                new float3(7, 1, 0));
            expected = new float3(3.906f, 2.125f, 0);
            actual = segment.Position(.25f);
            AssertFloat3s(expected, actual);

            segment = new Curve.Segment(
                new float3(3.2f, -4.2f, .4f),
                new float3(-6.8f, -5.7f, 5.6f),
                new float3(10.7f, 6, -2.8f),
                new float3(-6.8f, .8f, -8.9f));
            expected = new float3(-3.24f, 1.739f, -7.4f);
            actual = segment.Position(.92f);
            AssertFloat3s(expected, actual);

            var rand = new Unity.Mathematics.Random(4);
            float3 p0 = rand.NextFloat3(-1, 1);
            float3 p1 = rand.NextFloat3(-1, 1);
            float3 p2 = rand.NextFloat3(-1, 1);
            float3 p3 = rand.NextFloat3(-1, 1);
            float t = rand.NextFloat(0, 1);
            expected = WikipediaPosition(p0, p1, p2, p3, t);
            segment = new Curve.Segment(p0, p1, p2, p3);
            actual = segment.Position(t);
            AssertFloat3s(expected, actual);
        }

        [Test]
        public void Tangent()
        {
            var rand = new Unity.Mathematics.Random(10);
            float3 p0 = rand.NextFloat3(-1, 1);
            float3 p1 = rand.NextFloat3(-1, 1);
            float3 p2 = rand.NextFloat3(-1, 1);
            float3 p3 = rand.NextFloat3(-1, 1);
            float t = rand.NextFloat(0, 1);
            var expected = WikipediaDerivative(p0, p1, p2, p3, t);
            var segment = new Curve.Segment(p0, p1, p2, p3);
            var actual = segment.Tangent(t);
            AssertFloat3s(expected, actual);
        }

        [Test]
        public void Acceleration()
        {
            var rand = new Unity.Mathematics.Random(17);
            float3 p0 = rand.NextFloat3(-1, 1);
            float3 p1 = rand.NextFloat3(-1, 1);
            float3 p2 = rand.NextFloat3(-1, 1);
            float3 p3 = rand.NextFloat3(-1, 1);
            float t = rand.NextFloat(0, 1);
            var expected = WikipediaSecondDerivative(p0, p1, p2, p3, t);
            var segment = new Curve.Segment(p0, p1, p2, p3);
            var actual = segment.Acceleration(t);
            AssertFloat3s(expected, actual);
        }

        float3 WikipediaPosition(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float omt = 1 - t;
            return omt * omt * omt * p0
                + 3 * omt * omt * t * p1
                + 3 * omt * t * t * p2
                + t * t * t * p3;
        }

        float3 WikipediaDerivative(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float omt = 1 - t;
            return 3 * omt * omt * (p1 - p0)
                + 6 * omt * t * (p2 - p1)
                + 3 * t * t * (p3 - p2);
        }

        float3 WikipediaSecondDerivative(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float omt = 1 - t;
            return 6 * omt * (p2 - 2 * p1 + p0)
                + 6 * t * (p3 - 2 * p2 + p1);
        }

        void AssertFloat3s(float3 expected, float3 actual, float delta = .001f)
        {
            Assert.AreEqual(expected.x, actual.x, delta);
            Assert.AreEqual(expected.y, actual.y, delta);
            Assert.AreEqual(expected.z, actual.z, delta);
        }
    }
}