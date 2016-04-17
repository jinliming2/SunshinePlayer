namespace SunshinePlayer {
    /// <summary>
    /// 音乐ID3V1信息
    /// </summary>
    struct MusicID3 {
        /// <summary>
        /// 标题
        /// </summary>
        public string title;  //max. 30 chars
        /// <summary>
        /// 艺术家
        /// </summary>
        public string artist;  //max. 30 chars
        /// <summary>
        /// 专辑
        /// </summary>
        public string album;  //max. 30 chars
        /// <summary>
        /// 年份
        /// </summary>
        public string year;  //yyyy
        /// <summary>
        /// 评论
        /// </summary>
        public string comment;  //max. 28 chars
        /// <summary>
        /// 标识码
        /// </summary>
        public string genre_id;
        /// <summary>
        /// 轨道
        /// </summary>
        public string track;  //0-255
    }
}
