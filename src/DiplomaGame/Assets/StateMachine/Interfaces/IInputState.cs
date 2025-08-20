using UnityEngine;

namespace StateMachine.Interfaces
{
    public interface IInputState
    {
        void Enter();
        void Exit();
        void HandleLeftClick(Vector2 position);
        void HandleRightClick(Vector2 position);
        void HandleLeftHoldStart(Vector2 position);
        void HandleLeftHold(Vector2 position);
        void HandleLeftHoldEnd(Vector2 position);
    }
}