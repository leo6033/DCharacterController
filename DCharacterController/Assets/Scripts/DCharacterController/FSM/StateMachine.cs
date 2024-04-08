using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace Disc0ver.FSM
{
    public enum StateType
    {
        Idle, Move, Jump
    }
    
    [Serializable]
    public abstract class BaseState
    {
        public abstract StateType StateType { get; }
        public abstract void OnEnterState(StateMachine stateMachine);
        public abstract void HandleInput(StateMachine stateMachine);
        public virtual void UpdateAnimation(StateMachine stateMachine) {}
        public abstract void OnExistState(StateMachine stateMachine);

        public void Update(StateMachine stateMachine, float deltaTime)
        {
            HandleInput(stateMachine);
            UpdateAnimation(stateMachine);
            // OnUpdate(stateMachine, deltaTime);
        }

        // protected virtual void OnUpdate(StateMachine stateMachine, float deltaTime)
        // {
        //     
        // }

        public virtual bool CanExist(StateMachine stateMachine)
        {
            return true;
        }

        public virtual bool CanEnter(StateMachine stateMachine)
        {
            return true;
        }
    }
    
    [Serializable]
    public class StateMachine
    {
        public Dictionary<StateType, BaseState> states;
        public CharacterController controller;

        public BaseState currentState;
        public AnimancerComponent animancerComponent;

        public LinearMixerState RunStartMixerState = new LinearMixerState();
        public LinearMixerState RunMixerState = new LinearMixerState();

        public StateMachine(CharacterController controller)
        {
            states = new Dictionary<StateType, BaseState>()
            {
                { StateType.Idle , new IdleState()},
                { StateType.Jump , new JumpState()},
                { StateType.Move , new MoveState()}
            };

            this.controller = controller;
            currentState = states[StateType.Idle];
            animancerComponent = controller.modelGo.GetComponent<AnimancerComponent>();
            currentState.OnEnterState(this);
            
        }
        
        public void Update(float deltaTime)
        {
            currentState.Update(this, deltaTime);
            if (!animancerComponent.Animator.applyRootMotion)
            {
                controller.modelGo.transform.rotation = Quaternion.RotateTowards(controller.modelGo.transform.rotation, controller.transform.rotation, 10);
                // controller.modelGo.transform.SetPositionAndRotation(controller.transform.position, controller.transform.rotation);
            }
            controller.modelGo.transform.position = controller.transform.position;
        }

        public void ChangeToState(StateType stateType)
        {
            if (!states.ContainsKey(stateType))
                return;

            if (stateType == currentState.StateType)
                return;
            

            var state = states[stateType];
            if (!currentState.CanExist(this) || !state.CanEnter(this))
            {
                return;
            }
            
            currentState.OnExistState(this);
            currentState = state;
            currentState.OnEnterState(this);
        }
    }
}