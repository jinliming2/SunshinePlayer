using System;

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
    }
}
