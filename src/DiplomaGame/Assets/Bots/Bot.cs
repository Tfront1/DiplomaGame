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
            BotBrainManager.Instance.RegisterBot(BotBrain);
        }

        public void StopBot()
        {
            BotBrainManager.Instance.UnregisterBot(BotBrain);
        }

        private void OnDestroy()
        {
            BotBrainManager.Instance.UnregisterBot(BotBrain);
        }
    }
}
