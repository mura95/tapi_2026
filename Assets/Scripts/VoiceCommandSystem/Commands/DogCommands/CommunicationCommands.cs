using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Commands.DogCommands
{
    /// <summary>
    /// 「吠えて」コマンド
    /// </summary>
    public class BarkCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Bark";
        public override string[] Keywords => new[]
        {
            "吠えて", "ほえて", "bark", "バーク",
            "ワンワン", "わんわん", "ワン", "わん",
            "鳴いて", "ないて", "声出して", "こえだして",
            "吠えろ", "ほえろ", "鳴け", "なけ",
            "わんわんして"
        };
        public override string Description => "犬を吠えさせる";
        public override int Priority => 10;

        public BarkCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[BarkCommand] ✅ Dog is barking");
            // 吠えろ: 吠えるアニメーション
            dogController.ActionBark();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「静かに」コマンド
    /// </summary>
    public class QuietCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Quiet";
        public override string[] Keywords => new[]
        {
            "静かに", "しずかに", "quiet", "クワイエット",
            "シー", "しー", "shh", "シーッ",
            "黙って", "だまって", "やめて", "ストップ",
            "うるさい", "静か", "しずか", "だまれ",
            "鳴くな", "なくな", "吠えるな", "ほえるな"
        };
        public override string Description => "吠えるのをやめさせる";
        public override int Priority => 15;

        public QuietCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[QuietCommand] ✅ Dog is quiet now");
            // 静かに: アクション解除（通常状態に戻る）
            dogController.ActionBool(false);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }
}
