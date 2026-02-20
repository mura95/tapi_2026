using UnityEngine;
using Whisper.Utils;
using UnityEngine.Android;
using System.Collections.Generic;
using TapHouse.Logging;


namespace Whisper.Samples
{
    /// <summary>
    /// 音声コマンドを常に録音し、特定のキーワードが含まれているかを判定するクラス
    /// </summary>
    public class VoiceCommand : MonoBehaviour
    {
        [SerializeField] public WhisperManager whisper;
        [SerializeField] public MicrophoneRecord microphoneRecord;
        private WhisperStream _stream;
        [SerializeField] private DogController _dogController;
        [SerializeField] private TurnAndMoveHandler _turnAndMoveHandler;

        private async void Start()
        {
            // **Androidのマイクの許可をリクエスト**
            RequestMicrophonePermission();

            // **Whisperストリームの初期化**
            _stream = await whisper.CreateStream(microphoneRecord);
            //_stream.OnResultUpdated += OnResult;
            _stream.OnSegmentUpdated += OnSegmentUpdated;
            //_stream.OnSegmentFinished += OnSegmentFinished;
            _stream.OnStreamFinished += OnFinished;

            // **マイク録音の設定**
            microphoneRecord.OnRecordStop += OnRecordStop;

            // **常に録音を開始**
            StartRecording();
        }

        /// <summary>
        /// Android用のマイク許可リクエスト
        /// </summary>
        private void RequestMicrophonePermission()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                {
                    Permission.RequestUserPermission(Permission.Microphone);
                    GameLogger.Log(LogCategory.Voice,"マイクの使用許可をリクエストしました。");
                }
            }
        }

        /// <summary>
        /// 録音を開始する
        /// </summary>
        private void StartRecording()
        {
            if (!microphoneRecord.IsRecording)
            {
                _stream.StartStream();
                microphoneRecord.StartRecord();
                GameLogger.Log(LogCategory.Voice,"録音を開始しました。");
            }
        }

        /// <summary>
        /// 録音が終了した時の処理（録音を再開する）
        /// </summary>
        private void OnRecordStop(AudioChunk recordedAudio)
        {
            GameLogger.Log(LogCategory.Voice,"録音が停止しました。再開します...");
            StartRecording();
        }

        /// <summary>
        /// 解析された音声テキストが更新された時の処理
        /// </summary>
        private void OnResult(string result)
        {
            GameLogger.Log(LogCategory.Voice,$"認識されたテキスト: {result}");
            AnalyzeCommand(result);
        }

        /// <summary>
        /// セグメント（途中の認識）が更新された時の処理
        /// </summary>
        private void OnSegmentUpdated(WhisperResult segment)
        {
            //GameLogger.Log(LogCategory.Voice,$"途中の音声解析: {segment.Result}");
            if (_dogController.GetSleepBool() == true) return;
            AnalyzeCommand(segment.Result);
        }

        /// <summary>
        /// セグメント（部分的な文章）が確定した時の処理
        /// </summary>
        private void OnSegmentFinished(WhisperResult segment)
        {
            GameLogger.Log(LogCategory.Voice,$"確定した音声解析: {segment.Result}");
        }

        /// <summary>
        /// ストリームが終了した時の処理
        /// </summary>
        private void OnFinished(string finalResult)
        {
            GameLogger.Log(LogCategory.Voice,"音声解析が完了しました。");
        }

        /// <summary>
        /// 認識されたテキストを解析し、特定のコマンドが含まれているか判定
        /// </summary>
        private void AnalyzeCommand(string text)
        {
            if (GlobalVariables.CurrentState != PetState.idle) return;
            string lowerText = text.ToLower();

            Dictionary<string[], System.Action> commandActions = new Dictionary<string[], System.Action>
    {
        { new string[] { "paw", "give paw" }, () => _dogController.ActionRPaw() },
        { new string[] { "shake", "give other paw" }, () => _dogController.ActionLPaw() },
        { new string[] { "lie down", "down", "lay down" }, () => _dogController.ActionLieDown() },
        { new string[] { "dance" }, () => _dogController.ActionDance() },
        { new string[] { "bang" }, () => _dogController.ActionDang() },
        { new string[] { "sit", "beg" }, () => _dogController.ActionStand() },
        { new string[] { "come here", "here boy" }, () => _turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 1.0f) },
        { new string[] { "bark", "speak", "talk" }, () => _dogController.ActionBark() }
    };

            // 入力されたテキストに含まれるコマンドを探す
            foreach (var entry in commandActions)
            {
                foreach (string command in entry.Key)
                {
                    if (lowerText.Contains(command))
                    {
                        entry.Value.Invoke(); // 登録されたアクションを実行
                        GameLogger.Log(LogCategory.Voice,$"コマンド '{command}' を検出し実行しました！");
                        return;
                    }
                }
            }
            GameLogger.Log(LogCategory.Voice,$"コマンドが見つかりません。（入力: {text}");
        }
    }
}