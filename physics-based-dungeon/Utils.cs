using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Utils : Node
{
	public const float Uncertainty = 0.01f;

    public static Vector2 GetRngPointInCircle(RandomNumberGenerator rng, float radius)
    {
        return GetRngPointInEllipse(rng, radius, radius);
    }

    public static Vector2 GetRngPointInEllipse(RandomNumberGenerator rng, float width, float height)
    {
        //get a random number in [0, 2PI]
        var t = 2 * Math.PI * rng.Randf();

        //Adding two random number allows us to get a uniform distribution of points in the ellipse
        var u = rng.Randf() + rng.Randf();

        //Calculate a random factor in [0,1]
        var r = u > 1 ? 2 - u : u;

        //Calculate the coordinates of the point in the ellipse
        return r * new Vector2((float)(width * Math.Cos(t)), (float)(height * Math.Sin(t)));
    }

    public static Vector2 IndexToXY(int width, int index)
    {
        return new Vector2(index % width, index / width);
    }

    // Tests for approximate equality between two `Vector2`, allowing you to specify an absolute error margin.
    public static bool IsApproxEqual(Vector2 v1, Vector2 v2, float error = Uncertainty)
    {
        var isAproxEqual = Math.Abs(v1.x - v2.x) < error && Math.Abs(v1.y - v2.y) < error;
        GD.Print("IsApproxEqual: " + isAproxEqual);
        return isAproxEqual;
    }

    //Calculates the Minimum Spanning Tree (MST) for given points and returns an `AStar2D` graph using Prim's algorithm.
    // https://en.wikipedia.org/wiki/Prim%27s_algorithm
    // https://en.wikipedia.org/wiki/Minimum_spanning_tree
    public static AStar2D MST(List<Vector2> pointsList)
    {
        var result = new AStar2D();

        var firstPoint = pointsList.LastOrDefault();
        //Start from an arbitrary point in the list of points
        result.AddPoint(result.GetAvailablePointId(), firstPoint);
        pointsList.Remove(firstPoint);

        //Loop through all points, erasing them as we connect them.
        while (pointsList.Any())
        {
            var currentPosition = Vector2.Zero;
            var minPosition = Vector2.Zero;
            var minDistance = float.PositiveInfinity;

            foreach (int point1Id in result.GetPoints())
            {
                //Compare each point added to the Astar2D graph to each remaining point to find the closest one
                var point1Position = result.GetPointPosition(point1Id);
                foreach (var point2Position in pointsList)
                {
                    var distance = point1Position.DistanceTo(point2Position);
                    if (minDistance > distance)
                    {
                        //We use the variables to store the coordinates of the closest point.
                        //We have to loop over all points to ensure it's the closest.
                        currentPosition = point1Position;
                        minPosition = point2Position;
                        minDistance = distance;
                    }
                }
            }

            //Connect the point closest to the "current position" with our new point
            var pointId = result.GetAvailablePointId();
            result.AddPoint(pointId, minPosition);
            result.ConnectPoints(result.GetClosestPoint(currentPosition), pointId);
            pointsList.Remove(minPosition);
        }

        return result;
    }
}
