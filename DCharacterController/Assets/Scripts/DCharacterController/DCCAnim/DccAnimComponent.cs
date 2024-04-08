using System;
using System.Collections.Generic;
using Disc0ver.FSM;
using UnityEngine;
using UnityEngine.Playables;

namespace Disc0ver
{
    public class DccAnimComponent: MonoBehaviour
    {
        public Animator animator;
        private PlayableGraph _playableGraph;
        private Dictionary<Layer, DccAnimLayer> _layerMap = new Dictionary<Layer, DccAnimLayer>();

        private void Awake()
        {
            _playableGraph = PlayableGraph.Create("DCCAnimation");
        }

        private void Start()
        {
            _playableGraph.Play();
        }

        public void PlayAnimation(AnimationClip animationClip, Layer layer)
        {
            
        }
    }
}