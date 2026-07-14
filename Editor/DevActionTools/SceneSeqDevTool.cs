// todo: Add validation in DevActionTool execution to ensure Play Mode transitions only trigger when Unity is not already compiling or busy.
// idea: Add a custom icon or color highlight in the Project window for DevActionTool ScriptableObjects so they are easily recognizable.

using UnityEngine;
using GameLib.Editor; // Added to access our modernized Phase 5 editor controllers

namespace GameLib
{
    [CreateAssetMenu(menuName = "GameLib/Debug/DevKeyboardShortcuts/DevTools/Scene Sequence Play Tool", fileName = "SceneSeqTool")]
    public class SceneSeqDevTool : DevActionTool
    {
        public enum SequenceAction
        {
            RunSelectedSequence,
            RunStartSceneAsRelease,
            SelectNextSequence
        }

        [Header("Action Configuration")]
        public SequenceAction actionType;

        public override void Execute()
        {
            switch (actionType)
            {
                case SequenceAction.RunStartSceneAsRelease:                    
                    SceneSequenceController.RunStartScene();
                    break;

                case SequenceAction.RunSelectedSequence:
                    SceneSequenceController.RunSelectedSequence();
                    break;

                case SequenceAction.SelectNextSequence:
                    SceneSequenceController.SelectNextSequence();
                    break;
            }
        }
    }
}