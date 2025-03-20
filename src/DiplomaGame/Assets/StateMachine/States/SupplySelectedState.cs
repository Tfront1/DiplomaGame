using UnityEngine;

namespace StateMachine.States
{
    public class SupplySelectedState : BaseState
    {
        public SupplySelectedState(InputStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log("Entering SupplySelected state");
        }

        public override void Exit()
        {
            Debug.Log("Exiting SupplySelected state");
        }
    }
}