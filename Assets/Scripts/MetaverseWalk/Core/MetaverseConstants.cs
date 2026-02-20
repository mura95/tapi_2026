namespace TapHouse.MetaverseWalk.Core
{
    /// <summary>
    /// メタバース散歩機能で使用する定数
    /// </summary>
    public static class MetaverseConstants
    {
        // シーン名
        public const string METAVERSE_SCENE_NAME = "Metaverse";
        public const string MAIN_SCENE_NAME = "main";

        // レイヤー名
        public const string LAYER_GROUND = "Ground";
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_DOG = "Dog";

        // タグ名
        public const string TAG_PLAYER = "Player";
        public const string TAG_DOG = "Dog";
        public const string TAG_SPAWN_POINT = "SpawnPoint";

        // 移動パラメータ（旧: 犬操作・人追従）
        public const float DEFAULT_DOG_SPEED = 3.0f;
        public const float DEFAULT_PLAYER_SPEED = 3.0f;
        public const float DEFAULT_FOLLOW_DISTANCE = 1.5f;
        public const float DEFAULT_STOP_DISTANCE = 1.0f;

        // 移動パラメータ（新: 人操作・犬追従）
        public const float DEFAULT_PLAYER_CONTROL_SPEED = 2.5f;  // 人操作速度
        public const float DEFAULT_DOG_FOLLOWER_SPEED = 3.0f;    // 犬追従通常速度
        public const float DEFAULT_DOG_CATCHUP_SPEED = 5.0f;     // 犬追いつき速度
        public const float DEFAULT_DOG_FOLLOW_DISTANCE = 2.0f;   // 犬が前方に位置取る距離
        public const float DEFAULT_DOG_STOP_DISTANCE = 1.0f;     // 犬が停止する距離
        public const float SPAWN_OFFSET_FORWARD = 1.5f;          // スポーン前方オフセット
        public const float DOG_SCALE = 0.7f;                       // 犬のスケール（人に対する相対サイズ）

        // カメラパラメータ
        public const float DEFAULT_CAMERA_DISTANCE = 15f;
        public const float DEFAULT_CAMERA_HEIGHT = 10f;
        public const float DEFAULT_CAMERA_ANGLE = 45f;
        public const float DEFAULT_CAMERA_ROTATION = 45f;

        // UI
        public const float BUTTON_HOLD_THRESHOLD = 0.1f;

        // NavMesh
        public const int NAVMESH_AREA_WALKABLE = 0;
        public const int NAVMESH_AREA_NOT_WALKABLE = 1;
    }
}
