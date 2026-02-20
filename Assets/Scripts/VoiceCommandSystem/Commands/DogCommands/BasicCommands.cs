using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Commands.DogCommands
{
    /// <summary>
    /// 「おすわり」コマンド
    /// </summary>
    public class SitCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Sit";
        public override string[] Keywords => new[]
        {
            "おすわり", "お座り", "すわり", "座り", "座れ", "すわれ",
            "sit", "おすわ", "すわ", "座って", "すわって"
        };
        public override string Description => "犬を座らせる";
        public override int Priority => 10;

        public SitCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[SitCommand] ✅ Dog is sitting");
            // おすわり: ActionBoolでアクションモードに入る
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「おて」コマンド
    /// </summary>
    public class PawCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Paw";
        public override string[] Keywords => new[]
        {
            "おて", "お手", "手", "paw", "右手", "みぎて",
            "手を出して", "手出して", "おてて", "てて"
        };
        public override string Description => "犬に手を出させる";
        public override int Priority => 10;

        public PawCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[PawCommand] ✅ Dog is giving right paw");
            // おて: 右手を出す
            dogController.ActionRPaw();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「おかわり」コマンド
    /// </summary>
    public class OkawariCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Okawari";
        public override string[] Keywords => new[]
        {
            "おかわり", "お代わり", "お替わり", "かわり", "代わり",
            "左手", "ひだりて", "反対", "はんたい", "もう片方",
            "もう一回", "おかわ"
        };
        public override string Description => "反対の手を出させる";
        public override int Priority => 10;

        public OkawariCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[OkawariCommand] ✅ Dog is giving left paw");
            // おかわり: 左手を出す
            dogController.ActionLPaw();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「ふせ」コマンド
    /// </summary>
    public class DownCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Down";
        public override string[] Keywords => new[]
        {
            "ふせ", "伏せ", "フセ", "down", "ダウン",
            "伏せて", "ふせて", "寝そべれ", "ねそべれ"
        };
        public override string Description => "犬を伏せさせる";
        public override int Priority => 10;

        public DownCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[DownCommand] ✅ Dog is lying down");
            // ふせ: 伏せる
            dogController.ActionLieDown();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「たっち」コマンド
    /// </summary>
    public class StandCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Stand";
        public override string[] Keywords => new[]
        {
            "たっち", "タッチ", "立って", "たって", "二足立ち",
            "立ち上がって", "たちあがって", "スタンド", "stand",
            "起立", "きりつ"
        };
        public override string Description => "二足立ちさせる";
        public override int Priority => 10;

        public StandCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[StandCommand] ✅ Dog is standing on two legs");
            // タッチ/立て: 二足立ち
            dogController.ActionStand();
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「まて」コマンド
    /// </summary>
    public class WaitCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Wait";
        public override string[] Keywords => new[]
        {
            "まて", "待て", "マテ", "wait", "ウェイト",
            "待って", "まって", "動くな", "うごくな", "ストップ", "stop"
        };
        public override string Description => "犬を待たせる";
        public override int Priority => 15;

        public WaitCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[WaitCommand] ✅ Dog is waiting");
            // まて: 待機モードに入る
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「よし」コマンド
    /// </summary>
    public class OkayCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Okay";
        public override string[] Keywords => new[]
        {
            "よし", "良し", "ヨシ", "ok", "オッケー", "okay",
            "いいよ", "go", "ゴー", "解除", "かいじょ",
            "動いていいよ", "うごいていいよ"
        };
        public override string Description => "待て解除";
        public override int Priority => 15;

        public OkayCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;

            GameLogger.Log(LogCategory.Voice,"[OkayCommand] ✅ Released from wait");
            // よし: 待機解除
            dogController.ActionBool(false);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }
}
