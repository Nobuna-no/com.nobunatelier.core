using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier.Tests
{
    public class PlayModeTestStateModule_LogState : StateComponentModule
    {
        public override void Enter()
        {
            PlayModeTestLogger.LogCommand($"[{Time.frameCount}] Enter {ModuleOwner.name}.");
        }
        public override void Exit()
        {
            PlayModeTestLogger.LogCommand($"[{Time.frameCount}] Exit {ModuleOwner.name}.");
        }
    }
}
