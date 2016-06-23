using SunshinePlayer.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using zlib;

namespace SunshinePlayer {
    /// <summary>
    /// 歌词类
    /// </summary>
    [Serializable]
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
        #region 字体数据
        [NonSerialized]
        private FontFamily fontFamily = new FontFamily("Courier New");
        [NonSerialized]
        private FontStyle fontStyle = FontStyles.Normal;
        [NonSerialized]
        private FontWeight fontWeight = FontWeights.Bold;
        [NonSerialized]
        private FontStretch fontStretch = FontStretches.Normal;
        [NonSerialized]
        private double fontSize = 20;
        [NonSerialized]
        private Brush foreground = Brushes.Black;
        #endregion
        private string filePath = null;
        /// <summary>
        /// 歌词已加载完毕
        /// </summary>
        private bool ready = false;
        /// <summary>
        /// 序列文件保存路径
        /// </summary>
        [NonSerialized]
        public string srcxPath = null;

        /// <summary>
        /// 构造函数 - 直接解析歌词文本
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        /// <param name="src">是否为src精准歌词</param>
        public Lyric(string lrc, bool src) {
            //计算时间偏移
            analyzeOffset(lrc);
            //解析歌词
            if(src) {  //src精准歌词文件
                analyzeSRC(lrc);
            } else {  //lrc普通歌词文件
                analyzeLRC(lrc);
            }
        }
        /// <summary>
        /// 构造函数 - 加载歌词文件，通过扩展名判断src或lrc
        /// </summary>
        /// <param name="path">歌词文件路径</param>
        public Lyric(string path) {
            //文件类型
            Regex ext = new Regex(@".+\.(.+)$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
            MatchCollection mc = ext.Matches(path);
            string name = string.Empty;
            if(mc.Count > 0) {
                name = mc[0].Groups[1].Value.Trim().ToLower();
            }
            if(name != "src" && name != "lrc") {
                throw new Exception("无效的歌词文件格式！");
            }
            //打开文件
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            //读入文件
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, (int)fs.Length);
            //关闭文件
            fs.Flush();
            fs.Close();
            string lrc = Encoding.UTF8.GetString(data);
            //解析歌词
            analyzeOffset(lrc);
            if(name == "src") {
                analyzeSRC(lrc);
            } else if(name == "lrc") {
                analyzeLRC(lrc);
            }
            filePath = path;
        }
        /// <summary>
        /// 构造函数 - 自动搜索歌词（来源：酷狗音乐）
        /// </summary>
        /// <param name="title">音乐标题</param>
        /// <param name="singer">艺术家</param>
        /// <param name="fileHash">文件MD5校验</param>
        /// <param name="time">音乐时长（毫秒）</param>
        /// <param name="savePath">是否保存为文本文件，保存路径</param>
        public Lyric(string title, string singer, string fileHash, int time, string savePath = null) {
            try {
                //查询地址
                string url = string.Format(
                    @"http://mobilecdn.kugou.com/new/app/i/krc.php?cmd=201&keyword=""{0}""-""{1}""&timelength={2}&hash={3}",
                    Helper.urlEncode(singer, "%20"),
                    Helper.urlEncode(title, "%20"),
                    "" + time,
                    fileHash
                );
                string urlDownload = @"http://mobilecdn.kugou.com/new/app/i/krc.php?cmd=201&kid={0}";
                //下载歌词
                using(WebClient wc = new WebClient()) {
                    //查询歌词
                    wc.Encoding = Encoding.UTF8;
                    wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(
                        (object sender, DownloadStringCompletedEventArgs e)=> {
                            if(!e.Cancelled && e.Error == null) {
                                //解析查询结果
                                krc list = Json.parse<krc>(e.Result);
                                if(list.@default == null || list.@default.Length == 0) {
                                    return;
                                }
                                //下载歌词
                                using(WebClient download = new WebClient()) {
                                    download.DownloadDataCompleted += new DownloadDataCompletedEventHandler(
                                        (object s, DownloadDataCompletedEventArgs ed) => {
                                            if(!ed.Cancelled && ed.Error == null) {
                                                //歌词解密
                                                byte[] data = decodeKRC(ed.Result);
                                                if(data == null) {
                                                    return;
                                                }
                                                //保存歌词
                                                if(savePath != null) {
                                                    FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                                                    fs.Write(data, 0, data.Length);
                                                    fs.Flush();
                                                    fs.Close();
                                                    filePath = savePath;
                                                }
                                                string lrc = Encoding.UTF8.GetString(data);
                                                //解析歌词
                                                analyzeOffset(lrc);
                                                analyzeSRC(lrc);
                                            }
                                        }
                                    );
                                    download.DownloadDataAsync(new Uri(string.Format(urlDownload, list.@default)));
                                }
                            }
                        }
                    );
                    //异步执行
                    wc.DownloadStringAsync(new Uri(url));
                }
            } catch(Exception) {
            }
        }
        /// <summary>
        /// 加载序列化歌词
        /// </summary>
        /// <param name="path">文件路径param>
        /// <returns>歌词对象</returns>
        public static Lyric loadSRCX(string path) {
            //文件流
            Stream fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Lyric obj;
            try {
                //二进制反序列化器
                BinaryFormatter binFormat = new BinaryFormatter();
                //反序列化对象
                obj = (Lyric)binFormat.Deserialize(fStream);
            } catch(Exception) {
                return null;
            } finally {
                //关闭文件
                fStream.Close();
            }
            //默认字体
            obj.fontFamily = new FontFamily("Courier New");
            obj.fontStyle = FontStyles.Normal;
            obj.fontWeight = FontWeights.Bold;
            obj.fontStretch = FontStretches.Normal;
            obj.fontSize = 20;
            obj.foreground = Brushes.Black;
            return obj;
        }
        /// <summary>
        /// 序列化存储歌词
        /// </summary>
        /// <param name="path">保存路径</param>
        public static void saveSRCX(string path, Lyric obj) {
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
        /// 歌词加载完毕
        /// </summary>
        public bool Ready { get { return ready; } }
        /// <summary>
        /// 总时长
        /// </summary>
        public int Time { get { return time; } }
        /// <summary>
        /// 时间偏移，正值提前负值延迟
        /// </summary>
        public int Offset {
            get { return offset; }
            set { offset = value; saveOffset(); }
        }
        /// <summary>
        /// 歌词行数
        /// </summary>
        public int Lines { get { return text == null ? 0 : text.Count; } }
        /// <summary>
        /// 取行歌词文本
        /// </summary>
        /// <param name="index">歌词行索引</param>
        /// <returns>行歌词</returns>
        public string GetLine(uint index) {
            //超过最大行
            if(index >= Lines) {
                return string.Empty;
            }
            //拼接当前行
            StringBuilder sb = new StringBuilder();
            foreach(LrcWord word in text[(int)index].content) {
                sb.Append(word.word);
            }
            return sb.ToString();
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
                lrc = string.Empty;
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
            string tt = string.Empty;
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
        /// <summary>
        /// 设置字体 - 用于歌词文本宽度检测
        /// </summary>
        public void setFont(FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize, Brush foreground = null) {
            //设置字体
            this.fontFamily = fontFamily;
            this.fontStyle = fontStyle;
            this.fontWeight = fontWeight;
            this.fontStretch = fontStretch;
            this.fontSize = fontSize;
            this.foreground = foreground == null ? Brushes.Black : foreground;
            //重置计算
            for(int i = 0; i < text.Count; i++) {
                SingleLrc line = text[i];
                line.width = double.MinValue;
                for(int j = 0; j < line.content.Count; j++) {
                    LrcWord word = line.content[j];
                    word.widthBefore = double.MinValue;
                    word.width = double.MinValue;
                    line.content[j] = word;
                }
                text[i] = line;
            }
        }

        /// <summary>
        /// 解析时间偏移
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        private void analyzeOffset(string lrc) {
            //时间偏移
            Regex regOffset = new Regex(@"^\[offset:(-*\d+)\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
            MatchCollection mc = regOffset.Matches(lrc);
            if(mc.Count > 0) {
                offset = int.Parse(mc[0].Groups[1].Value.Trim());
            }
        }
        /// <summary>
        /// 解析LRC普通歌词
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        private void analyzeLRC(string lrc) {
            //行匹配
            Regex regLine = new Regex(@"^((\[\d+:\d+\.\d+\])+)(.*?)$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
            //时间匹配
            Regex regTime = new Regex(@"\[\d+:\d+.\d+\]", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
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
            //歌词全排序
            sort();
            //就绪
            ready = true;
            //保存序列文件
            if(srcxPath != null) {
                saveSRCX(srcxPath, this);
            }
        }
        /// <summary>
        /// 解析SRC精准歌词
        /// </summary>
        /// <param name="lrc">歌词数据文本</param>
        private void analyzeSRC(string lrc) {
            //行匹配
            Regex regLine = new Regex(@"^\[(\d+),(\d+)\](.*?)$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
            //单词匹配
            Regex regWords = new Regex(@"<(\d+),(\d+),\d+>([^<\[]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
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
            //歌词全排序
            sort();
            //就绪
            ready = true;
            //保存序列文件
            if(srcxPath != null) {
                saveSRCX(srcxPath, this);
            }
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
        /// KRC歌词解密
        /// </summary>
        /// <param name="data">歌词加密数据</param>
        /// <returns></returns>
        private byte[] decodeKRC(byte[] data) {
            if(data[0] != 107 || data[1] != 114 || data[2] != 99 || data[3] != 49) {
                return null;
            }
            byte[] key = { 64, 71, 97, 119, 94, 50, 116, 71, 81, 54, 49, 45, 206, 210, 110, 105 };  //密钥
            //解密
            for(int i = 4; i < data.Length; i++) {
                data[i - 4] = (byte)(data[i] ^ key[(i - 4) % 16]);
            }
            //zlib解压
            MemoryStream outfile = new MemoryStream();
            ZOutputStream outZStream = new ZOutputStream(outfile);
            byte[] ret;
            try {
                outZStream.Write(data, 0, data.Length - 4);
                outZStream.Flush();
                outfile.Flush();
                ret = outfile.ToArray();
            } finally {
                outZStream.Close();
            }
            return ret;
        }
        /// <summary>
        /// 将偏移保存到原歌词文件
        /// </summary>
        private void saveOffset() {
            if(filePath != null && File.Exists(filePath)) {
                FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                byte[] textlrc = new byte[fs.Length];
                fs.Read(textlrc, 0, (int)fs.Length);
                string str;
                if(filePath.ToLower().EndsWith("src")) {
                    str = Encoding.UTF8.GetString(textlrc);
                } else {
                    str = Encoding.Default.GetString(textlrc);
                }
                //时间偏移
                Regex regex = new Regex(@"^\[offset:(-*\d+)\]", RegexOptions.Multiline | RegexOptions.CultureInvariant);
                MatchCollection mc = regex.Matches(str);
                if(mc.Count > 0) {
                    str = regex.Replace(str, "[offset:" + offset.ToString() + "]");
                } else {
                    str = "[offset:" + offset.ToString() + "]\r\n" + str;
                }
                fs.Seek(0, SeekOrigin.Begin);
                fs.SetLength(0);
                if(filePath.ToLower().EndsWith("src")) {  //写出修改后的数据
                    fs.Write(Encoding.UTF8.GetBytes(str), 0, Encoding.UTF8.GetByteCount(str));
                } else {
                    fs.Write(Encoding.Default.GetBytes(str), 0, Encoding.Default.GetByteCount(str));
                }
                fs.Flush();
                fs.Close();
            }
            if(srcxPath != null) {
                saveSRCX(srcxPath, this);
            }
        }

        /// <summary>
        /// 单行歌词
        /// </summary>
        [Serializable]
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
        [Serializable]
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
    }
}
