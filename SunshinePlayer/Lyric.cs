using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SunshinePlayer {
    /// <summary>
    /// 歌词类
    /// </summary>
    class Lyric {
        /// <summary>
        /// 全部歌词
        /// </summary>
        private List<SingleLrc> text;
        /// <summary>
        /// 总时长
        /// </summary>
        private int time = 0;
        /// <summary>
        /// 时间偏移
        /// </summary>
        private int offset = 0;
        /// <summary>
        /// 原时间偏移
        /// </summary>
        private int offsetOld = 0;
        private FontFamily fontFamily;
        private FontStyle fontStyle;
        private FontWeight fontWeight;
        private FontStretch fontStretch;
        private double fontSize;
        private Brush foreground = Brushes.Black;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        /// <param name="src">是否为src精准歌词</param>
        public Lyric(string lrc, bool src = true) {
            //时间偏移
            Regex regOffset = new Regex(@"^\[offset:(-*\d+)\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
            MatchCollection mc = regOffset.Matches(lrc);
            if(mc.Count > 0) {
                offset = offsetOld = int.Parse(mc[0].Groups[1].Value.Trim());
            }
            if(src) {  //src精准歌词文件
                //行匹配
                Regex regLine = new Regex(@"^\[(\d+),(\d+)\](.*?)$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //单词匹配
                Regex regWords = new Regex(@"<(\d+),(\d+),\d+>([^<\[]+)", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //歌词表
                text = new List<SingleLrc>();
                foreach(Match line in regLine.Matches(lrc)) {
                    //单行歌词
                    SingleLrc sl;
                    sl.width = int.MinValue;
                    sl.time = int.Parse(line.Groups[1].Value.Trim());
                    sl.during = int.Parse(line.Groups[2].Value.Trim());
                    sl.content = new List<LrcWord>();
                    //每个单词
                    foreach(Match word in regWords.Matches(line.Groups[3].Value.Trim())) {
                        LrcWord lw;
                        lw.time = int.Parse(word.Groups[1].Value.Trim());
                        lw.during = int.Parse(word.Groups[2].Value.Trim());
                        lw.word = word.Groups[3].Value;
                        sl.content.Add(lw);
                    }
                    text.Add(sl);
                }
            } else {  //lrc普通歌词文件
                //行匹配
                Regex regLine = new Regex(@"^((\[\d+:\d+\.\d+\])+)(.*?)$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //时间匹配
                Regex regTime = new Regex(@"\[\d+:\d+.\d+\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                //歌词表
                text = new List<SingleLrc>();
                foreach(Match line in regLine.Matches(lrc)) {
                    //歌词文本
                    LrcWord lw;
                    lw.word = line.Groups[3].Value.Trim();
                    lw.time = 0;
                    lw.during = 0;
                    //这一行歌词出现的时间们
                    foreach(Match time in regTime.Matches(line.Groups[1].Value.Trim())) {
                        //单行歌词
                        SingleLrc sl;
                        sl.time = getmm(time.Groups[0].Value.Trim());
                        sl.content = new List<LrcWord>();
                        sl.content.Add(lw);
                        sl.width = int.MinValue;
                        sl.time = 0;
                        sl.during = 0;
                        text.Add(sl);
                    }
                }
                //起始时间排序
                sort();
                //每一句
                SingleLrc[] slArray = text.ToArray();
                for(int i = 0; i < slArray.Length - 1; i++) {
                    //每一句
                    LrcWord[] lwArray = slArray[i].content.ToArray();
                    for(int j = 0; j < lwArray.Length; j++) {
                        slArray[i].during = lwArray[j].during = slArray[i + 1].time - slArray[i].time;
                    }
                    slArray[i].content.Clear();
                    slArray[i].content.AddRange(lwArray);
                }
                //更新歌词数据
                text.Clear();
                text.AddRange(slArray);
            }
            //歌词全排序
            sort();
        }
        /// <summary>
        /// 析构函数
        /// </summary>
        ~Lyric() {
        }
        
        /// <summary>
        /// 总时长
        /// </summary>
        public int Time { get { return time; } }
        /// <summary>
        /// 时间偏移，正值提前负值延迟
        /// </summary>
        public int Offset {
            get { return offset; }
            set { offset = value; }
        }
        /// <summary>
        /// 全部歌词文本
        /// </summary>
        public string Text {
            get {
                StringBuilder sb = new StringBuilder();
                foreach(SingleLrc sl in text) {
                    foreach(LrcWord lw in sl.content) {
                        sb.Append(lw.word);
                    }
                    sb.Append("\n");
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// 歌词行数
        /// </summary>
        public int Lines { get { return text.Count; } }

        /// <summary>
        /// 歌词排序
        /// </summary>
        public void sort() {
            //每行排序
            text.Sort((left, right) => {
                if(left.time > right.time)
                    return 1;
                else if(left.time < right.time)
                    return -1;
                else
                    return 0;
            });
            //每行每词排序
            foreach(SingleLrc sl in text) {
                sl.content.Sort((left, right) => {
                    if(left.time > right.time)
                        return 1;
                    else if(left.time < right.time)
                        return -1;
                    else
                        return 0;
                });
            }
        }
        public void setFont() {
        }

        /// <summary>
        /// 时间转毫秒
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>毫秒</returns>
        private int getmm(string time) {
            Regex r = new Regex(@"\[(\d+):(\d+)\.(\d+)\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
            MatchCollection mc = r.Matches(time);
            if(mc.Count == 0)
                return 0;
            return int.Parse(mc[0].Groups[1].Value.Trim()) * 60000
                + int.Parse(mc[0].Groups[2].Value.Trim()) * 1000
                + int.Parse(mc[0].Groups[3].Value.Trim()) * 10;
        }

        /// <summary>
        /// 单行歌词
        /// </summary>
        private struct SingleLrc {
            /// <summary>
            /// 开始时间
            /// </summary>
            public int time;
            /// <summary>
            /// 持续时间
            /// </summary>
            public int during;
            /// <summary>
            /// 歌词数组
            /// </summary>
            public List<LrcWord> content;
            /// <summary>
            /// 显示宽度
            /// </summary>
            public double width;
        }
        /// <summary>
        /// 每个单词
        /// </summary>
        private struct LrcWord {
            /// <summary>
            /// 开始时间
            /// </summary>
            public int time;
            /// <summary>
            /// 持续时间
            /// </summary>
            public int during;
            /// <summary>
            /// 内容
            /// </summary>
            public string word;
        }
    }
}
