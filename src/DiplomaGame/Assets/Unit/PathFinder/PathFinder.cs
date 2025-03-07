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
    /// <returns></returns>
    public List<Vector2> RefindPath(List<Vector2> controlPoints, Vector2 currentPoint, Vector2 endPoint)
    {
        if (controlPoints == null || controlPoints.Count < 2)
        {
            return FindPath(currentPoint, endPoint);
        }

        var hasObstacles = false;

        for (var i = 0; i < controlPoints.Count - 1; i++)
        {
            var current = controlPoints[i];
            var next = controlPoints[i + 1];
            
            var direction = next - current;
            var distance = direction.magnitude;

            var hit = Physics2D.Raycast(current, direction.normalized, distance, _obstacleMask);

            if (hit.collider != null)
            {
                hasObstacles = true;
                break;
            }
        }

        if (!hasObstacles)
        {
            return controlPoints;
        }

        return FindPath(currentPoint, endPoint);
    }


    public List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        if (Physics2D.OverlapPoint(start, _obstacleMask))
        {
            Debug.LogWarning("Start point inside of a collision");
            return null;
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

    public Vector2? FindNearestAccessiblePoint(Vector2 characterPosition, Vector2 targetPoint, float maxDistance = 2.0f, int pointsPerEdge = 5)
    {
        // Перевіряємо, чи цільова точка вже доступна
        if (!Physics2D.OverlapPoint(targetPoint, _obstacleMask))
        {
            return targetPoint;
        }

        // Отримуємо колайдер, на який вказав користувач
        var targetCollider = Physics2D.OverlapPoint(targetPoint, _obstacleMask);
        if (targetCollider == null)
        {
            return null;
        }

        // Створюємо список потенційних точок
        var candidatePoints = new List<CandidatePoint>();

        // Отримуємо межі колайдера
        var bounds = targetCollider.bounds;

        // Сгенеруємо точки навколо колайдера з поступовим відступом
        for (var offset = _avoidanceOffset; offset <= maxDistance; offset += maxDistance / 3)
        {
            // Межі колайдера з відступом
            var minX = bounds.min.x - offset;
            var maxX = bounds.max.x + offset;
            var minY = bounds.min.y - offset;
            var maxY = bounds.max.y + offset;

            // Список всіх точок для цього рівня відступу
            var edgePoints = new List<Vector2>();

            // Нижня сторона
            for (var i = 0; i <= pointsPerEdge; i++)
            {
                var x = Mathf.Lerp(minX, maxX, i / (float)pointsPerEdge);
                edgePoints.Add(new Vector2(x, minY));
            }

            // Верхня сторона
            for (var i = 0; i <= pointsPerEdge; i++)
            {
                var x = Mathf.Lerp(minX, maxX, i / (float)pointsPerEdge);
                edgePoints.Add(new Vector2(x, maxY));
            }

            // Ліва сторона
            for (var i = 1; i < pointsPerEdge; i++)
            {
                var y = Mathf.Lerp(minY, maxY, i / (float)pointsPerEdge);
                edgePoints.Add(new Vector2(minX, y));
            }

            // Права сторона
            for (var i = 1; i < pointsPerEdge; i++)
            {
                var y = Mathf.Lerp(minY, maxY, i / (float)pointsPerEdge);
                edgePoints.Add(new Vector2(maxX, y));
            }

            // Перевіряємо кожну точку
            foreach (var point in edgePoints)
            {
                // Перевіряємо, чи точка доступна (не в колізії)
                if (!Physics2D.OverlapPoint(point, _obstacleMask) && GridService.IsWorldPositionInMapBounds(point))
                {
                    // Перевіряємо, чи є прямий шлях від персонажа до точки
                    if (!Physics2D.Raycast(characterPosition, point - characterPosition, Vector2.Distance(characterPosition, point), _obstacleMask))
                    {
                        // Додаємо точку з пріоритетом на основі відстаней
                        var distToTarget = Vector2.Distance(point, targetPoint);
                        var distToChar = Vector2.Distance(point, characterPosition);

                        // Комбінована оцінка (менше краще)
                        var score = distToTarget * 0.7f + distToChar * 0.3f;

                        candidatePoints.Add(new CandidatePoint(point, score));
                    }
                }
            }

            // Якщо на цьому рівні відступу знайдено достатньо точок, можемо зупинити пошук
            if (candidatePoints.Count >= pointsPerEdge)
            {
                break;
            }
        }

        // Якщо не знайдено жодної точки, повертаємо null
        if (candidatePoints.Count == 0)
        {
            return null;
        }

        // Сортуємо точки за оцінкою і повертаємо найкращу
        candidatePoints.Sort((a, b) => a.Score.CompareTo(b.Score));
        return candidatePoints[0].Position;
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

                var edgePoints = new List<Vector2>
            {
                new(currentBounds.min.x - _avoidanceOffset, currentBounds.min.y - _avoidanceOffset),
                new(currentBounds.max.x + _avoidanceOffset, currentBounds.min.y - _avoidanceOffset),
                new(currentBounds.min.x - _avoidanceOffset, currentBounds.max.y + _avoidanceOffset),
                new(currentBounds.max.x + _avoidanceOffset, currentBounds.max.y + _avoidanceOffset)
            };

                var numPointsPerEdge = 3;

                // Bottom edge
                for (var i = 1; i < numPointsPerEdge; i++)
                {
                    var x = Mathf.Lerp(currentBounds.min.x - _avoidanceOffset, currentBounds.max.x + _avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(x, currentBounds.min.y - _avoidanceOffset));
                }

                // Top edge
                for (var i = 1; i < numPointsPerEdge; i++)
                {
                    var x = Mathf.Lerp(currentBounds.min.x - _avoidanceOffset, currentBounds.max.x + _avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(x, currentBounds.max.y + _avoidanceOffset));
                }

                // Left edge
                for (var i = 1; i < numPointsPerEdge; i++)
                {
                    var y = Mathf.Lerp(currentBounds.min.y - _avoidanceOffset, currentBounds.max.y + _avoidanceOffset, i / (float)numPointsPerEdge);
                    edgePoints.Add(new Vector2(currentBounds.min.x - _avoidanceOffset, y));
                }

                // Right edge
                for (var i = 1; i < numPointsPerEdge; i++)
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

    private class CandidatePoint
    {
        public Vector2 Position;
        public float Score;

        public CandidatePoint(Vector2 position, float score)
        {
            Position = position;
            Score = score;
        }
    }
}
