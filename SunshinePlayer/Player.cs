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
        private Player(System.IntPtr windowHandle) {
            //注册 BassNet 库
            if(BassNetRegistration.eMail != null && BassNetRegistration.registrationKey != null) {
                BassNet.Registration(BassNetRegistration.eMail, BassNetRegistration.registrationKey);
            }
            //初始化 BassNet 库
            if(!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, windowHandle)) {
                Error error = getError();
                System.Windows.MessageBox.Show(error.code + " - " + error.content, error.title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error, System.Windows.MessageBoxResult.OK, System.Windows.MessageBoxOptions.ServiceNotification);
            }
        }
        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>播放器</returns>
        public Player getInstance(System.IntPtr windowHandle) {
            //外层判断
            if(instance == null) {
                //互斥锁
                lock(syncObject) {
                    //内层互斥判断
                    if(instance == null) {
                        //实例化单例对象
                        instance = new Player(windowHandle);
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
        /// <summary>
        /// 音量
        /// </summary>
        private int volumn = 100;

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否打开成功</returns>
        public bool openFile(string filePath) {
            stop();
            stream = Bass.BASS_StreamCreateFile(filePath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            return stream == 0;
        }
        /// <summary>
        /// 开始播放
        /// </summary>
        /// <param name="restart">重头开始</param>
        /// <returns>播放结果</returns>
        public bool play(bool restart = false) {
            return stream != 0 && Bass.BASS_ChannelPlay(stream, restart);
        }
        /// <summary>
        /// 暂停播放
        /// </summary>
        /// <returns>暂停结果</returns>
        public bool pause() {
            return Bass.BASS_ChannelPause(stream);
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
        /// <summary>
        /// 取音乐长度
        /// </summary>
        /// <returns>秒数</returns>
        public double getLength() {
            if(stream != 0) {
                return Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream));
            } else {
                return -1;
            }
        }
        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volumn">0~100音量</param>
        public void setVolumn(int volumn) {
            this.volumn = volumn;
            if(stream != 0) {
                Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, volumn / 100f);
            }
        }
        /// <summary>
        /// 获取音量
        /// </summary>
        /// <returns>音量</returns>
        public int getVolumn() {
            return volumn;
        }
        /// <summary>
        /// 取播放状态
        /// </summary>
        /// <returns>播放状态</returns>
        public BASSActive getStatus() {
            if(stream != 0) {
                return Bass.BASS_ChannelIsActive(stream);
            }
            return BASSActive.BASS_ACTIVE_STOPPED;
        }
        /// <summary>
        /// 获取音乐信息
        /// </summary>
        /// <returns>音乐ID3V1信息</returns>
        public MusicID3 getInformation() {
            MusicID3 i = new MusicID3();
            if(stream != 0) {
                string[] info = Bass.BASS_ChannelGetTagsID3V1(stream);
                if(info != null) {
                    i.title = info[0];
                    i.artist = info[1];
                    i.album = info[2];
                    i.year = info[3];
                    i.comment = info[4];
                    i.genre_id = info[5];
                    i.track = info[6];
                }
                info = Bass.BASS_ChannelGetTagsID3V2(stream);
                if(info != null) {
                    foreach(string s in info) {
                        if(s.StartsWith("TIT2", true, null)) {
                            i.title = s.Remove(0, 5);
                        } else if(s.StartsWith("TPE1", true, null)) {
                            i.artist = s.Remove(0, 5);
                        } else if(s.StartsWith("TALB", true, null)) {
                            i.album = s.Remove(0, 5);
                        }
                    }
                }
            }
            return i;
        }
        /// <summary>
        /// 取错误信息
        /// </summary>
        /// <returns>错误信息</returns>
        public Error getError() {
            return Error.getError(Bass.BASS_ErrorGetCode());
        }
    }
}
