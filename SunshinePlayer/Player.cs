namespace SunshinePlayer {
    /// <summary>
    /// 音乐播放类
    /// </summary>
    class Player {
        /// <summary>
        /// 单利模式实例
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
    }
}
