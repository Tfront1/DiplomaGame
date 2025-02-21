using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder
{
    private static PathFinder _instance;
    private static readonly object _lock = new();
    private static LayerMask obstacleMask;
    private static float avoidanceOffset;

    public static PathFinder Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new PathFinder();
                    Debug.Log(obstacleMask);
                    Debug.Log(avoidanceOffset);
                }
                return _instance;
            }
        }
    }

    private PathFinder()
    {
        avoidanceOffset = 0.1f;
        obstacleMask = LayerMask.GetMask("Objects");
    }


    public List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        if (Physics2D.OverlapPoint(start, obstacleMask))
        {
            Debug.LogWarning("Start point inside of a collision");
            return null;
        }

        if (Physics2D.OverlapPoint(end, obstacleMask))
        {
            Debug.LogWarning("End point inside of a collision.");
            return null;
        }

        var path = new List<Vector2> { start };

        if (!Physics2D.Raycast(start, end - start, Vector2.Distance(start, end), obstacleMask))
        {
            path.Add(end);
            return path;
        }

        var visited = new HashSet<Vector2>();
        var cameFrom = new Dictionary<Vector2, Vector2>();

        var openSet = new List<NodeWithPriority>();
        var openSetContents = new HashSet<Vector2>();

        openSet.Add(new NodeWithPriority(start, 0, Vector2.Distance(start, end)));
        openSetContents.Add(start);

        var gScore = new Dictionary<Vector2, float>
        {
            { start, 0 }
        };

        var fScore = new Dictionary<Vector2, float>
        {
            { start, Vector2.Distance(start, end) }
        };

        while (openSet.Count > 0)
        {
            if (openSet.Count > 10000)
                return null;

            var current = openSet[0].Position;
            openSet.RemoveAt(0);
            openSetContents.Remove(current);

            if (!Physics2D.Raycast(current, end - current, Vector2.Distance(current, end), obstacleMask))
            {
                var finalPath = ReconstructPath(cameFrom, current);
                finalPath.Add(end);
                return finalPath;
            }

            visited.Add(current);

            var neighbors = FindNeighborsAlongEdges(current, end, visited);

            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor))
                    continue;

                if (Physics2D.Raycast(current, neighbor - current, Vector2.Distance(current, neighbor), obstacleMask))
                {
                    continue;
                }

                var tentativeGScore = gScore[current] + Vector2.Distance(current, neighbor);

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Vector2.Distance(neighbor, end);

                    if (!openSetContents.Contains(neighbor))
                    {
                        InsertOrdered(openSet, new NodeWithPriority(neighbor, gScore[neighbor], fScore[neighbor]));
                        openSetContents.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }
    private static void InsertOrdered(List<NodeWithPriority> list, NodeWithPriority node)
    {
        var i = 0;
        while (i < list.Count && list[i].FScore < node.FScore)
        {
            i++;
        }
        list.Insert(i, node);
    }

    private static List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        var totalPath = new List<Vector2> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }

    private static List<Vector2> FindNeighborsAlongEdges(Vector2 current, Vector2 goal, HashSet<Vector2> visited)
    {
        var neighbors = new HashSet<Vector2>();

        var hitToGoal = Physics2D.Raycast(current, goal - current, Vector2.Distance(current, goal), obstacleMask);

        if (hitToGoal)
        {
            var obstacle = hitToGoal.collider;

            var exploredColliders = new HashSet<Collider2D> { obstacle };

            var collidersToExplore = new Queue<Collider2D>();
            collidersToExplore.Enqueue(obstacle);

            while (collidersToExplore.Count > 0)
            {
                var currentObstacle = collidersToExplore.Dequeue();
                var currentBounds = currentObstacle.bounds;

                var edgePoints = new List<Vector2>
            {
                new(currentBounds.min.x - avoidanceOffset, currentBounds.min.y - avoidanceOffset),
                new(currentBounds.max.x + avoidanceOffset, currentBounds.min.y - avoidanceOffset),
                new(currentBounds.min.x - avoidanceOffset, currentBounds.max.y + avoidanceOffset),
                new(currentBounds.max.x + avoidanceOffset, currentBounds.max.y + avoidanceOffset)
            };

                var numPointsPerEdge = 3;

                // Bottom edge
                for (var i = 1; i < numPointsPerEdge; i++)
                {
                    var x = Mathf.Lerp(currentBounds.min.x - avoidanceOffset, currentBounds.max.x + avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(x, currentBounds.min.y - avoidanceOffset));
                }

                // Top edge
                for (var i = 1; i < numPointsPerEdge; i++)
                {
                    var x = Mathf.Lerp(currentBounds.min.x - avoidanceOffset, currentBounds.max.x + avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(x, currentBounds.max.y + avoidanceOffset));
                }

                // Left edge
                for (var i = 1; i < numPointsPerEdge; i++)
                {
                    var y = Mathf.Lerp(currentBounds.min.y - avoidanceOffset, currentBounds.max.y + avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(currentBounds.min.x - avoidanceOffset, y));
                }

                // Right edge
                for (var i = 1; i < numPointsPerEdge; i++)
                {
                    var y = Mathf.Lerp(currentBounds.min.y - avoidanceOffset, currentBounds.max.y + avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(currentBounds.max.x + avoidanceOffset, y));
                }

                foreach (var point in edgePoints)
                {
                    var nearbyColliders = Physics2D.OverlapCircleAll(point, avoidanceOffset, obstacleMask);

                    var shouldAddPoint = true;

                    foreach (var collider in nearbyColliders)
                    {
                        if (collider == currentObstacle)
                            continue;

                        if (!exploredColliders.Contains(collider))
                        {
                            exploredColliders.Add(collider);
                            collidersToExplore.Enqueue(collider);
                        }

                        shouldAddPoint = false;
                        break;
                    }

                    if (shouldAddPoint && !visited.Contains(point) &&
                        !Physics2D.Raycast(current, point - current, Vector2.Distance(current, point), obstacleMask))
                    {
                        neighbors.Add(point);
                    }
                }
            }
        }

        return neighbors.ToList();
    }

    private class NodeWithPriority
    {
        public Vector2 Position;
        public float GScore;
        public float FScore;

        public NodeWithPriority(Vector2 position, float gScore, float fScore)
        {
            Position = position;
            GScore = gScore;
            FScore = fScore;
        }
    }
}
