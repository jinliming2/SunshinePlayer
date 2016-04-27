using System;
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
        private static Player instance = null;
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
                Error error = this.error;
                System.Windows.MessageBox.Show(error.code + " - " + error.content, error.title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error, System.Windows.MessageBoxResult.OK, System.Windows.MessageBoxOptions.ServiceNotification);
            }
            //默认音量
            volumn = 100;
        }
        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>播放器</returns>
        public static Player getInstance(System.IntPtr windowHandle) {
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
        /// 析构函数
        /// </summary>
        ~Player() {
            //停止并释放音乐
            stop();
        }

        /// <summary>
        /// 文件流
        /// </summary>
        private int stream = 0;
        /// <summary>
        /// 音量值记录
        /// </summary>
        private int _volumn = 100;
        /// <summary>
        /// 频谱数据
        /// </summary>
        private float[] _spectrum = new float[128];

        /// <summary>
        /// 设置静音
        /// </summary>
        public bool mute {
            set {
                if(value) {
                    int v = _volumn;
                    volumn = 0;
                    _volumn = v;
                } else {
                    volumn = _volumn;
                }
            }
        }
        /// <summary>
        /// 音量
        /// </summary>
        public int volumn {
            get {
                float value = 100;
                if(Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref value)) {
                    _volumn = (int)(Math.Round(value * 100));
                    return _volumn;
                } else {
                    return 100;
                }
            }
            set {
                //记录音量
                _volumn = value;
                //设置音量
                if(stream != 0) {
                    Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, value / 100f);
                }
            }
        }
        /// <summary>
        /// 是否已打开过文件
        /// </summary>
        public bool openedFile {
            get {
                return stream != 0;
            }
        }
        /// <summary>
        /// 音乐长度
        /// </summary>
        public double length {
            get {
                return Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream));
            }
        }
        /// <summary>
        /// 播放进度
        /// </summary>
        public double position {
            get {
                return Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetPosition(stream));
            }
            set {
                if(stream != 0) {
                    Bass.BASS_ChannelSetPosition(stream, value);
                }
            }
        }
        /// <summary>
        /// 播放状态
        /// </summary>
        public BASSActive status {
            get {
                if(stream != 0) {
                    return Bass.BASS_ChannelIsActive(stream);
                }
                return BASSActive.BASS_ACTIVE_STOPPED;
            }
        }
        /// <summary>
        /// 获取频谱数据
        /// </summary>
        public float[] spectrum {
            get {
                if(stream != 0 && status == BASSActive.BASS_ACTIVE_PLAYING) {
                    Bass.BASS_ChannelGetData(stream, _spectrum, (int)BASSData.BASS_DATA_FFT256);
                } else {
                    Array.Clear(_spectrum, 0, _spectrum.Length);
                }
                return _spectrum;
            }
        }
        /// <summary>
        /// 音乐ID3信息
        /// </summary>
        public MusicID3 information {
            get {
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
        }
        /// <summary>
        /// 错误信息
        /// </summary>
        public Error error {
            get {
                return Error.getError(Bass.BASS_ErrorGetCode());
            }
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否打开成功</returns>
        public bool openFile(string filePath) {
            //停止当前的播放
            stop();
            //打开新文件
            stream = Bass.BASS_StreamCreateFile(filePath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            //设置音量
            volumn = _volumn;
            return stream != 0;
        }
        /// <summary>
        /// 开始播放
        /// </summary>
        /// <param name="restart">重头开始</param>
        /// <returns>播放结果</returns>
        public bool play(bool restart = false) {
            //设置音量
            volumn = _volumn;
            //播放
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
        /// 获取指定音乐文件的ID3信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>音乐ID3信息</returns>
        public MusicID3? getInformation(string filePath) {
            //打开文件
            int s = Bass.BASS_StreamCreateFile(filePath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
            if(s == 0) {
                //打开失败
                return null;
            }
            //获取ID3信息
            MusicID3 i = new MusicID3();
            string[] info = Bass.BASS_ChannelGetTagsID3V1(s);
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
                foreach(string str in info) {
                    if(str.StartsWith("TIT2", true, null)) {
                        i.title = str.Remove(0, 5);
                    } else if(str.StartsWith("TPE1", true, null)) {
                        i.artist = str.Remove(0, 5);
                    } else if(str.StartsWith("TALB", true, null)) {
                        i.album = str.Remove(0, 5);
                    }
                }
            }
            //释放文件
            Bass.BASS_StreamFree(s);
            return i;
        }
    }
}
