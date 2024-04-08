using System;
using System.Collections.Generic;
using Disc0ver.FSM;
using UnityEngine;
using UnityEngine.Playables;

namespace Disc0ver
{
    public class AnimationComponent: MonoBehaviour
    {
        public Animator animator;
        private PlayableGraph _playableGraph;
        private Dictionary<StateType, AnimationLayer> _layerMap = new Dictionary<StateType, AnimationLayer>();

        private void Awake()
        {
            _playableGraph = PlayableGraph.Create("DCCAnimation");
        }

        private void Start()
        {
            _playableGraph.Play();
        }

        public void PlayAnimation()
        {
            
        }
    }
}