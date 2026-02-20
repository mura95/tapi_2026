using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Fusion;
using Fusion.Sockets;
using TapHouse.MetaverseWalk.Network;

namespace TapHouse.MetaverseWalk.Tests
{
    /// <summary>
    /// MetaverseErrorHandler のテスト
    /// エラーコード→メッセージマッピング、ShutdownReason変換、定数値検証等
    ///
    /// 実行方法: Unity Editor → Window → General → Test Runner → EditMode → Run All
    /// </summary>
    [TestFixture]
    public class MetaverseErrorHandlerTests
    {
        [SetUp]
        public void SetUp()
        {
            MetaverseErrorHandler.ClearError();
        }

        #region CreateError テスト

        [Test]
        public void CreateError_E001_NoNetwork_ReturnsCorrectMessage()
        {
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E001_NoNetwork);

            Assert.AreEqual(MetaverseErrorCode.E001_NoNetwork, error.Code);
            Assert.IsTrue(error.UserMessage.Contains("インターネット"));
            Assert.IsTrue(error.CanRetry);
            Assert.IsFalse(error.AutoReturn);
        }

        [Test]
        public void CreateError_E002_RoomSearchTimeout_ReturnsRetryableError()
        {
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E002_RoomSearchTimeout);

            Assert.AreEqual(MetaverseErrorCode.E002_RoomSearchTimeout, error.Code);
            Assert.IsTrue(error.CanRetry);
            Assert.IsFalse(error.AutoReturn);
        }

        [Test]
        public void CreateError_E003_RoomFull_ReturnsRetryableError()
        {
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E003_RoomFull);

            Assert.AreEqual(MetaverseErrorCode.E003_RoomFull, error.Code);
            Assert.IsTrue(error.UserMessage.Contains("混み合"));
            Assert.IsTrue(error.CanRetry);
        }

        [Test]
        public void CreateError_E004_Disconnected_NoRetryButton()
        {
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E004_Disconnected);

            Assert.AreEqual(MetaverseErrorCode.E004_Disconnected, error.Code);
            Assert.IsFalse(error.CanRetry);
            Assert.IsFalse(error.AutoReturn);
        }

        [Test]
        public void CreateError_E006_ServerError_AutoReturns()
        {
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E006_ServerError);

            Assert.AreEqual(MetaverseErrorCode.E006_ServerError, error.Code);
            Assert.IsFalse(error.CanRetry);
            Assert.IsTrue(error.AutoReturn);
        }

        [Test]
        public void CreateError_E007_BackgroundTimeout_AutoReturns()
        {
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E007_BackgroundTimeout);

            Assert.AreEqual(MetaverseErrorCode.E007_BackgroundTimeout, error.Code);
            Assert.IsTrue(error.AutoReturn);
        }

        [Test]
        public void CreateError_E008_ReconnectFailed_AutoReturns()
        {
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E008_ReconnectFailed);

            Assert.AreEqual(MetaverseErrorCode.E008_ReconnectFailed, error.Code);
            Assert.IsTrue(error.AutoReturn);
        }

        [Test]
        public void CreateError_WithDebugDetail_IncludesInDebugMessage()
        {
            string detail = "Custom debug info";
            var error = MetaverseErrorHandler.CreateError(MetaverseErrorCode.E001_NoNetwork, detail);

            Assert.AreEqual(detail, error.DebugMessage);
        }

        [Test]
        public void CreateError_AllCodes_HaveNoTechnicalTermsInUserMessage()
        {
            // 高齢者向け: 技術用語を含まないことを検証
            string[] technicalTerms = { "サーバー", "タイムアウト", "エラー", "ネットワーク切断", "Exception", "null" };

            var allCodes = System.Enum.GetValues(typeof(MetaverseErrorCode));
            foreach (MetaverseErrorCode code in allCodes)
            {
                if (code == MetaverseErrorCode.None) continue;

                var error = MetaverseErrorHandler.CreateError(code);

                foreach (string term in technicalTerms)
                {
                    Assert.IsFalse(
                        error.UserMessage.Contains(term),
                        $"Code {code}: UserMessage contains technical term '{term}': {error.UserMessage}");
                }
            }
        }

        #endregion

        #region ShutdownReason / ConnectFailedReason 変換テスト

        [Test]
        public void FromShutdownReason_Ok_ReturnsNone()
        {
            var code = MetaverseErrorHandler.FromShutdownReason(ShutdownReason.Ok);
            Assert.AreEqual(MetaverseErrorCode.None, code);
        }

        [Test]
        public void FromShutdownReason_MaxCcuReached_ReturnsRoomFull()
        {
            var code = MetaverseErrorHandler.FromShutdownReason(ShutdownReason.MaxCcuReached);
            Assert.AreEqual(MetaverseErrorCode.E003_RoomFull, code);
        }

        [Test]
        public void FromShutdownReason_OperationTimeout_ReturnsTimeout()
        {
            var code = MetaverseErrorHandler.FromShutdownReason(ShutdownReason.OperationTimeout);
            Assert.AreEqual(MetaverseErrorCode.E002_RoomSearchTimeout, code);
        }

        [Test]
        public void FromConnectFailedReason_Timeout_ReturnsTimeout()
        {
            var code = MetaverseErrorHandler.FromConnectFailedReason(NetConnectFailedReason.Timeout);
            Assert.AreEqual(MetaverseErrorCode.E002_RoomSearchTimeout, code);
        }

        [Test]
        public void FromConnectFailedReason_ServerFull_ReturnsRoomFull()
        {
            var code = MetaverseErrorHandler.FromConnectFailedReason(NetConnectFailedReason.ServerFull);
            Assert.AreEqual(MetaverseErrorCode.E003_RoomFull, code);
        }

        #endregion

        #region RaiseError / ClearError テスト

        [Test]
        public void RaiseError_SetsLastError()
        {
            MetaverseErrorHandler.RaiseError(MetaverseErrorCode.E001_NoNetwork);

            Assert.IsTrue(MetaverseErrorHandler.LastError.HasValue);
            Assert.AreEqual(MetaverseErrorCode.E001_NoNetwork, MetaverseErrorHandler.LastError.Value.Code);
        }

        [Test]
        public void RaiseError_InvokesOnErrorEvent()
        {
            MetaverseError? receivedError = null;
            void handler(MetaverseError e) => receivedError = e;

            MetaverseErrorHandler.OnError += handler;
            try
            {
                MetaverseErrorHandler.RaiseError(MetaverseErrorCode.E002_RoomSearchTimeout);
                Assert.IsTrue(receivedError.HasValue);
                Assert.AreEqual(MetaverseErrorCode.E002_RoomSearchTimeout, receivedError.Value.Code);
            }
            finally
            {
                MetaverseErrorHandler.OnError -= handler;
            }
        }

        [Test]
        public void ClearError_ResetsLastError()
        {
            MetaverseErrorHandler.RaiseError(MetaverseErrorCode.E001_NoNetwork);
            MetaverseErrorHandler.ClearError();

            Assert.IsFalse(MetaverseErrorHandler.LastError.HasValue);
        }

        [Test]
        public void ClearError_InvokesOnErrorClearedEvent()
        {
            bool cleared = false;
            void handler() => cleared = true;

            MetaverseErrorHandler.OnErrorCleared += handler;
            try
            {
                MetaverseErrorHandler.ClearError();
                Assert.IsTrue(cleared);
            }
            finally
            {
                MetaverseErrorHandler.OnErrorCleared -= handler;
            }
        }

        #endregion

        #region NetworkConstants テスト

        [Test]
        public void NetworkConstants_MaxPlayersPerRoom_Is10()
        {
            Assert.AreEqual(10, NetworkConstants.MAX_PLAYERS_PER_ROOM);
        }

        [Test]
        public void NetworkConstants_TickRate_Is30()
        {
            Assert.AreEqual(30, NetworkConstants.TICK_RATE);
        }

        [Test]
        public void NetworkConstants_FixedRegion_IsJapan()
        {
            Assert.AreEqual("jp", NetworkConstants.FIXED_REGION);
        }

        [Test]
        public void NetworkConstants_MaxPlayerNameLength_Is10()
        {
            Assert.AreEqual(10, NetworkConstants.MAX_PLAYER_NAME_LENGTH);
        }

        [Test]
        public void NetworkConstants_ConnectionTimeout_Is30Seconds()
        {
            Assert.AreEqual(30000, NetworkConstants.CONNECTION_TIMEOUT_MS);
        }

        [Test]
        public void NetworkConstants_BackgroundThreshold_Is30Seconds()
        {
            Assert.AreEqual(30, NetworkConstants.BACKGROUND_REJOIN_THRESHOLD_SEC);
        }

        [Test]
        public void NetworkConstants_ExponentialBackoff_Values()
        {
            // 指数バックオフ: 2s → 4s → 8s
            int baseMs = NetworkConstants.RECONNECT_BASE_INTERVAL_MS;
            Assert.AreEqual(2000, baseMs * (1 << 0)); // 1回目: 2秒
            Assert.AreEqual(4000, baseMs * (1 << 1)); // 2回目: 4秒
            Assert.AreEqual(8000, baseMs * (1 << 2)); // 3回目: 8秒
        }

        #endregion

        #region プレイヤー名バリデーション テスト

        [Test]
        public void PlayerNameValidation_ShortName_Unchanged()
        {
            string name = "テスト";
            string validated = ValidateName(name);
            Assert.AreEqual("テスト", validated);
        }

        [Test]
        public void PlayerNameValidation_ExactLength_Unchanged()
        {
            string name = "1234567890"; // 10文字
            string validated = ValidateName(name);
            Assert.AreEqual(10, validated.Length);
            Assert.AreEqual(name, validated);
        }

        [Test]
        public void PlayerNameValidation_TooLong_Truncated()
        {
            string name = "とても長い名前のユーザーです"; // 13文字
            string validated = ValidateName(name);
            Assert.AreEqual(NetworkConstants.MAX_PLAYER_NAME_LENGTH, validated.Length);
        }

        [Test]
        public void PlayerNameValidation_EmptyName_Unchanged()
        {
            string name = "";
            string validated = ValidateName(name);
            Assert.AreEqual("", validated);
        }

        /// <summary>
        /// NetworkPlayerController.Spawned() と同じバリデーションロジック
        /// </summary>
        private static string ValidateName(string name)
        {
            if (name.Length > NetworkConstants.MAX_PLAYER_NAME_LENGTH)
            {
                return name.Substring(0, NetworkConstants.MAX_PLAYER_NAME_LENGTH);
            }
            return name;
        }

        #endregion
    }
}
