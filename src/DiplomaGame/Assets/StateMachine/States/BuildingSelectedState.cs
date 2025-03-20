using UnityEngine;

namespace StateMachine.States
{
    public class BuildingSelectedState : BaseState
    {
        public BuildingSelectedState(InputStateMachine stateMachine): base(stateMachine)
        {

        }

        public override void Enter()
        {
            Debug.Log("Entering BuildingSelected state");
        }

        public override void Exit()
        {
            Debug.Log("Exiting BuildingSelected state");
        }
    }
}