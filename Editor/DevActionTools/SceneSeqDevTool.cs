using UnityEngine;

namespace GameLib
{
    /// Triggers custom sequence runners, release scene playback, or sequence iteration.
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

        /// Executes the sequence, release scene playback, or sequence selection.
        public override void Execute()
        {
            switch (actionType)
            {
                case SequenceAction.RunStartSceneAsRelease:
                    EditorPlayAsRelease.RunStartScene();
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