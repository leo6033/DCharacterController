using UnityEngine;

namespace Disc0ver.FSM
{
    public class MoveState: BaseState
    {
        public override StateType StateType => StateType.Move;
        
        private float _jumpTime = 0;
        private bool _jumpDown = false;
        
        public override void OnEnterState(StateMachine stateMachine)
        {
            _jumpTime = 0;
            _jumpDown = false;
            stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.run, 0.25f);
            stateMachine.animancerComponent.Animator.applyRootMotion = false;
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
            }

            if (stateMachine.controller.MovementComponent.Velocity.magnitude < 1e-4 && stateMachine.controller.inputVec.magnitude < 1e-4)
            {
                stateMachine.ChangeToState(StateType.Idle);
            }
        }

        public override void OnExistState(StateMachine stateMachine)
        {
            // stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.run, 0.25f);
        }
    }
}