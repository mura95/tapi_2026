using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Commands.DogCommands
{
    /// <summary>
    /// 「バーン」コマンド
    /// </summary>
    public class BangCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Bang";
        public override string[] Keywords => new[]
        {
            "バーン", "ばーん", "bang", "バン",
            "死んだふり", "しんだふり", "死んで", "しんで",
            "やられた", "バタン", "ばたん", "倒れて", "たおれて",
            "死んだ", "しんだ", "ばたっ"
        };
        public override string Description => "死んだふりをする";
        public override int Priority => 10;

        public BangCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[BangCommand] ✅ Dog is playing dead");
            // バーン: 死んだふり
            dogController.ActionDang();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「ちんちん」コマンド
    /// </summary>
    public class ChinChinCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "ChinChin";
        public override string[] Keywords => new[]
        {
            "ちんちん", "チンチン", "chin",
            "お座りして手", "おすわりして手", "座って手",
            "前足上げて", "まえあしあげて", "おねだり", "お願い"
        };
        public override string Description => "お座りして前足を上げる";
        public override int Priority => 10;

        public ChinChinCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[ChinChinCommand] ✅ Dog is doing chin-chin (standing)");
            // ちんちん: 二足立ち
            dogController.ActionStand();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「ハイタッチ」コマンド
    /// </summary>
    public class HighFiveCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "HighFive";
        public override string[] Keywords => new[]
        {
            "ハイタッチ", "はいたっち", "highfive", "ハイファイブ",
            "タッチ", "たっち", "touch", "タッチして",
            "ハイ", "はい", "パチン", "ぱちん"
        };
        public override string Description => "ハイタッチする";
        public override int Priority => 10;

        public HighFiveCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[HighFiveCommand] ✅ Dog is giving high five");
            // ハイタッチ: 右手を出す（お手と同じ動作）
            dogController.ActionRPaw();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }
}
