namespace SunshinePlayer {
    /// <summary>
    /// 配置类
    /// </summary>
    class Config {
        /// <summary>
        /// 是否显示播放列表
        /// </summary>
        public static bool playListVisible = false;
        /// <summary>
        /// 音量
        /// </summary>
        public static int volumn = 100;
        /// <summary>
        /// 播放列表当前
        /// </summary>
        public static int playlistIndex = 0;
        /// <summary>
        /// 播放模式
        /// </summary>
        public static PlayModel playModel = PlayModel.CirculationList;
        /// <summary>
        /// 播放模式
        /// </summary>
        public enum PlayModel {
            /// <summary>
            /// 单曲循环
            /// </summary>
            SingleCycle,
            /// <summary>
            /// 顺序播放
            /// </summary>
            OrderPlay,
            /// <summary>
            /// 列表循环
            /// </summary>
            CirculationList,
            /// <summary>
            /// 随机播放
            /// </summary>
            ShufflePlayback
        }
    }
}
