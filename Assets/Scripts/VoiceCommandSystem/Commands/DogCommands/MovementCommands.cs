using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Commands.DogCommands
{
    /// <summary>
    /// 「おいで」コマンド
    /// </summary>
    public class ComeCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Come";
        public override string[] Keywords => new[]
        {
            "おいで", "お出で", "オイデ", "come", "カム",
            "来い", "こい", "コイ", "来て", "きて", "こっち",
            "こっち来い", "こっちこい", "こっちおいで",
            "ここ", "ここおいで", "近く", "ちかく"
        };
        public override string Description => "犬を呼び寄せる";
        public override int Priority => 10;

        public ComeCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[ComeCommand] ✅ Dog is coming");
            // おいで: アクションモードに入り、近くに来る
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「まわれ」コマンド
    /// </summary>
    public class TurnCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Turn";
        public override string[] Keywords => new[]
        {
            "まわれ", "回れ", "マワレ", "turn", "ターン",
            "回って", "まわって", "くるくる", "クルクル",
            "回転", "かいてん", "一回転", "いっかいてん",
            "スピン", "spin", "ぐるぐる", "グルグル"
        };
        public override string Description => "その場で回転させる";
        public override int Priority => 10;

        public TurnCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[TurnCommand] ✅ Dog is dancing/spinning");
            // まわれ: ダンス（回転）
            dogController.ActionDance();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「ジャンプ」コマンド
    /// </summary>
    public class JumpCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Jump";
        public override string[] Keywords => new[]
        {
            "ジャンプ", "じゃんぷ", "jump", "ジャンプして",
            "跳べ", "とべ", "飛べ", "跳んで", "とんで", "飛んで",
            "ぴょん", "ピョン", "ジャーンプ", "ジャンピング"
        };
        public override string Description => "ジャンプさせる";
        public override int Priority => 10;

        public JumpCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[JumpCommand] ✅ Dog is jumping (high dance)");
            // ジャンプ: ハイダンス（ジャンプ的な動き）
            dogController.ActionHighDance();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }
}
