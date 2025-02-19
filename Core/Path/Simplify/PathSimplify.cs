﻿using System.Collections.Generic;
using System.Numerics;

namespace Core
{
    public static class PathSimplify
    {
        private static float GetSquareDistance(Vector3 p1, Vector3 p2)
        {
            float dx = p1.X - p2.X,
                dy = p1.Y - p2.Y;

            return (dx * dx) + (dy * dy);
        }

        // square distance from a WowPoint to a segment
        private static float GetSquareSegmentDistance(Vector3 p, Vector3 p1, Vector3 p2)
        {
            var x = p1.X;
            var y = p1.Y;
            var dx = p2.X - x;
            var dy = p2.Y - y;

            if (!dx.Equals(0.0) || !dy.Equals(0.0))
            {
                var t = ((p.X - x) * dx + (p.Y - y) * dy) / (dx * dx + dy * dy);

                if (t > 1)
                {
                    x = p2.X;
                    y = p2.Y;
                }
                else if (t > 0)
                {
                    x += dx * t;
                    y += dy * t;
                }
            }

            dx = p.X - x;
            dy = p.Y - y;

            return (dx * dx) + (dy * dy);
        }

        // basic distance-based simplification
        private static List<Vector3> SimplifyRadialDistance(Vector3[] WowPoints, float sqTolerance)
        {
            var prevWowPoint = WowPoints[0];
            var newWowPoints = new List<Vector3> { prevWowPoint };
            Vector3 WowPoint = Vector3.Zero;

            for (var i = 1; i < WowPoints.Length; i++)
            {
                WowPoint = WowPoints[i];

                if (GetSquareDistance(WowPoint, prevWowPoint) > sqTolerance)
                {
                    newWowPoints.Add(WowPoint);
                    prevWowPoint = WowPoint;
                }
            }

            if (WowPoint != Vector3.Zero && !prevWowPoint.Equals(WowPoint))
                newWowPoints.Add(WowPoint);

            return newWowPoints;
        }

        // simplification using optimized Douglas-Peucker algorithm with recursion elimination
        private static List<Vector3> SimplifyDouglasPeucker(Vector3[] WowPoints, float sqTolerance)
        {
            var len = WowPoints.Length;
            var markers = new int?[len];
            int? first = 0;
            int? last = len - 1;
            int? index = 0;
            var stack = new List<int?>();
            var newWowPoints = new List<Vector3>();

            markers[first.Value] = markers[last.Value] = 1;

            while (last != null)
            {
                var maxSqDist = 0.0d;

                for (int? i = first + 1; first.HasValue && i < last; i++)
                {
                    var sqDist = GetSquareSegmentDistance(WowPoints[i.Value], WowPoints[first.Value], WowPoints[last.Value]);

                    if (sqDist > maxSqDist)
                    {
                        index = i;
                        maxSqDist = sqDist;
                    }
                }

                if (maxSqDist > sqTolerance)
                {
                    markers[index.Value] = 1;
                    stack.AddRange(new[] { first, index, index, last });
                }


                if (stack.Count > 0)
                {
                    last = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                }
                else
                    last = null;

                if (stack.Count > 0)
                {
                    first = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                }
                else
                    first = null;
            }

            for (var i = 0; i < len; i++)
            {
                if (markers[i] != null)
                    newWowPoints.Add(WowPoints[i]);
            }

            return newWowPoints;
        }

        /// <summary>
        /// Simplifies a list of WowPoints to a shorter list of WowPoints.
        /// </summary>
        /// <param name="WowPoints">WowPoints original list of WowPoints</param>
        /// <param name="tolerance">Tolerance tolerance in the same measurement as the WowPoint coordinates</param>
        /// <param name="highestQuality">Enable highest quality for using Douglas-Peucker, set false for Radial-Distance algorithm</param>
        /// <returns>Simplified list of WowPoints</returns>
        public static List<Vector3> Simplify(Vector3[] WowPoints, float tolerance = 0.3f, bool highestQuality = false)
        {
            if (WowPoints == null || WowPoints.Length == 0)
                return new List<Vector3>();

            var sqTolerance = tolerance * tolerance;

            if (highestQuality)
                return SimplifyDouglasPeucker(WowPoints, sqTolerance);

            List<Vector3> WowPoints2 = SimplifyRadialDistance(WowPoints, sqTolerance);
            return SimplifyDouglasPeucker(WowPoints2.ToArray(), sqTolerance);
        }

        /// <summary>
        /// Simplifies a list of WowPoints to a shorter list of WowPoints.
        /// </summary>
        /// <param name="WowPoints">WowPoints original list of WowPoints</param>
        /// <param name="tolerance">Tolerance tolerance in the same measurement as the WowPoint coordinates</param>
        /// <param name="highestQuality">Enable highest quality for using Douglas-Peucker, set false for Radial-Distance algorithm</param>
        /// <returns>Simplified list of WowPoints</returns>
        public static List<Vector3> SimplifyArray(Vector3[] WowPoints, float tolerance = 0.3f, bool highestQuality = false)
        {
            return Simplify(WowPoints, tolerance, highestQuality);
        }
    }
}
