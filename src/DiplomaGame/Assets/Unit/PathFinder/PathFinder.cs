using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder
{
    private static PathFinder _instance;
    private static readonly object _lock = new();
    private static LayerMask _obstacleMask;
    private static float _avoidanceOffset;

    public static PathFinder Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new PathFinder();
                }
                return _instance;
            }
        }
    }

    private PathFinder()
    {
        _avoidanceOffset = 0.1f;
        _obstacleMask = LayerMask.GetMask("Objects");
    }


    /// <param name="controlPoints">List of path points</param>
    /// <param name="currentPoint">Current point, where is unit</param>
    /// <param name="endPoint">End point of path</param>
    /// <param name="moveCloseToObject">To move close to object</param>
    /// <param name="partialPath">To find not completed path</param>
    /// <returns></returns>
    public List<Vector2> RefindPath(List<Vector2> controlPoints, Vector2 currentPoint, Vector2 endPoint, bool moveCloseToObject, bool partialPath)
    {
        if (controlPoints == null)
        {
            if (moveCloseToObject)
            {
                return FindNearestAccessiblePath(currentPoint, endPoint);
            }
            if (partialPath)
            {
                return FindPartialPath(currentPoint, endPoint);
            }

            return FindPath(currentPoint, endPoint);
        }

        if(controlPoints.Count == 0)
        {
            if (moveCloseToObject)
            {
                return FindNearestAccessiblePath(currentPoint, endPoint);
            }
            if (partialPath)
            {
                return FindPartialPath(currentPoint, endPoint);
            }

            return FindPath(currentPoint, endPoint);
        }

        if (!controlPoints.Contains(currentPoint))
        {
            controlPoints.Insert(0, currentPoint);
        }
        var hasObstacles = IsPathPassable(controlPoints);

        if (!hasObstacles)
        {
            return controlPoints;
        }

        if (moveCloseToObject)
        {
            return FindNearestAccessiblePath(currentPoint, endPoint);
        }
        if (partialPath)
        {
            return FindPartialPath(currentPoint, endPoint);
        }

        return FindPath(currentPoint, endPoint);
    }
    
    public bool IsObstacleBetweenPoints(Vector2 start, Vector2 end)
    {
        var direction = end - start;
        var distance = direction.magnitude;

        var hit = Physics2D.Raycast(start, direction.normalized, distance, _obstacleMask);

        return hit.collider != null;
    }

    public bool IsPathPassable(List<Vector2> path)
    {
        var hasObstacles = false;

        for (var i = 0; i < path.Count - 1; i++)
        {
            var current = path[i];
            var next = path[i + 1];

            var direction = next - current;
            var distance = direction.magnitude;

            var hit = Physics2D.Raycast(current, direction.normalized, distance, _obstacleMask);

            if (hit.collider != null)
            {
                hasObstacles = true;
                break;
            }
        }

        return hasObstacles;
    }

    public bool IsPositionOccupied(Vector2 position)
    {
        var colliders = Physics2D.OverlapCircleAll(position, 0.1f, _obstacleMask);
        return colliders.Length > 0;
    }

    
    public List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        if (Physics2D.OverlapPoint(start, _obstacleMask))
        {
            var obstacleCollider = Physics2D.OverlapPoint(start, _obstacleMask);
            if (obstacleCollider != null)
            {
                var bounds = obstacleCollider.bounds;

                var closestPoint = GetClosestPointOnEdge2D(start, bounds);

                start = closestPoint;
            }
            else
            {
                return null;
            }
        }

        if (Physics2D.OverlapPoint(end, _obstacleMask))
        {
            Debug.LogWarning("End point inside of a collision.");
            return null;
        }

        var path = new List<Vector2> { start };

        if (!Physics2D.Raycast(start, end - start, Vector2.Distance(start, end), _obstacleMask))
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

            if (!Physics2D.Raycast(current, end - current, Vector2.Distance(current, end), _obstacleMask))
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

                if (Physics2D.Raycast(current, neighbor - current, Vector2.Distance(current, neighbor), _obstacleMask))
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

    public List<Vector2> FindPartialPath(Vector2 start, Vector2 end, float maxIterations = 10000)
    {
        if (Physics2D.OverlapPoint(start, _obstacleMask))
        {
            Debug.LogWarning("Start point inside of a collision");
            return null;
        }

        var path = new List<Vector2> { start };
        if (!Physics2D.Raycast(start, end - start, Vector2.Distance(start, end), _obstacleMask))
        {
            path.Add(end);
            return path;
        }

        var visited = new HashSet<Vector2>();
        var cameFrom = new Dictionary<Vector2, Vector2>();
        var openSet = new List<NodeWithPriority>();
        var openSetContents = new HashSet<Vector2>();
        var gScore = new Dictionary<Vector2, float> { { start, 0 } };
        var fScore = new Dictionary<Vector2, float> { { start, Vector2.Distance(start, end) } };
        var bestEstimatedTotalLength = float.MaxValue;

        openSet.Add(new NodeWithPriority(start, 0, Vector2.Distance(start, end)));
        openSetContents.Add(start);

        var bestPosition = start;
        var bestDistance = Vector2.Distance(start, end);
        float iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            var current = openSet[0].Position;
            openSet.RemoveAt(0);
            openSetContents.Remove(current);

            var distanceToEnd = Vector2.Distance(current, end);
            if (!Physics2D.Raycast(current, end - current, distanceToEnd, _obstacleMask))
            {
                var finalPath = ReconstructPath(cameFrom, current);
                finalPath.Add(end);
                return finalPath;
            }

            var currentPathLength = gScore[current];
            var currentEstimatedTotalLength = currentPathLength + distanceToEnd;

            if (bestPosition == start && current != start)
            {
                bestPosition = current;
                bestDistance = distanceToEnd;
                bestEstimatedTotalLength = currentEstimatedTotalLength;
            }
            else if (distanceToEnd < bestDistance * 0.7f)
            {
                bestPosition = current;
                bestDistance = distanceToEnd;
                bestEstimatedTotalLength = currentEstimatedTotalLength;
            }
            else if (currentEstimatedTotalLength < bestEstimatedTotalLength * 0.9f)
            {
                bestPosition = current;
                bestDistance = distanceToEnd;
                bestEstimatedTotalLength = currentEstimatedTotalLength;
            }
            else if (Math.Abs(currentEstimatedTotalLength - bestEstimatedTotalLength) / bestEstimatedTotalLength < 0.1f &&
                     distanceToEnd < bestDistance * 0.9f)
            {
                bestPosition = current;
                bestDistance = distanceToEnd;
                bestEstimatedTotalLength = currentEstimatedTotalLength;
            }

            visited.Add(current);

            var neighbors = FindNeighborsAlongEdges(current, end, visited);

            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor))
                    continue;

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

        return ReconstructPath(cameFrom, bestPosition);
    }

    public List<Vector2> FindNearestAccessiblePath(Vector2 characterPosition, Vector2 targetPoint, int pointsPerEdge = 3)
    {
        if (!Physics2D.OverlapPoint(targetPoint, _obstacleMask))
        {
            return FindPath(characterPosition, targetPoint);
        }

        var targetCollider = Physics2D.OverlapPoint(targetPoint, _obstacleMask);
        if (targetCollider == null)
        {
            return FindPath(characterPosition, targetPoint);
        }

        var bounds = targetCollider.bounds;
        var minX = bounds.min.x - _avoidanceOffset;
        var maxX = bounds.max.x + _avoidanceOffset;
        var minY = bounds.min.y - _avoidanceOffset;
        var maxY = bounds.max.y + _avoidanceOffset;

        var edgePoints = new HashSet<Vector2>();
        for (var i = 0; i <= pointsPerEdge; i++)
        {
            var x = Mathf.Lerp(minX, maxX, i / (float)pointsPerEdge);
            edgePoints.Add(new Vector2(x, minY));
        }
        for (var i = 0; i <= pointsPerEdge; i++)
        {
            var x = Mathf.Lerp(minX, maxX, i / (float)pointsPerEdge);
            edgePoints.Add(new Vector2(x, maxY));
        }
        for (var i = 1; i < pointsPerEdge; i++)
        {
            var y = Mathf.Lerp(minY, maxY, i / (float)pointsPerEdge);
            edgePoints.Add(new Vector2(minX, y));
        }
        for (var i = 1; i < pointsPerEdge; i++)
        {
            var y = Mathf.Lerp(minY, maxY, i / (float)pointsPerEdge);
            edgePoints.Add(new Vector2(maxX, y));
        }

        var candidatePoints = new List<(Vector2 point, float score)>();

        foreach (var point in edgePoints)
        {
            if (!Physics2D.OverlapPoint(point, _obstacleMask) && GridService.IsWorldPositionInMapBounds(point))
            {
                var hasDirectLineOfSight = !Physics2D.Linecast(characterPosition, point, _obstacleMask);

                var distanceScore = Vector2.Distance(characterPosition, point);

                var distanceToTarget = Vector2.Distance(point, targetPoint);

                var obstacleScore = EstimatePathComplexity(characterPosition, point);

                var totalScore = distanceScore + obstacleScore * 3f + distanceToTarget * 0.5f;

                if (hasDirectLineOfSight)
                {
                    totalScore -= 50f;
                }

                candidatePoints.Add((point, totalScore));
            }
        }

        candidatePoints.Sort((a, b) => a.score.CompareTo(b.score));

        var maxPointsToCheck = Mathf.Min(3, candidatePoints.Count);

        for (var i = 0; i < maxPointsToCheck; i++)
        {
            if (i >= candidatePoints.Count)
                break;

            var point = candidatePoints[i].point;
            var path = FindPath(characterPosition, point);

            if (path != null && path.Count > 0)
            {
                return path;
            }
        }

        for (var i = maxPointsToCheck; i < candidatePoints.Count; i++)
        {
            var point = candidatePoints[i].point;
            var path = FindPath(characterPosition, point);

            if (path != null && path.Count > 0)
            {
                return path;
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

        var hitToGoal = Physics2D.Raycast(current, goal - current, Vector2.Distance(current, goal), _obstacleMask);

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

                var edgePoints = new HashSet<Vector2>
                {
                    new(currentBounds.min.x - _avoidanceOffset, currentBounds.min.y - _avoidanceOffset),
                    new(currentBounds.max.x + _avoidanceOffset, currentBounds.min.y - _avoidanceOffset),
                    new(currentBounds.min.x - _avoidanceOffset, currentBounds.max.y + _avoidanceOffset),
                    new(currentBounds.max.x + _avoidanceOffset, currentBounds.max.y + _avoidanceOffset)
                };

                var numPointsPerEdge = 3;

                // Bottom edge
                for (var i = 1; i <= numPointsPerEdge; i++)
                {
                    var x = Mathf.Lerp(currentBounds.min.x - _avoidanceOffset, currentBounds.max.x + _avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(x, currentBounds.min.y - _avoidanceOffset));
                }

                // Top edge
                for (var i = 1; i <= numPointsPerEdge; i++)
                {
                    var x = Mathf.Lerp(currentBounds.min.x - _avoidanceOffset, currentBounds.max.x + _avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(x, currentBounds.max.y + _avoidanceOffset));
                }

                // Left edge
                for (var i = 1; i <= numPointsPerEdge; i++)
                {
                    var y = Mathf.Lerp(currentBounds.min.y - _avoidanceOffset, currentBounds.max.y + _avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(currentBounds.min.x - _avoidanceOffset, y));
                }

                // Right edge
                for (var i = 1; i <= numPointsPerEdge; i++)
                {
                    var y = Mathf.Lerp(currentBounds.min.y - _avoidanceOffset, currentBounds.max.y + _avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(currentBounds.max.x + _avoidanceOffset, y));
                }

                foreach (var point in edgePoints)
                {
                    var nearbyColliders = Physics2D.OverlapCircleAll(point, _avoidanceOffset, _obstacleMask);

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
                        !Physics2D.Raycast(current, point - current, Vector2.Distance(current, point), _obstacleMask))
                    {
                        neighbors.Add(point);
                    }
                }
            }
        }

        var validNeighbors = neighbors.Where(GridService.IsWorldPositionInMapBounds).ToList();

        return validNeighbors.ToList();
    }

    private static float CalculatePathLength(List<Vector2> path)
    {
        var length = 0f;

        for (var i = 0; i < path.Count - 1; i++)
        {
            length += Vector2.Distance(path[i], path[i + 1]);
        }

        return length;
    }

    private static float EstimatePathComplexity(Vector2 start, Vector2 end)
    {
        var complexity = 0f;
        var direction = end - start;
        var distance = direction.magnitude;
        direction.Normalize();

        var checkPoints = Mathf.CeilToInt(distance / 0.5f);

        var previousWasObstacle = false;
        var obstacleCount = 0;

        for (var i = 1; i < checkPoints; i++)
        {
            var checkPoint = start + direction * (i * 0.5f);
            bool isObstacle = Physics2D.OverlapPoint(checkPoint, _obstacleMask);

            if (isObstacle)
            {
                obstacleCount++;

                if (!previousWasObstacle)
                {
                    complexity += 10f;
                }

                complexity += 2f;
            }

            previousWasObstacle = isObstacle;
        }

        if (obstacleCount > checkPoints * 0.3f)
        {
            complexity += 30f;
        }

        return complexity;
    }

    private static Vector2 GetClosestPointOnEdge2D(Vector2 unitPosition, Bounds buildingBounds)
    {
        Vector2 center = buildingBounds.center;
        Vector2 halfSize = buildingBounds.extents;
        var keyPoints = new Vector2[8];
        keyPoints[0] = new Vector2(center.x - halfSize.x, center.y - halfSize.y);
        keyPoints[1] = new Vector2(center.x + halfSize.x, center.y - halfSize.y);
        keyPoints[2] = new Vector2(center.x - halfSize.x, center.y + halfSize.y);
        keyPoints[3] = new Vector2(center.x + halfSize.x, center.y + halfSize.y);
        keyPoints[4] = new Vector2(center.x, center.y - halfSize.y);
        keyPoints[5] = new Vector2(center.x, center.y + halfSize.y);
        keyPoints[6] = new Vector2(center.x - halfSize.x, center.y);
        keyPoints[7] = new Vector2(center.x + halfSize.x, center.y);

        var closestPoint = center;
        var minDistance = float.MaxValue;
        foreach (var point in keyPoints)
        {
            if (GridService.IsWorldPositionInMapBounds(point))
            {
                var distance = Vector2.Distance(unitPosition, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = point;
                }
            }
        }

        var directionFromCenter = (closestPoint - center).normalized;
        var offsetPoint = closestPoint + directionFromCenter * 0.2f;

        var collision = Physics2D.OverlapCircle(offsetPoint, 0.1f, LayerMask.GetMask("Objects"));
        if (collision == null)
        {
            return offsetPoint;
        }
        else
        {
            return closestPoint;
        }
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
