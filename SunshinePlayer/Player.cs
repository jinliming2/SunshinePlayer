using Un4seen.Bass;

namespace SunshinePlayer {
    /// <summary>
    /// 音乐播放类
    /// </summary>
    class Player {
        #region 单例模式
        /// <summary>
        /// 单例模式实例
        /// </summary>
        private Player instance = null;
        /// <summary>
        /// 互斥锁对象
        /// </summary>
        private static readonly object syncObject = new object();
        /// <summary>
        /// 构造函数私有化
        /// </summary>
        private Player() {}
        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>播放器</returns>
        public Player getInstance() {
            //外层判断
            if(instance == null) {
                //互斥锁
                lock(syncObject) {
                    //内层互斥判断
                    if(instance == null) {
                        //实例化单例对象
                        instance = new Player();
                    }
                }
            }
            //返回单例对象
            return instance;
        }
        #endregion
        /// <summary>
        /// 文件流
        /// </summary>
        private int stream = 0;
        public bool openFile(string filePath) {
            stop();
            stream = Bass.BASS_StreamCreateFile(filePath, 0L, 0L, BASSFlag.BASS_MUSIC_FLOAT);
            return stream == 0;
        }
        /// <summary>
        /// 停止播放
        /// </summary>
        public void stop() {
            if(stream != 0) {
                Bass.BASS_ChannelStop(stream);
                Bass.BASS_StreamFree(stream);
            }
            stream = 0;
        }
        public Error getError() {
            return Error.getError(Bass.BASS_ErrorGetCode());
        }
    }
}
