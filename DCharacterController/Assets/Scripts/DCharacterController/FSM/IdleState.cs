using UnityEngine;

namespace Disc0ver.FSM
{
    public class IdleState : BaseState
    {
        public override StateType StateType => StateType.Idle;

        private float _jumpTime = 0;
        private bool _jumpDown = false;
        private float _moveTime = 0;
        
        public override void OnEnterState(StateMachine stateMachine)
        {
            _jumpTime = 0;
            _moveTime = 0;
            _jumpDown = false;
            stateMachine.controller.MovementComponent.SetMovementMode(MoveMode.MoveNone);
        }

        public override void HandleInput(StateMachine stateMachine)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpTime = 0f;
                _jumpDown = true;
            }

            if (_jumpDown)
            {
                _jumpTime += Time.deltaTime;
                if (_jumpTime >= stateMachine.controller.maxJumpDownTime || Input.GetKeyUp(KeyCode.Space))
                {
                    stateMachine.controller.DoJump(_jumpTime);
                    _jumpDown = false;
                }
                return;
            }

            if (stateMachine.controller.inputVec.magnitude > 1e-4f)
            {
                _moveTime += Time.deltaTime;
                if (_moveTime > 0.2f)
                {
                    stateMachine.ChangeToState(StateType.Move);
                }
            }
            else
            {
                _moveTime = 0f;
            }
        }

        public override void OnExistState(StateMachine stateMachine)
        {
            
        }
    }
}