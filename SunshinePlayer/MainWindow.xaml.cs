using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SunshinePlayer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// 歌词开关按钮
        /// </summary>
        private static CheckBox LrcButton;
        /// <summary>
        /// 打开文件按钮
        /// </summary>
        private static Button OpenButton;
        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr Handle { get { return new WindowInteropHelper(this).Handle; } }
        /// <summary>
        /// 播放列表按钮效果
        /// </summary>
        private DropShadowEffect shadow = new DropShadowEffect();
        /// <summary>
        /// 播放进度时钟
        /// </summary>
        private DispatcherTimer progressClock = new DispatcherTimer();
        /// <summary>
        /// 总时间标签，当前时间标签
        /// </summary>
        private Run time_total = new Run("/00:00"), time_now = new Run("00:00");
        /// <summary>
        /// 进度条拖动状态
        /// </summary>
        private bool draggingProgress = false;
        /// <summary>
        /// 进度时间颜色
        /// </summary>
        private Brush progressColor = new SolidColorBrush(Colors.White);
        /// <summary>
        /// 进度时间拖动时颜色
        /// </summary>
        private Brush draggingProgressColor = new SolidColorBrush(Colors.Orange);
        /// <summary>
        /// 后台频谱操作线程
        /// </summary>
        private BackgroundWorker spectrumWorker = new BackgroundWorker();
        /// <summary>
        /// 频谱线、频谱条
        /// </summary>
        private Rectangle[] spectrum_t = new Rectangle[42], spectrum_x = new Rectangle[42];
        /// <summary>
        /// 频谱数据
        /// </summary>
        private int[] spectrum_position_t = new int[42], spectrum_position_x = new int[42];
        /// <summary>
        /// 频谱线下落速度
        /// </summary>
        private int[] spectrum_fall_rate = new int[42];
        /// <summary>
        /// 用于频谱线程访问的player对象
        /// </summary>
        private Player playerForSpectrum;
        /// <summary>
        /// 播放列表
        /// </summary>
        public Playlist play_list;
        /// <summary>
        /// 构造函数 初始化程序
        /// </summary>
        public MainWindow() {
            //窗口初始化事件
            this.Loaded += initialize;
            InitializeComponent();
        }
        /// <summary>
        /// 窗口初始化
        /// </summary>
        private void initialize(object sender, RoutedEventArgs e) {
            //窗口样式模板
            ControlTemplate baseWindowTemplate = (ControlTemplate)this.Resources["mainWindowTemplate"];
            //关闭按钮
            Button closeButton = (Button)baseWindowTemplate.FindName("closeButton", this);
            closeButton.Click += close;  //窗口关闭
            //最小化按钮
            Button minimizeButton = (Button)baseWindowTemplate.FindName("minimizeButton", this);
            minimizeButton.Click += minimize;  //窗口最小化
            //设置按钮
            Button settingButton = (Button)baseWindowTemplate.FindName("settingButton", this);
            settingButton.Click += setting;  //设置
            //歌词提前按钮
            Button advanceButton = (Button)baseWindowTemplate.FindName("advanceButton", this);
            advanceButton.Click += lrcAdvance;  //歌词提前
            //歌词延后按钮
            Button delayButton = (Button)baseWindowTemplate.FindName("delayButton", this);
            delayButton.Click += lrcDdelay;  //歌词延后
            //桌面歌词开关按钮
            LrcButton = (CheckBox)baseWindowTemplate.FindName("LrcButton", this);
            LrcButton.Click += lrcSwitch;  //切换桌面歌词显示状态
            //打开文件按钮
            OpenButton = (Button)baseWindowTemplate.FindName("OpenButton", this);
            OpenButton.Click += openFile;  //打开文件
            //频谱
            for(int i = 1; i <= 42; i++) {
                spectrum_t[i - 1] = (Rectangle)Spectrum.FindName("ppt" + i);
                spectrum_x[i - 1] = (Rectangle)Spectrum.FindName("ppx" + i);
            }

            //窗口拖动
            this.MouseLeftButtonDown += delegate { this.MouseMove += dragWindow; };
            this.MouseUp += delegate { this.MouseMove -= dragWindow; };

            //进度条拖动
            Progress.PreviewMouseDown += delegate {
                draggingProgress = true;
                Progress.ValueChanged += progress_valueChange;
                //时间文本颜色
                time_now.Foreground = draggingProgressColor;
            };
            Progress.PreviewMouseUp += delegate {
                draggingProgress = false;
                Progress.ValueChanged -= progress_valueChange;
                //时间文本颜色
                time_now.Foreground = progressColor;
            };

            //播放列表按钮效果
            shadow.ShadowDepth = 0;
            shadow.Color = Colors.White;
            shadow.Opacity = 1;
            PlayListButton.Effect = shadow;

            //时间显示
            TimeLabel.Inlines.Clear();
            TimeLabel.Inlines.Add(time_now);
            TimeLabel.Inlines.Add(time_total);

            //时钟设置
            progressClock.Interval = new TimeSpan(0, 0, 0, 0, 250);
            progressClock.Tick += ProgressClock;
            progressClock.Start();

            //频谱线程
            playerForSpectrum = Player.getInstance(Handle);
            spectrumWorker.WorkerReportsProgress = true;
            spectrumWorker.WorkerSupportsCancellation = true;
            spectrumWorker.ProgressChanged += spectrum_change;
            spectrumWorker.DoWork += spectrum_caculator;
            spectrumWorker.RunWorkerAsync();
        }
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //播放列表状态
            if(Config.playListVisible) {
                PlayList.Visibility = Visibility.Visible;
                shadow.BlurRadius = 20;
            } else {
                PlayList.Visibility = Visibility.Collapsed;
                shadow.BlurRadius = 0;
            }
            //加载播放列表
            load_playlist();
        }
        /// <summary>
        /// 窗口最小化
        /// </summary>
        private void minimize(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }
        /// <summary>
        /// 窗口关闭
        /// </summary>
        private void close(object sender, RoutedEventArgs e) {
            spectrumWorker.CancelAsync();
            this.Close();
        }
        /// <summary>
        /// 歌词提前
        /// </summary>
        private void lrcAdvance(object sender, RoutedEventArgs e) {
        }
        /// <summary>
        /// 歌词延后
        /// </summary>
        private void lrcDdelay(object sender, RoutedEventArgs e) {
        }
        /// <summary>
        /// 设置
        /// </summary>
        private void setting(object sender, RoutedEventArgs e) {
        }
        /// <summary>
        /// 桌面歌词开关切换
        /// </summary>
        private void lrcSwitch(object sender, RoutedEventArgs e) {
        }
        /// <summary>
        /// 打开文件
        /// </summary>
        private void openFile(object sender, RoutedEventArgs e) {
            //打开文件对话框
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            //标题
            ofd.Title = "打开音乐";
            //检查文件必须存在
            ofd.CheckFileExists = true;
            //允许多选（所有文件加入播放列表，自动播放第一首）
            ofd.Multiselect = true;
            //快捷方式返回引用的文件
            ofd.DereferenceLinks = true;
            //文件筛选过滤器
            ofd.Filter = "音乐文件|*.mp3;*.mp2;*.mp1;*.ogg;*.wav;*.aiff"
                + "|MP3|*.mp3"
                + "|OGG|*.ogg"
                + "|WAV|*.wav"
                + "|AIFF|*.aiff"
                + "|MP2|*.mp2"
                + "|MP1|*.mp1"
                + "|所有文件|*";
            //文件筛选索引
            ofd.FilterIndex = 1;
            //打开文件
            if(ofd.ShowDialog() == true) {
                //文件列表
                string[] files = ofd.FileNames;
                //添加到播放列表
                List.SelectedIndex = addToPlaylist(files);
                List.ScrollIntoView(List.SelectedItem);
                //打开第一个文件
                PlaylistOpen(sender, null);
            }
        }
        /// <summary>
        /// 窗口拖动
        /// </summary>
        private void dragWindow(object sender, MouseEventArgs e) {
            //鼠标不在进度条、音量条、播放列表上时
            if(e.LeftButton == MouseButtonState.Pressed &&
                !this.Progress.IsMouseOver &&
                !this.Volume.IsMouseOver &&
                !this.List.IsMouseOver
            ) {
                this.DragMove();  //拖动窗口
            }
        }
        /// <summary>
        /// 播放列表按钮
        /// </summary>
        private void PlayListButton_Click(object sender, RoutedEventArgs e) {
            //显示/隐藏播放列表
            if(Config.playListVisible) {
                PlayList.Visibility = Visibility.Collapsed;
                shadow.BlurRadius = 0;
                Config.playListVisible = false;
            } else {
                PlayList.Visibility = Visibility.Visible;
                shadow.BlurRadius = 20;
                Config.playListVisible = true;
            }
        }
        /// <summary>
        /// 播放按钮
        /// </summary>
        private void PlayButton_Click(object sender, RoutedEventArgs e) {
            Player player = Player.getInstance(Handle);
            if(!player.openedFile) {
                openFile(sender, e);
            } else {
                player.play();
            }
            //暂停播放按钮
            PauseButton.Visibility = Visibility.Visible;
            PlayButton.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// 暂停按钮
        /// </summary>
        private void PauseButton_Click(object sender, RoutedEventArgs e) {
            Player player = Player.getInstance(Handle);
            player.pause();
            //暂停播放按钮
            PauseButton.Visibility = Visibility.Hidden;
            PlayButton.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 播放进度时钟
        /// </summary>
        private void ProgressClock(object sender, EventArgs e) {
            Player player = Player.getInstance(Handle);
            if(!draggingProgress) {
                //播放进度
                Progress.Value = player.position;
                //播放时间
                time_now.Text = Helper.Seconds2Time(Progress.Value);
            }
        }
        /// <summary>
        /// 拖动音量条
        /// </summary>
        private void VolumeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            Player player = Player.getInstance(Handle);
            //调整音量
            player.volumn = (int)Math.Round(e.NewValue);
            //显示静音按钮
            VolumeButton.Visibility = Visibility.Hidden;
            MuteButton.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 静音
        /// </summary>
        private void MuteButton_Click(object sender, RoutedEventArgs e) {
            //静音并显示取消静音按钮
            MuteButton.Visibility = Visibility.Hidden;
            Player.getInstance(Handle).mute = true;
            VolumeButton.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 音量
        /// </summary>
        private void VolumeButton_Click(object sender, RoutedEventArgs e) {
            //取消静音并显示静音按钮
            VolumeButton.Visibility = Visibility.Hidden;
            Player.getInstance(Handle).mute = false;
            MuteButton.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 拖动进度条
        /// </summary>
        private void progress_valueChange(object sender, RoutedPropertyChangedEventArgs<double> e) {
            //拖动进度条
            if(draggingProgress) {
                Player player = Player.getInstance(Handle);
                //改变进度
                player.position = e.NewValue;
                //时间显示
                time_now.Text = Helper.Seconds2Time(e.NewValue);
            }
        }
        /// <summary>
        /// 更新频谱显示
        /// </summary>
        private void spectrum_change(object sender, ProgressChangedEventArgs e) {
            for(int i = 0; i < 42; i++) {
                spectrum_t[i].Height = spectrum_position_t[i];
                Canvas.SetBottom(spectrum_x[i], spectrum_position_x[i]);
            }
        }
        /// <summary>
        /// 频谱计算
        /// </summary>
        private void spectrum_caculator(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;
            Player player = playerForSpectrum;
            //频谱显示最大高度
            int max_height = 295;
            while(true) {
                if(worker.CancellationPending) {
                    break;
                }
                //频谱数据
                float[] spectrum = player.spectrum;
                for(int i = 0; i < 42; i++) {
                    //忽略最前面三条，后面每隔一条取一条
                    int id = i * 2 + 3;
                    //计算高度（以0.1为最大值，但是实际在高音时可达到0.4左右）
                    float height = spectrum[id] * max_height * 10;
                    if(height > max_height) {
                        height = max_height;
                    }
                    //上升
                    if(height > spectrum_position_t[i]) {
                        spectrum_position_t[i] = (int)height;
                    } else if(spectrum_position_t[i] > 5) {
                        //大幅下降
                        spectrum_position_t[i] -= 5;
                        if(spectrum_position_t[i] < height) {
                            spectrum_position_t[i] = (int)height;
                        }
                    } else if(spectrum_position_t[i] > 0) {
                        //小幅下降
                        spectrum_position_t[i]--;
                    }
                    //频谱线下落速度
                    if(spectrum_fall_rate[i] <= 10) {
                        spectrum_fall_rate[i]++;
                    }
                    //频谱线下落
                    if(spectrum_fall_rate[i] > 0) {
                        spectrum_position_x[i] -= spectrum_fall_rate[i];
                    }
                    if(spectrum_position_x[i] < spectrum_position_t[i] + 1) {
                        spectrum_position_x[i] = spectrum_position_t[i] + 1;
                        //置为-10， 延迟下落
                        spectrum_fall_rate[i] = -10;
                    }
                }
                //更新显示
                worker.ReportProgress(0);
                //延迟获取
                System.Threading.Thread.Sleep(35);
            }
        }
        /// <summary>
        /// 加载播放列表
        /// </summary>
        private void load_playlist() {
            Playlist.loadFile(out play_list, App.workPath + "\\Playlist.db");
            foreach(Playlist.Music music in play_list.list) {
                TextBlock textblock = new TextBlock();
                textblock.TextTrimming = TextTrimming.WordEllipsis;
                //歌曲名
                Run title = new Run(music.title);
                title.FontSize = 20;
                title.FontWeight = FontWeights.Bold;
                textblock.Inlines.Add(title);
                //时长
                Run duration = new Run(music.duration == "" ? "" : (" - " + music.duration));
                duration.FontSize = 16;
                duration.FontStyle = FontStyles.Italic;
                textblock.Inlines.Add(duration);
                textblock.Inlines.Add(new LineBreak());
                //艺术家
                Run artist = new Run(music.artist == "" ? music.path : music.artist);
                artist.FontSize = 14;
                textblock.Inlines.Add(artist);
                //专辑
                Run album = new Run(music.album == "" ? "" : (" - " + music.album));
                album.FontSize = 14;
                textblock.Inlines.Add(album);
                //宽度
                textblock.Width = 725;
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;
                stackPanel.Children.Add(textblock);
                ListBoxItem item = new ListBoxItem();
                item.Content = stackPanel;
                item.ToolTip = music.path;
                item.IsTabStop = false;
                item.MouseDoubleClick += PlaylistOpen;
                List.Items.Add(item);
            }
        }
        /// <summary>
        /// 从播放列表打开文件
        /// </summary>
        private void PlaylistOpen(object sender, MouseButtonEventArgs e) {
            Player player = Player.getInstance(Handle);
            //打开文件
            player.openFile((string)((ListBoxItem)List.Items.GetItemAt(List.SelectedIndex)).ToolTip);
            if(player.play(true)) {
                //进度条最大值
                Progress.Maximum = player.length;
                //音乐长度
                time_total.Text = "/" + Helper.Seconds2Time(Progress.Maximum);
                //暂停播放按钮
                PauseButton.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Hidden;
                //音乐信息
                MusicID3 information = player.information;
                TitleLabel.Content = information.title;
                SingerLabel.Content = information.artist;
                AlbumLabel.Content = information.album;
            } else {
                Error error = player.error;
                MessageBox.Show(error.content, error.title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 向播放列表插入文件
        /// </summary>
        /// <param name="files">文件路径数组</param>
        /// <returns>成功返回插入的第一个文件在列表中的位置，失败返回播放列表最后一个文件的位置（可能为-1）</returns>
        private int addToPlaylist(string[] files) {
            int lastid = List.Items.Count - 2, count = -1;
            foreach(string file in files) {
                //检验音乐文件合法性并获取音乐信息
                MusicID3? info = Player.getInformation(file);
                if(info == null) {
                    //音乐文件无法打开
                    continue;
                }
                //删除已存在项
                ArrayList deleted = new ArrayList();
                foreach(ListBoxItem lbi in List.Items) {
                    if(((string)lbi.ToolTip) == file) {
                        deleted.Add(lbi);
                    }
                }
                foreach(ListBoxItem lbi in deleted) {
                    List.Items.Remove(lbi);
                }
                deleted.Clear();
                foreach(Playlist.Music music in play_list.list) {
                    if(music.path == file) {
                        deleted.Add(music);
                    }
                }
                foreach(Playlist.Music music in deleted) {
                    play_list.list.Remove(music);
                }
                //添加到列表
                TextBlock textblock = new TextBlock();
                textblock.TextTrimming = TextTrimming.WordEllipsis;
                //歌曲名
                Run title = new Run(info.Value.title);
                title.FontSize = 20;
                title.FontWeight = FontWeights.Bold;
                textblock.Inlines.Add(title);
                //时长
                Run duration = new Run(" - " + info.Value.duration);
                duration.FontSize = 16;
                duration.FontStyle = FontStyles.Italic;
                textblock.Inlines.Add(duration);
                textblock.Inlines.Add(new LineBreak());
                //艺术家
                Run artist = new Run(info.Value.artist == "" ? file : info.Value.artist);
                artist.FontSize = 14;
                textblock.Inlines.Add(artist);
                //专辑
                Run album = new Run(info.Value.album == "" ? "" : (" - " + info.Value.album));
                album.FontSize = 14;
                textblock.Inlines.Add(album);
                //宽度
                textblock.Width = 725;
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;
                stackPanel.Children.Add(textblock);
                ListBoxItem item = new ListBoxItem();
                item.Content = stackPanel;
                item.ToolTip = file;
                item.IsTabStop = false;
                item.MouseDoubleClick += PlaylistOpen;
                //统计
                lastid = List.Items.Add(item);
                count++;
                //添加到列表
                play_list.list.Add(new Playlist.Music {
                    title = info.Value.title,
                    artist = info.Value.artist,
                    album = info.Value.album,
                    duration = info.Value.duration,
                    path = file
                });
            }
            //保存播放列表
            Playlist.saveFile(ref play_list, App.workPath + "\\Playlist.db");
            //返回插入的第一条位置id
            return lastid - count;
        }
    }
}
