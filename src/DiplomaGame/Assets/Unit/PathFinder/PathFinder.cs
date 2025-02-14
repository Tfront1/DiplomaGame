using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameUtilities.Utils;
using UnityEngine;

namespace Unit.PathFinder
{
    public static class PathFinder
    {
        private static readonly Vector2[] Directions = {
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0),
        new(1, 1),
        new(-1, 1),
        new(1, -1),
        new(-1, -1)
        };

        public static async Task<List<Vector2>> FindPathAsync(Vector2 worldStart, Vector2 worldEnd)
        {
            return await Task.Run(() =>
            {
                var gridStart = new Vector2(
                    (float)Math.Floor((worldStart.x - MapConfig.MapStartPointX) / MapConfig.CellSize),
                    (float)Math.Floor((worldStart.y - MapConfig.MapStartPointY) / MapConfig.CellSize)
                );

                var originalGridEnd = new Vector2(
                    (float)Math.Floor((worldEnd.x - MapConfig.MapStartPointX) / MapConfig.CellSize),
                    (float)Math.Floor((worldEnd.y - MapConfig.MapStartPointY) / MapConfig.CellSize)
                );

                var (isEndObstacle, _) = CheckObstacle((int)originalGridEnd.x, (int)originalGridEnd.y);
                var gridEnd = originalGridEnd;

                if (isEndObstacle)
                {
                    var nearestPoint = FindNearestAccessiblePoint(originalGridEnd);
                    if (nearestPoint == null)
                    {
                        return new List<Vector2>();
                    }
                    gridEnd = nearestPoint.Value;
                }

                var startNode = new Node(gridStart, true, false);
                var endNode = new Node(gridEnd, true, false);
                var openSet = new List<Node> { startNode };
                var closedSet = new HashSet<Vector2>();
                var nodeGrid = new Dictionary<Vector2, Node>();

                startNode.GCost = 0;
                startNode.HCost = CalculateHCost(startNode.GridPosition, endNode.GridPosition);

                while (openSet.Count > 0)
                {
                    var currentNode = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();

                    if (currentNode.GridPosition == endNode.GridPosition)
                    {
                        var path = RetracePath(startNode, currentNode);

                        return path;
                    }

                    openSet.Remove(currentNode);
                    closedSet.Add(currentNode.GridPosition);

                    foreach (var direction in Directions)
                    {
                        var neighborPos = currentNode.GridPosition + direction;
                        if (closedSet.Contains(neighborPos))
                            continue;

                        var (isObstacle, hasMargin) = CheckObstacle((int)neighborPos.x, (int)neighborPos.y);
                        if (isObstacle)
                            continue;

                        if (direction.x != 0 && direction.y != 0)
                        {
                            var (horizontalObstacle, _) = CheckObstacle((int)currentNode.GridPosition.x + (int)direction.x, (int)currentNode.GridPosition.y);
                            var (verticalObstacle, _) = CheckObstacle((int)currentNode.GridPosition.x, (int)currentNode.GridPosition.y + (int)direction.y);
                            if (horizontalObstacle || verticalObstacle)
                                continue;
                        }

                        if (hasMargin)
                        {
                            var hasAdjacentMargin = false;
                            for (var dx = -1; dx <= 1 && !hasAdjacentMargin; dx++)
                            {
                                for (var dy = -1; dy <= 1 && !hasAdjacentMargin; dy++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    var (_, adjMargin) = CheckObstacle((int)neighborPos.x + dx, (int)neighborPos.y + dy);
                                    if (adjMargin)
                                    {
                                        hasAdjacentMargin = true;
                                        break;
                                    }
                                }
                            }
                            if (hasAdjacentMargin)
                                continue;
                        }

                        if (!nodeGrid.TryGetValue(neighborPos, out var neighbor))
                        {
                            neighbor = new Node(neighborPos, !isObstacle, hasMargin);
                            nodeGrid[neighborPos] = neighbor;
                        }

                        var newGCost = currentNode.GCost + UtilsClass.CalculateDistance(currentNode.GridPosition, neighborPos);
                        if (newGCost < neighbor.GCost)
                        {
                            neighbor.Parent = currentNode;
                            neighbor.GCost = newGCost;
                            neighbor.HCost = CalculateHCost(neighbor.GridPosition, endNode.GridPosition);
                            if (!openSet.Contains(neighbor))
                            {
                                openSet.Add(neighbor);
                            }
                        }
                    }
                }
                return new List<Vector2>();
            });
        }

        private static float CalculateHCost(Vector2 start, Vector2 end)
        {
            return Math.Abs(start.x - end.x) + Math.Abs(start.y - end.y);
        }

        private static List<Vector2> RetracePath(Node startNode, Node endNode)
        {
            var path = new List<Vector2>();
            var currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(new Vector2(
                    currentNode.GridPosition.x * MapConfig.CellSize + MapConfig.CellSize / 2,
                    currentNode.GridPosition.y * MapConfig.CellSize + MapConfig.CellSize / 2
                ));
                currentNode = currentNode.Parent;
            }

            path.Add(new Vector2(
                startNode.GridPosition.x * MapConfig.CellSize + MapConfig.CellSize / 2,
                startNode.GridPosition.y * MapConfig.CellSize + MapConfig.CellSize / 2
            ));

            path.Reverse();
            return path;
        }

        private static (bool isObstacle, bool hasMargin) CheckObstacle(int gridX, int gridY)
        {
            if (gridX < 0 || gridX >= MapConfig.MapWidth || gridY < 0 || gridY >= MapConfig.MapHeight)
                return (true, false);
            if (!GridService.IsEmptyPosition(gridX, gridY, GridRegistry.GetAllGridsList().ToArray()))
            {
                var obj = GridService.GetObjectAtPosition(gridX, gridY, GridRegistry.GetAllGridsList().ToArray());
                if (obj is BuildingGridObject buildingGrid)
                {
                    var objBuilding = ItemListService.GetObjectByGuid(buildingGrid.Guid, ItemListRegistry.GetAllListsItemsList().ToArray());
                    if (objBuilding is Building building)
                    {
                        return (true, building.Margin);
                    }
                    return (true, false);
                }
                if (obj is SupplyGridObject)
                {
                    return (true, true);
                }
            }
            return (false, false);
        }

        private static Vector2? FindNearestAccessiblePoint(Vector2 targetPos)
        {
            var minDistance = float.MaxValue;
            Vector2? nearestPoint = null;

            for (var dx = 0; dx <= MapConfig.MapWidth; dx++)
            {
                for (var dy = 0; dy <= MapConfig.MapHeight; dy++)
                {
                    var checkPos = new Vector2(targetPos.x + dx, targetPos.y + dy);

                    if (checkPos.x < 0 || checkPos.x >= MapConfig.MapWidth ||
                        checkPos.y < 0 || checkPos.y >= MapConfig.MapHeight)
                        continue;

                    var (isObstacle, _) = CheckObstacle((int)checkPos.x, (int)checkPos.y);
                    if (!isObstacle)
                    {
                        var distance = Vector2.Distance(targetPos, checkPos);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestPoint = checkPos;
                        }
                    }
                }
            }

            return nearestPoint;
        }
    }
}