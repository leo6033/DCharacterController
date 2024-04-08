using System;
using Animancer;
using UnityEngine;

namespace Disc0ver.Test
{
    public class TestTurnMixer: MonoBehaviour
    {
        public AnimancerComponent animancer;
        public AnimationClip turn1;
        public AnimationClip turn2;

        [Range(0, 1)]
        public float param;

        private LinearMixerState _rotateMixer = new LinearMixerState();
        
        private void Awake()
        {
            Play();
        }

        public void Play()
        {
            _rotateMixer.Initialize(turn1, turn2);
            _rotateMixer.Parameter = param;

            var state = animancer.Play(_rotateMixer);
            state.Events.OnEnd = Play;
        }
    }
}