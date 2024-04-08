using Animancer;
using UnityEngine;

namespace Disc0ver.FSM
{
    public class MoveState: BaseState
    {
        public override StateType StateType => StateType.Move;
        
        private float _jumpTime = 0;
        private bool _jumpDown = false;
        private Vector3 _lastInputVec = Vector3.zero;

        private AnimancerState _state;
        
        public override void OnEnterState(StateMachine stateMachine)
        {
            _jumpTime = 0;
            _jumpDown = false;
            stateMachine.RunMixerState.Initialize(stateMachine.controller.dccAnimClips.runTurnLeft, stateMachine.controller.dccAnimClips.run, stateMachine.controller.dccAnimClips.runTurnRight);
            _state = stateMachine.animancerComponent.Play(stateMachine.RunMixerState, 0.1f);
            stateMachine.animancerComponent.Animator.applyRootMotion = true;
            _lastInputVec = stateMachine.controller.inputVec;
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

            _lastInputVec = stateMachine.controller.inputVec;
        }

        public override void UpdateAnimation(StateMachine stateMachine)
        {
            if (stateMachine.controller.MovementComponent.Velocity.magnitude < 1e-4 && stateMachine.controller.inputVec.magnitude < 1e-4)
            {
                stateMachine.ChangeToState(StateType.Idle);
            }

            var angle = Vector3.Angle(stateMachine.controller.inputVec,
                stateMachine.controller.MovementComponent.Velocity);

            var flag = 1;
            if (Vector3.Cross(stateMachine.controller.MovementComponent.Velocity, stateMachine.controller.inputVec).y <
                0)
                flag = -1;
            
            if (angle <= 90)
            {
                stateMachine.RunMixerState.Parameter = flag * angle / 90;
            }
            else
            {
                IdleState.GetRotateAnimationClip(stateMachine, ref stateMachine.RunStartMixerState);
                var state = stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.runStopL, 0.5f);
                if (stateMachine.controller.inputVec.magnitude > 1e-4)
                {
                    state.Time = 3;
                    state.Events.OnEnd =
                        () =>
                        {
                            var state1 = stateMachine.animancerComponent.Play(stateMachine.RunStartMixerState, 0.1f);
                            state1.Events.OnEnd = () => { stateMachine.animancerComponent.Play(stateMachine.RunMixerState); };
                        };
                }
            }
        }


        public override void OnExistState(StateMachine stateMachine)
        {
            // stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.run, 0.25f);
        }
    }
}