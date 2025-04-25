using System;
using System.Collections;
using System.Threading.Tasks;
using Town;
using UnityEngine;

namespace Bots
{
    public class Bot : MonoBehaviour
    {
        public TownItem Town { get; set; }
        public BotBrain BotBrain { get; set; }

        public void Initialize(TownItem town)
        {
            Town = town;

            BotBrain = new BotBrain(this);
        }

        public void StartBot()
        {
            if (!BotBrain.IsSortedSupplies)
            {
                BotBrain.SortNearestSupplies();
            }

            BotTickRateSystem.Instance.OnTick += Think;
            UnitRegistry.OnUnitChangedCell += BotBrain.OnUnitChangedCellHandler;
        }

        public void StopBot()
        {
            BotTickRateSystem.Instance.OnTick -= Think;
            UnitRegistry.OnUnitChangedCell -= BotBrain.OnUnitChangedCellHandler;
        }
        
        private void Think(Task task)
        {
            CoroutineRunner.Instance.StartCoroutineWithId(Guid.NewGuid(), ToThink());
        }

        IEnumerator ToThink()
        {
            var randomDelay = UnityEngine.Random.Range(0f, 5f);

            yield return new WaitForSeconds(randomDelay);

            BotBrain.Think();
        }

        private void OnDestroy()
        {
            BotTickRateSystem.Instance.OnTick -= Think;
        }
    }
}
