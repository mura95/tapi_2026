using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Commands.DogCommands
{
    /// <summary>
    /// 「いい子」コマンド
    /// </summary>
    public class GoodBoyCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "GoodBoy";
        public override string[] Keywords => new[]
        {
            "いい子", "いいこ", "良い子", "よい子", "よいこ",
            "いい子だね", "いいこだね", "良い子だね",
            "good", "グッド"
        };
        public override string Description => "良い子だねと褒める";
        public override int Priority => 5;

        public GoodBoyCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[GoodBoyCommand] ✅ Praising: Good boy!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「えらい」コマンド
    /// </summary>
    public class GreatCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Great";
        public override string[] Keywords => new[]
        {
            "えらい", "偉い", "エライ",
            "えらいね", "偉いね", "えらいぞ",
            "great", "グレート"
        };
        public override string Description => "偉いねと褒める";
        public override int Priority => 5;

        public GreatCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[GreatCommand] ✅ Praising: Great!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「よくできました」コマンド
    /// </summary>
    public class WellDoneCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "WellDone";
        public override string[] Keywords => new[]
        {
            "よくできました", "よくできた", "良くできました", "良くできた",
            "上手", "じょうず", "上手い", "うまい", "うまいね",
            "上手だね", "じょうずだね", "上手にできたね",
            "welldone", "ウェルダン"
        };
        public override string Description => "よくできたねと褒める";
        public override int Priority => 5;

        public WellDoneCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[WellDoneCommand] ✅ Praising: Well done!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「すごい」コマンド
    /// </summary>
    public class AmazingCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Amazing";
        public override string[] Keywords => new[]
        {
            "すごい", "凄い", "スゴイ", "すごいね",
            "凄いね", "すげー", "すげえ",
            "amazing", "アメージング", "awesome", "オーサム"
        };
        public override string Description => "すごいねと褒める";
        public override int Priority => 5;

        public AmazingCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[AmazingCommand] ✅ Praising: Amazing!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「かしこい」コマンド
    /// </summary>
    public class SmartCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Smart";
        public override string[] Keywords => new[]
        {
            "かしこい", "賢い", "カシコイ",
            "かしこいね", "賢いね", "頭いい", "あたまいい",
            "smart", "スマート", "clever", "クレバー"
        };
        public override string Description => "賢いねと褒める";
        public override int Priority => 5;

        public SmartCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[SmartCommand] ✅ Praising: Smart!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「かわいい」コマンド
    /// </summary>
    public class CuteCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Cute";
        public override string[] Keywords => new[]
        {
            "かわいい", "可愛い", "カワイイ",
            "かわいいね", "可愛いね", "かわい", "可愛",
            "きゃわいい", "きゃわ", "cute", "キュート"
        };
        public override string Description => "可愛いねと褒める";
        public override int Priority => 5;

        public CuteCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[CuteCommand] ✅ Praising: Cute!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「大好き」コマンド
    /// </summary>
    public class LoveCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Love";
        public override string[] Keywords => new[]
        {
            "大好き", "だいすき", "ダイスキ",
            "大好きだよ", "だいすきだよ", "愛してる", "あいしてる",
            "love", "ラブ", "愛してるよ", "あいしてるよ"
        };
        public override string Description => "大好きだよと伝える";
        public override int Priority => 5;

        public LoveCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[LoveCommand] ✅ Expressing: I love you!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「好き」コマンド
    /// </summary>
    public class LikeCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Like";
        public override string[] Keywords => new[]
        {
            "好き", "すき", "スキ",
            "好きだよ", "すきだよ", "好きよ", "すきよ",
            "like", "ライク"
        };
        public override string Description => "好きだよと伝える";
        public override int Priority => 5;

        public LikeCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[LikeCommand] ✅ Expressing: I like you!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「ラブラブ」コマンド
    /// </summary>
    public class LoveLoveCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "LoveLove";
        public override string[] Keywords => new[]
        {
            "ラブラブ", "らぶらぶ", "lovelove",
            "ちゅっちゅ", "ちゅー", "チュー",
            "めちゃくちゃ好き", "めっちゃ好き"
        };
        public override string Description => "ラブラブと伝える";
        public override int Priority => 5;

        public LoveLoveCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[LoveLoveCommand] ✅ Expressing: Love love!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「がんばれ」コマンド
    /// </summary>
    public class CheerCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Cheer";
        public override string[] Keywords => new[]
        {
            "がんばれ", "頑張れ", "ガンバレ",
            "がんばって", "頑張って", "応援してるよ",
            "おうえんしてるよ", "cheer", "チアー"
        };
        public override string Description => "頑張れと応援する";
        public override int Priority => 5;

        public CheerCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[CheerCommand] ✅ Cheering: Ganbare!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「ファイト」コマンド
    /// </summary>
    public class FightCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Fight";
        public override string[] Keywords => new[]
        {
            "ファイト", "ふぁいと", "fight",
            "ファイトだよ", "ファイトー", "ふぁいとー"
        };
        public override string Description => "ファイトと応援する";
        public override int Priority => 5;

        public FightCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[FightCommand] ✅ Cheering: Fight!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「できるよ」コマンド
    /// </summary>
    public class YouCanDoItCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "YouCanDoIt";
        public override string[] Keywords => new[]
        {
            "できるよ", "出来るよ", "デキルヨ",
            "できる", "出来る", "やればできる",
            "大丈夫", "だいじょうぶ", "いける"
        };
        public override string Description => "できるよと励ます";
        public override int Priority => 5;

        public YouCanDoItCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[YouCanDoItCommand] ✅ Encouraging: You can do it!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「ご褒美」コマンド
    /// </summary>
    public class RewardCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Reward";
        public override string[] Keywords => new[]
        {
            "ご褒美", "ごほうび", "ゴホウビ",
            "ご褒美あげる", "ごほうびあげる", "ご褒美だよ",
            "reward", "リワード"
        };
        public override string Description => "ご褒美をあげる";
        public override int Priority => 5;

        public RewardCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[RewardCommand] ✅ Giving reward!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「おやつ」コマンド
    /// </summary>
    public class TreatCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Treat";
        public override string[] Keywords => new[]
        {
            "おやつ", "オヤツ",
            "おやつだよ", "おやつあげる",
            "treat", "トリート", "スナック", "snack"
        };
        public override string Description => "おやつをあげる";
        public override int Priority => 5;

        public TreatCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[TreatCommand] ✅ Giving treat!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }

    /// <summary>
    /// 「おいしい」コマンド
    /// </summary>
    public class YummyCommand : VoiceCommandBase
    {
        private DogController dogController;

        public override string CommandName => "Yummy";
        public override string[] Keywords => new[]
        {
            "おいしい", "美味しい", "オイシイ",
            "おいしいね", "美味しいね", "うまい", "うまいね",
            "yummy", "ヤミー", "delicious", "デリシャス"
        };
        public override string Description => "美味しいねと言う";
        public override int Priority => 5;

        public YummyCommand(DogController controller)
        {
            dogController = controller;
        }

        public override void Execute(VoiceCommandContext context)
        {
            LogExecution(context);
            if (dogController == null) return;
            
            GameLogger.Log(LogCategory.Voice,"[YummyCommand] ✅ Saying: Yummy!");
            dogController.ActionBool(true);
        }

        public override bool CanExecute()
        {
            return dogController != null;
        }
    }
}
