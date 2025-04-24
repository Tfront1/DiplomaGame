using Town;
using UnityEngine;

namespace Bots
{
    public class Bot : MonoBehaviour
    {
        public TownItem Town { get; set; }
        public BotBrain BotBrain { get; set; }

        private object _lockObject = new();

        public void Initialize(TownItem town)
        {
            Town = town;

            BotBrain = new BotBrain(this);
        }

        public void StartBot()
        {
            BotTickRateSystem.Instance.OnTick += Think;
            UnitRegistry.OnUnitChangedCell += BotBrain.OnUnitChangedCellHandler;
        }

        public void StopBot()
        {
            BotTickRateSystem.Instance.OnTick -= Think;
            UnitRegistry.OnUnitChangedCell -= BotBrain.OnUnitChangedCellHandler;
        }

        private void Think(float _)
        {
            BotBrain.Think();
        }

        private void OnDestroy()
        {
            TickRateSystem.Instance.OnTick -= Think;
        }
    }
}
