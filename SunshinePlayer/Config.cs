using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;

namespace SunshinePlayer {
    /// <summary>
    /// 配置类
    /// </summary>
    [Serializable]
    class Config {
        /// <summary>
        /// 播放模式
        /// </summary>
        [Serializable]
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
        #region 单例模式
        /// <summary>
        /// 单例模式实例
        /// </summary>
        private static Config instance = null;
        /// <summary>
        /// 构造函数私有化
        /// </summary>
        private Config() {
        }
        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>播放器</returns>
        public static Config getInstance() {
            //外层判断
            if(instance == null) {
                throw new Exception("配置尚未加载！");
            }
            //返回单例对象
            return instance;
        }
        #endregion
        /// <summary>
        /// 是否显示播放列表
        /// </summary>
        public bool playListVisible = false;
        /// <summary>
        /// 音量
        /// </summary>
        public int volumn = 100;
        /// <summary>
        /// 播放列表当前
        /// </summary>
        public int playlistIndex = 0;
        /// <summary>
        /// 播放模式
        /// </summary>
        public PlayModel playModel = PlayModel.CirculationList;
        /// <summary>
        /// 窗口位置
        /// </summary>
        public Point position;
        /// <summary>
        /// 自动播放
        /// </summary>
        public bool autoPlay = false;
        /// <summary>
        /// 歌词卡拉OK效果
        /// </summary>
        public bool lyricAnimation = true;
        /// <summary>
        /// 是否显示桌面歌词
        /// </summary>
        public bool showDesktopLyric = true;
        /// <summary>
        /// 桌面歌词位置
        /// </summary>
        public Point desktopLyricPosition = new Point(double.MinValue, double.MinValue);

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="path">路径</param>
        public static void saveConfig(string path) {
            //文件流
            Stream fStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            //二进制序列化器
            BinaryFormatter binFormat = new BinaryFormatter();
            //序列化对象
            binFormat.Serialize(fStream, getInstance());
            //关闭文件
            fStream.Flush();
            fStream.Close();
        }
        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="path">路径</param>
        public static void loadConfig(string path) {
            try {
                //文件流
                Stream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                //二进制反序列化器
                BinaryFormatter binFormat = new BinaryFormatter();
                //反序列化对象
                instance = (Config)binFormat.Deserialize(fStream);
                //关闭文件
                fStream.Close();
            } catch(FileNotFoundException) {  //文件不存在
                //创建文件
                Stream fStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                //直接关闭文件
                fStream.Close();
                //返回一个新的空对象
                instance = new Config();
                instance.position.X = instance.position.Y = int.MinValue;
            } catch(SerializationException) {  //文件为空
                //返回一个新的空对象
                instance = new Config();
            }
        }
    }
}
