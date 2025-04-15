using Game;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameUtilities.Utils;

namespace Town
{
    public class TownSpawner
    {
        private static int _maxAttempts = 100;

        public static List<TownItem> SpawnTowns(int townCount, float townSafeDistance, string userTownName)
        {
            var towns = new List<TownItem>();
            var townHall = BuildingsConfig.Buildings.Find(x => x.BuildingType == Building.BuildingTypes.TownHall);
            var townSize = new Vector2Int(townHall.WidthCell, townHall.HeightCell);

            var createdTownForUser = false;

            for (var i = 0; i < townCount; i++)
            {
                var isValidPosition = false;
                var attempts = 0;

                while (!isValidPosition && attempts < _maxAttempts)
                {
                    var coords = GameRandom.GetRandomCoords();

                    if (GridService.CanPlaceAtPosition(coords, townSize, GridRegistry.GetAllGridsList().ToArray()))
                    {
                        isValidPosition = true;

                        foreach (var town in towns)
                        {
                            var distance = Vector2.Distance(coords, town.TownHall.Coords);
                            if (distance < townSafeDistance)
                            {
                                isValidPosition = false;
                                break;
                            }
                        }

                        if (isValidPosition)
                        {
                            TownItem town;
                            if(!createdTownForUser)
                            {
                                town = new TownItem(userTownName, System.Guid.NewGuid(), true);
                                BuildingManager.BuildInstantly(coords, townHall, town);
                                createdTownForUser = true;
                            }
                            else
                            {
                                town = new TownItem(UtilsClass.GetRandomCityName(), System.Guid.NewGuid(), false);
                                BuildingManager.BuildInstantly(coords, townHall, town);
                            }

                            towns.Add(town);
                        }
                    }

                    attempts++;
                }

                if (attempts >= _maxAttempts)
                {
                    Debug.LogWarning($"Can`t found place for town #{i} after {_maxAttempts} times.");
                }
            }

            return towns;
        }
    }
}
