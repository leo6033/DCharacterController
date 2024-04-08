using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Disc0ver
{
    public enum Layer
    {
        Base,
    }
    
    public class DccAnimLayer
    {
        public AvatarMask mask;
        public bool isAdditive;
        
    }

    public class DccAnimPlayable : PlayableBehaviour
    {
        private PlayableGraph _root;

        public DccAnimPlayable(PlayableGraph root)
        {
            _root = root;
        }
        
        public void CreatePlayable(out Playable playable) => playable = AnimationMixerPlayable.Create(_root);
    }
}