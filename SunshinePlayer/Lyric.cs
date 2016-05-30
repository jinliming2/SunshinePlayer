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
                    sl.width = double.MinValue;
                    sl.time = int.Parse(line.Groups[1].Value.Trim());
                    sl.during = int.Parse(line.Groups[2].Value.Trim());
                    sl.content = new List<LrcWord>();
                    //每个单词
                    foreach(Match word in regWords.Matches(line.Groups[3].Value.Trim())) {
                        LrcWord lw;
                        lw.time = int.Parse(word.Groups[1].Value.Trim());
                        lw.during = int.Parse(word.Groups[2].Value.Trim());
                        lw.word = word.Groups[3].Value;
                        lw.width = double.MinValue;
                        lw.widthBefore = double.MinValue;
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
                    lw.width = double.MinValue;
                    lw.widthBefore = double.MinValue;
                    //这一行歌词出现的时间们
                    foreach(Match time in regTime.Matches(line.Groups[1].Value.Trim())) {
                        //单行歌词
                        SingleLrc sl;
                        sl.time = getmm(time.Groups[0].Value.Trim());
                        sl.content = new List<LrcWord>();
                        sl.content.Add(lw);
                        sl.width = double.MinValue;
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
            //时间偏移被修改
            if(offset != offsetOld) {
                //TODO: 保存修改到歌词文件
            }
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
        /// 计算文本在当前字体显示的宽度
        /// </summary>
        /// <param name="text">待计算的文本</param>
        /// <returns>宽度</returns>
        private double getTextWidth(string text) {
            return new FormattedText(text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                    fontSize,
                    foreground).Width;
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
            /// <summary>
            /// 当前词之前显示宽度
            /// </summary>
            public double widthBefore;
            /// <summary>
            /// 当前词显示宽度
            /// </summary>
            public double width;
        }
        
        /// <summary>
        /// 查询当前时间所对应的那一句歌词
        /// </summary>
        /// <param name="time">时间（毫秒）</param>
        /// <param name="index">行序号</param>
        /// <param name="lrc">歌词</param>
        /// <param name="len">当前句全长</param>
        /// <param name="progress">已过当前句时间进度</param>
        /// <param name="label">测量宽度使用的Label</param>
        /// <returns>当前歌词已经过的进度</returns>
        public double FindLrc(int time, out int index, out string lrc, out double len, out double progress) {
            if(Lines == 0) {  //没有歌词
                lrc = "无歌词";
                index = 0;
                len = 0;
                progress = 0;
                return 0;
            }
            //偏移
            time += offset;
            //寻找当前所在行
            for(index = 0; index < text.Count; index++) {
                if(
                    text[index].time + text[index].during >= time ||  //处于当前行结尾之前
                    index == text.Count - 1 ||  //已是寻找的最后一句
                    text[index + 1].time > time  //还未到下一句
                ) {
                    break;
                }
            }
            if(index == text.Count) {  //查找失败
                lrc = "";
                index = Lines;
                len = 0;
                progress = 1;
                return 0;
            }
            //找到当前行
            StringBuilder sb = new StringBuilder();
            foreach(LrcWord word in text[index].content) {
                sb.Append(word.word);
            }
            lrc = sb.ToString();
            //显示宽度
            if(text[index].width == double.MinValue) {  //还未计算显示宽度
                SingleLrc tmp = text[index];
                //计算显示宽度
                tmp.width = getTextWidth(lrc);
                text[index] = tmp;
            }
            len = text[index].width;
            if(text[index].time > time || len == 0) {  //还未到当前行
                progress = 0;
                return 0;
            }
            //已到当前行
            //当前句已过时间
            time -= text[index].time;
            //时间进度
            progress = (double)time / text[index].during;
            string tt = "";
            //寻找当前所在词
            for(int n = 0; n < text[index].content.Count; n++) {
                if(text[index].content[n].time + text[index].content[n].during >= time) {  //处于当前词结尾之前
                    //当前词已过时间
                    time -= text[index].content[n].time;
                    //之前词显示宽度
                    if(text[index].content[n].widthBefore == double.MinValue) {
                        //取出
                        SingleLrc tmpS = text[index];
                        LrcWord tmpL = tmpS.content[n];
                        //计算
                        tmpL.widthBefore = getTextWidth(tt);
                        //放回
                        tmpS.content[n] = tmpL;
                        text[index] = tmpS;
                    }
                    //当前词显示宽度
                    if(text[index].content[n].width == double.MinValue) {
                        //取出
                        SingleLrc tmpS = text[index];
                        LrcWord tmpL = tmpS.content[n];
                        //计算
                        tmpL.width = getTextWidth(text[index].content[n].word);
                        //放回
                        tmpS.content[n] = tmpL;
                        text[index] = tmpS;
                    }
                    //当前词已过百分比
                    double p = text[index].content[n].width * time / text[index].content[n].during;
                    p += text[index].content[n].widthBefore;
                    //当前句已过显示百分比
                    return p / len;
                }
                //已过当前词
                tt += text[index].content[n].word;
            }
            //已过当前句
            progress = 1;
            return 1;
        }

        /// <summary>
        /// 取行歌词文本
        /// </summary>
        /// <param name="index">歌词行索引</param>
        /// <returns>行歌词</returns>
        public string GetLine(uint index) {
            if(index >= Lines)
                return "";
            string lrc = "";
            for(int i = 0; i < text[(int)index].content.Count; i++)
                lrc += text[(int)index].content[i].word;
            return lrc;
        }
    }
}
