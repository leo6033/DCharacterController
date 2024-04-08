using Animancer;
using UnityEngine;

namespace Disc0ver.FSM
{
    public class IdleState : BaseState
    {
        public override StateType StateType => StateType.Idle;

        private float _jumpTime = 0;
        private bool _jumpDown = false;
        private float _moveTime = 0;
        private float _rotateTime = 0f;

        private Quaternion _startQuaternion;
        private LinearMixerState _rotateMixer = new LinearMixerState();
        
        public override void OnEnterState(StateMachine stateMachine)
        {
            _jumpTime = 0;
            _moveTime = 0;
            _rotateTime = 0;
            _jumpDown = false;
            stateMachine.controller.MovementComponent.SetMovementMode(MoveMode.MoveNone);
            stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.idle, 0.25f);
            stateMachine.animancerComponent.Animator.applyRootMotion = true;
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

            if (stateMachine.controller.inputVec.magnitude > 0.5f)
            {
                if (_moveTime == 0)
                {
                    _startQuaternion = stateMachine.controller.transform.rotation;
                    GetRotateAnimationClip(stateMachine);
                    var state = stateMachine.animancerComponent.Play(_rotateMixer);
                    _rotateTime = state.Length;
                    stateMachine.controller.StartMove();
                    Debug.Log($"[Idle] rotate {_rotateTime}");
                }
                _moveTime += Time.deltaTime;
                if (_moveTime > _rotateTime)
                {
                    Debug.Log($"[Idle] enter Move, rotate time: {_moveTime}");
                    stateMachine.ChangeToState(StateType.Move);
                }
                
            }
            else
            {
                _moveTime = 0f;
                stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.idle, 0.25f);
            }
        }

        private LinearMixerState GetRotateAnimationClip(StateMachine stateMachine)
        {
            var angle = Vector3.Angle(stateMachine.controller.inputVec, stateMachine.controller.transform.forward);
            var percent = 1f;
            if (angle > 180)
                angle -= 360;

            if (Vector3.Cross(stateMachine.controller.transform.forward, stateMachine.controller.inputVec).y < 0)
                angle = -angle;
            
            if (angle is >= -180 and < -135)
            {
                percent = (-135 - angle) / 45;
                _rotateMixer.Initialize(stateMachine.controller.dccAnimClips.runStartTurnLeft135,
                    stateMachine.controller.dccAnimClips.runStartTurnLeft180);
            }
            else if (angle is >= -135 and < -90 )
            {
                percent = (-90 - angle) / 45;
                _rotateMixer.Initialize(stateMachine.controller.dccAnimClips.runStartTurnLeft90,
                    stateMachine.controller.dccAnimClips.runStartTurnLeft135);
            }
            else if (angle is >= -90 and < 0)
            {
                percent = - angle / 90;
                _rotateMixer.Initialize(stateMachine.controller.dccAnimClips.runStart,
                    stateMachine.controller.dccAnimClips.runStartTurnLeft90);
            }
            else if(angle is >=0 and < 90)
            {
                percent = angle / 90;
                _rotateMixer.Initialize(stateMachine.controller.dccAnimClips.runStart,
                    stateMachine.controller.dccAnimClips.runStartTurnRight90);
            }
            else if(angle is >= 90 and < 135)
            {
                percent = (angle - 90) / 45;
                _rotateMixer.Initialize(stateMachine.controller.dccAnimClips.runStartTurnRight90,
                    stateMachine.controller.dccAnimClips.runStartTurnRight135);
            }
            else if(angle is >= 135 and <= 180)
            {
                percent = (angle - 135) / 45;
                _rotateMixer.Initialize(stateMachine.controller.dccAnimClips.runStartTurnRight135,
                    stateMachine.controller.dccAnimClips.runStartTurnRight180);
            }
            _rotateMixer.Parameter = percent;
            Debug.Log($"[Idle] GetRotateAnimationClip percent: {percent}");
            
            return _rotateMixer;
        }

        public override void OnExistState(StateMachine stateMachine)
        {
            
        }
    }
}