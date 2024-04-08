namespace Disc0ver.FSM
{
    public class JumpState : BaseState
    {
        public override StateType StateType => StateType.Jump;

        public override void OnEnterState(StateMachine stateMachine)
        {
            stateMachine.animancerComponent.Animator.applyRootMotion = false;
            if (stateMachine.controller.NotifyApex)
            {
                stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.jump).Events.OnEnd = () =>
                {
                    stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.jumpFallingLoop, 0.1f);
                };
            }
            else
            {
                stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.jumpFallingLoop, 0.2f);
            }
                
        }

        public override void HandleInput(StateMachine stateMachine)
        {
            
        }

        public override void UpdateAnimation(StateMachine stateMachine)
        {

        }

        public override void OnExistState(StateMachine stateMachine)
        {
            stateMachine.animancerComponent.Play(stateMachine.controller.dccAnimClips.jumpLand);
        }
    }
}