using System;
using UnityEngine;

namespace Disc0ver
{
    [Serializable]
    public struct DccAnimClips
    {
        [Header("空闲")]
        public AnimationClip idle;
        public AnimationClip idle1;
        public AnimationClip idle2;
        [Header("移动")]
        // public AnimationClip walk;

        public AnimationClip runStart;
        public AnimationClip run;
        public AnimationClip runTurnLeft;
        public AnimationClip runTurnRight;
        public AnimationClip runStartTurnLeft90;
        public AnimationClip runStartTurnLeft135;
        public AnimationClip runStartTurnLeft180;
        public AnimationClip runStartTurnRight90;
        public AnimationClip runStartTurnRight135;
        public AnimationClip runStartTurnRight180;
    }
}