using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SunshinePlayer {
    /// <summary>
    /// 工具类
    /// </summary>
    class Helper {
        /// <summary>
        /// 随机数获取
        /// </summary>
        public static Random random = new Random();
        /// <summary>
        /// 秒数转换为时间
        /// </summary>
        /// <param name="seconds">秒数</param>
        /// <returns>时间</returns>
        public static string Seconds2Time(double seconds) {
            //四舍五入取整
            int second = (int)Math.Round(seconds);
            int H = second / 3600;
            int M = (second % 3600) / 60;
            int S = (second % 3600) % 60;
            return (H > 0 ? H + ":" : "") + M.ToString("00") + ':' + S.ToString("00");
        }
        /// <summary>
        /// 取文件MD5校验
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>MD5</returns>
        public static string getHash(string path) {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(fs);
            fs.Close();
            fs.Dispose();
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < retVal.Length; i++)
                sb.Append(retVal[i].ToString("x2"));
            return sb.ToString();
        }
        /// <summary>
         /// URL转义
         /// </summary>
         /// <param name="URL">待转义URL</param>
         /// <param name="Space">空格转义符</param>
         /// <returns>转义后URL</returns>
        public static string urlEncode(string URL, string Space) {
            return URL.Replace("%", "%25")
            .Replace("+", "%2B")
            .Replace(" ", Space)
            .Replace("\"", "%22")
            .Replace("#", "%23")
            .Replace("&", "%26")
            .Replace("(", "%28")
            .Replace(")", "%29")
            .Replace(",", "%2C")
            .Replace("/", "%2F")
            .Replace(":", "%3A")
            .Replace(";", "%3B")
            .Replace("<", "%3C")
            .Replace("=", "%3D")
            .Replace(">", "%3E")
            .Replace("?", "%3F")
            .Replace("@", "%40")
            .Replace("\\", "%5C")
            .Replace("|", "%7C");
        }
        /// <summary>
        /// 文件名非法字符清理
        /// </summary>
        /// <param name="path">文件名</param>
        /// <returns>清理后的结果</returns>
        public static string pathClear(string path) {
            return path.Replace("\\", " ")
                .Replace("/", " ")
                .Replace(":", " ")
                .Replace("*", " ")
                .Replace("?", " ")
                .Replace("\"", " ")
                .Replace("<", " ")
                .Replace(">", " ")
                .Replace("|", " ");
        }
    }
}
