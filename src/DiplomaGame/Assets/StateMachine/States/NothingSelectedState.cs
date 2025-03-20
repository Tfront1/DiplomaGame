using UnityEngine;

namespace StateMachine.States
{
    public class NothingSelectedState : BaseState
    {
        public NothingSelectedState(InputStateMachine stateMachine) : base(stateMachine)
        {

        }

        public override void Enter()
        {
            Debug.Log("Entering NothingSelected state");
        }

        public override void Exit()
        {
            Debug.Log("Exiting NothingSelected state");
        }
    }
}