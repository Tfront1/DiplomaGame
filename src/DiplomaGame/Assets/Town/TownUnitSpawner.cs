using System.Collections;
using UnityEngine;

namespace Town
{
    public class TownUnitSpawner : MonoBehaviour
    {
        private TownItem _town;

        private float _minSpawnInterval = 45f; // Sec
        private float _maxSpawnInterval = 75f; // Sec
        private float _spawnInterval = 60f; // Sec
        private float _spawnChance = 0.3f;

        public bool ToSpawn { get; set; } = true;

        private WaitForSeconds _waitInterval;

        public void Initialize(TownItem town)
        {
            _town = town;
            _waitInterval = new WaitForSeconds(_spawnInterval);

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (ToSpawn)
            {
                yield return _waitInterval;

                TrySpawnUnit();

                _spawnInterval = Random.Range(_minSpawnInterval, _maxSpawnInterval);
                _waitInterval = new WaitForSeconds(_spawnInterval);
            }
        }

        private void TrySpawnUnit()
        {
            if (_town.UnitsCount < _town.MaxUnits)
            {
                var freeSpacePercentage = 1f - ((float)_town.UnitsCount / _town.MaxUnits);

                var adjustedSpawnChance = _spawnChance + (freeSpacePercentage * 0.5f);

                adjustedSpawnChance = Mathf.Clamp(adjustedSpawnChance, _spawnChance, 0.9f);

                if (Random.value <= adjustedSpawnChance)
                {
                    SpawnUnit();
                }
            }
        }

        private void SpawnUnit()
        {
            if (_town != null && _town.TownHall != null)
            {
                UnitManager.CreateUnit(GridService.GetWorldPosition(_town.TownHall.CenterCoords), 3, _town);
                BuildingManager.TeleportUnitsToEdgeOfTheBuilding(_town.TownHall);
            }
        }

        public void SpawnStartUnits(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (_town != null && _town.TownHall != null)
                {
                    UnitManager.CreateUnit(GridService.GetWorldPosition(_town.TownHall.CenterCoords), 3, _town);
                    BuildingManager.TeleportUnitsToEdgeOfTheBuilding(_town.TownHall);
                }
            }
        }
    }
}
