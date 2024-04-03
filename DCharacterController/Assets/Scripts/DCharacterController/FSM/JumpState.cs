namespace Disc0ver.FSM
{
    public class JumpState : BaseState
    {
        public override StateType StateType => StateType.Jump;
        private bool _notifyApex = false;
        
        public override void OnEnterState(StateMachine stateMachine)
        {
            _notifyApex = false;
        }

        public override void HandleInput(StateMachine stateMachine)
        {
            
        }

        public override void UpdateAnimation(StateMachine stateMachine)
        {
            
        }

        public override void OnExistState(StateMachine stateMachine)
        {
            
        }

        public void NotifyJumpApex()
        {
            _notifyApex = true;
        }
    }
}