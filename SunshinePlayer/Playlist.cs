using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SunshinePlayer {
    /// <summary>
    /// 播放列表类
    /// </summary>
    [Serializable]
    public class Playlist {
        /// <summary>
        /// 音乐信息
        /// </summary>
        public struct Music {
            /// <summary>
            /// 标题
            /// </summary>
            public string title;
            /// <summary>
            /// 艺术家
            /// </summary>
            public string artist;
            /// <summary>
            /// 专辑
            /// </summary>
            public string album;
            /// <summary>
            /// 时长
            /// </summary>
            public string duration;
            /// <summary>
            /// 路径
            /// </summary>
            public string path;
        }

        /// <summary>
        /// 列表名称
        /// </summary>
        public string name = "default";
        /// <summary>
        /// 音乐列表
        /// </summary>
        public ArrayList list = new ArrayList();

        /// <summary>
        /// 序列化保存文件
        /// </summary>
        /// <param name="obj">播放列表对象</param>
        /// <param name="path">文件路径及文件名</param>
        public static void saveFile(ref Playlist obj, string path) {
            //文件流
            Stream fStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            //二进制序列化器
            BinaryFormatter binFormat = new BinaryFormatter();
            //序列化对象
            binFormat.Serialize(fStream, obj);
            //关闭文件
            fStream.Flush();
            fStream.Close();
        }
        /// <summary>
        /// 读取反序列化文件
        /// </summary>
        /// <param name="obj">播放列表对象</param>
        /// <param name="path">文件路径及文件名</param>
        public static void loadFile(out Playlist obj, string path) {
            try {
                //文件流
                Stream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                //二进制反序列化器
                BinaryFormatter binFormat = new BinaryFormatter();
                //反序列化对象
                obj = (Playlist)binFormat.Deserialize(fStream);
                //关闭文件
                fStream.Close();
            } catch(FileNotFoundException) {  //文件不存在
                //创建文件
                Stream fStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                //直接关闭文件
                fStream.Close();
                //返回一个新的空对象
                obj = new Playlist();
            }
        }
    }
}
