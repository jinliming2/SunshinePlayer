using Microsoft.Windows.Shell;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SunshinePlayer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, IDisposable {
        /// <summary>
        /// 当前实例
        /// </summary>
        public static MainWindow _this = null;
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
        private Playlist play_list;
        /// <summary>
        /// 歌词对象
        /// </summary>
        public static Lyric lyric = null;
        /// <summary>
        /// 后台歌词处理线程
        /// </summary>
        private BackgroundWorker lyricWorker = new BackgroundWorker();
        /// <summary>
        /// 用于歌词线程访问的player对象
        /// </summary>
        private Player playerForLyric;
        #region 歌词数据
        private bool addedLyric = false;
        private int indexLyric;
        private string lrcLyric;
        private double lenLyric, progressLyric, valueLyric;
        #endregion
        /// <summary>
        /// 默认黑色背景
        /// </summary>
        private SolidColorBrush defaultBackground = new SolidColorBrush(Colors.Black);
        /// <summary>
        /// 歌手图片背景对象
        /// </summary>
        private ImageBrush singerBackground = new ImageBrush();
        /// <summary>
        /// 任务栏预览按钮
        /// </summary>
        private TaskbarItemInfo tii = new TaskbarItemInfo();
        /// <summary>
        /// 桌面歌词窗口
        /// </summary>
        private DesktopLyric desktopLyric = null;

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用
        /// <summary>
        /// 资源释放
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if(!disposedValue) {
                if(disposing) {
                    //托管资源释放
                    this.spectrumWorker.Dispose();
                    this.lyricWorker.Dispose();
                    if(this.desktopLyric != null) {
                        this.desktopLyric.Dispose();
                    }
                }
                //未托管资源释放
                //this.abc = null;
                disposedValue = true;
            }
        }
        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~MainWindow() {
        //   Dispose(false);
        // }
        /// <summary>
        /// 实现IDisposable接口
        /// </summary>
        public void Dispose() {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
        /// <summary>
        /// 构造函数 初始化程序
        /// </summary>
        public MainWindow() {
            _this = this;
            //加载配置
            Config.loadConfig(App.workPath + "\\config.db");
            //窗口初始化事件
            this.Loaded += initialize;
            InitializeComponent();
            //初始化背景图片对象
            singerBackground.Stretch = Stretch.UniformToFill;
            singerBackground.AlignmentX = AlignmentX.Center;
            singerBackground.AlignmentY = AlignmentY.Center;
        }
        /// <summary>
        /// 窗口初始化
        /// </summary>
        private void initialize(object sender, RoutedEventArgs e) {
            //标题栏版本号
            this.Title = this.Title.Replace("{$Version}", App.version);
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

            //事件绑定
            this.CommandBindings.Add(new CommandBinding(MediaCommands.Play, (object m_sender, ExecutedRoutedEventArgs m_e) => {
                PlayButton_Click(m_sender, null);
                m_e.Handled = true;
            }));  //播放
            this.CommandBindings.Add(new CommandBinding(MediaCommands.Pause, (object m_sender, ExecutedRoutedEventArgs m_e) => {
                PauseButton_Click(m_sender, null);
                m_e.Handled = true;
            }));  //暂停
            this.CommandBindings.Add(new CommandBinding(MediaCommands.PreviousTrack, (object m_sender, ExecutedRoutedEventArgs m_e) => {
                LastButton_Click(m_sender, null);
                m_e.Handled = true;
            }));  //上一曲
            this.CommandBindings.Add(new CommandBinding(MediaCommands.NextTrack, (object m_sender, ExecutedRoutedEventArgs m_e) => {
                NextButton_Click(m_sender, null);
                m_e.Handled = true;
            }));  //下一曲

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
            //progressClock.Start();  //仅在播放时启动

            //频谱线程
            playerForSpectrum = Player.getInstance(Handle);
            spectrumWorker.WorkerReportsProgress = true;
            spectrumWorker.WorkerSupportsCancellation = true;
            spectrumWorker.ProgressChanged += spectrum_change;
            spectrumWorker.DoWork += spectrum_caculator;
            spectrumWorker.RunWorkerAsync();

            //歌词线程
            playerForLyric = Player.getInstance(Handle);
            lyricWorker.WorkerReportsProgress = true;
            lyricWorker.WorkerSupportsCancellation = true;
            lyricWorker.ProgressChanged += LyricWorker_ProgressChanged;
            lyricWorker.DoWork += LyricWorker_DoWork;
            lyricWorker.RunWorkerAsync();
        }
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //歌手图片保存路径
            SingerImage.path = App.workPath + "\\singer";
            //加载配置
            Config config = Config.getInstance();
            //窗口位置
            if(
                config.position.X > -Width &&
                config.position.X < SystemParameters.PrimaryScreenWidth &&
                config.position.Y > -Height &&
                config.position.Y < SystemParameters.PrimaryScreenHeight
            ) {
                Left = config.position.X;
                Top = config.position.Y;
            } else {
                config.position.X = Left;
                config.position.Y = Top;
            }
            //播放列表状态
            if(config.playListVisible) {
                PlayList.Visibility = Visibility.Visible;
                shadow.BlurRadius = 20;
            } else {
                PlayList.Visibility = Visibility.Collapsed;
                shadow.BlurRadius = 0;
            }
            //音量
            Player.getInstance(Handle).volumn = config.volumn;
            VolumeBar.Value = config.volumn;
            //加载播放列表
            load_playlist();
            List.SelectedIndex = config.playlistIndex;
            //播放模式
            switch(config.playModel) {
            case Config.PlayModel.SingleCycle:
                Model.SelectedIndex = 2;  //单曲循环
                break;
            case Config.PlayModel.OrderPlay:
                Model.SelectedIndex = 1;  //顺序播放
                break;
            case Config.PlayModel.CirculationList:
                Model.SelectedIndex = 0;  //列表循环
                break;
            case Config.PlayModel.ShufflePlayback:
                Model.SelectedIndex = 3;  //随机播放
                break;
            }
            //启动参数
            if(App.Args.Length > 0) {
                //添加到播放列表
                List.SelectedIndex = addToPlaylist(App.Args);
                PlaylistOpen(sender, null);
            } else if(config.autoPlay) {  //启动自动播放
                PlaylistOpen(sender, null);
            }
            //任务栏预览按钮
            tii.Description = "Sunshine Player";
            tii.ProgressState = TaskbarItemProgressState.None;
            tii.ProgressValue = 0;
            //上一曲按钮
            ThumbButtonInfo tbi_Last = new ThumbButtonInfo();
            tbi_Last.Command = MediaCommands.PreviousTrack;
            tbi_Last.CommandTarget = this;
            tbi_Last.Description = "上一曲";
            tbi_Last.DismissWhenClicked = false;
            tbi_Last.ImageSource = (DrawingImage)Resources["LastButtonImage"];
            tii.ThumbButtonInfos.Add(tbi_Last);
            //播放按钮
            ThumbButtonInfo tbi_Play = new ThumbButtonInfo();
            tbi_Play.Command = MediaCommands.Play;
            tbi_Play.CommandTarget = this;
            tbi_Play.Description = "播放";
            tbi_Play.DismissWhenClicked = false;
            tbi_Play.ImageSource = (DrawingImage)Resources["PlayButtonImage"];
            tii.ThumbButtonInfos.Add(tbi_Play);
            //下一曲按钮
            ThumbButtonInfo tbi_Next = new ThumbButtonInfo();
            tbi_Next.Command = MediaCommands.NextTrack;
            tbi_Next.CommandTarget = this;
            tbi_Next.Description = "下一曲";
            tbi_Next.DismissWhenClicked = false;
            tbi_Next.ImageSource = (DrawingImage)Resources["NextButtonImage"];
            tii.ThumbButtonInfos.Add(tbi_Next);
            TaskbarItemInfo.SetTaskbarItemInfo(this, tii);
            //桌面歌词
            if(config.showDesktopLyric) {
                desktopLyric = new DesktopLyric();
                desktopLyric.Show();
            }
            LrcButton.IsChecked = config.showDesktopLyric;
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
            //保存配置
            Config.getInstance().volumn = (int)Math.Round(VolumeBar.Value);
            Config.saveConfig(App.workPath + "\\config.db");
            //停止频谱
            spectrumWorker.CancelAsync();
            //关闭桌面歌词
            if(desktopLyric != null) {
                desktopLyric.Close();
                desktopLyric.Dispose();
                desktopLyric = null;
            }
            //关闭窗口
            this.Close();
        }
        /// <summary>
        /// 歌词提前
        /// </summary>
        private void lrcAdvance(object sender, RoutedEventArgs e) {
            if(lyric != null) {
                lyric.Offset += 100;
            }
        }
        /// <summary>
        /// 歌词延后
        /// </summary>
        private void lrcDdelay(object sender, RoutedEventArgs e) {
            if(lyric != null) {
                lyric.Offset -= 100;
            }
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
            Config config = Config.getInstance();
            LrcButton.IsChecked = config.showDesktopLyric = !config.showDesktopLyric;
            if(config.showDesktopLyric) {
                //载入桌面歌词窗口
                desktopLyric = new DesktopLyric();
                desktopLyric.Show();
            } else if(desktopLyric != null) {
                //关闭桌面歌词
                desktopLyric.Close();
                desktopLyric.Dispose();
                desktopLyric = null;
            }
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
                //打开第一个文件
                PlaylistOpen(sender, null);
            }
        }
        /// <summary>
        /// 窗口拖动
        /// </summary>
        private void dragWindow(object sender, MouseEventArgs e) {
            Config config = Config.getInstance();
            //鼠标不在进度条、音量条、播放列表上时
            if(e.LeftButton == MouseButtonState.Pressed &&
                !this.Progress.IsMouseOver &&
                !this.Volume.IsMouseOver &&
                !this.List.IsMouseOver
            ) {
                this.DragMove();  //拖动窗口
                config.position.X = Left;
                config.position.Y = Top;
            }
        }
        /// <summary>
        /// 播放列表按钮
        /// </summary>
        private void PlayListButton_Click(object sender, RoutedEventArgs e) {
            Config config = Config.getInstance();
            //显示/隐藏播放列表
            if(config.playListVisible) {
                PlayList.Visibility = Visibility.Collapsed;
                shadow.BlurRadius = 0;
                config.playListVisible = false;
            } else {
                PlayList.Visibility = Visibility.Visible;
                shadow.BlurRadius = 20;
                config.playListVisible = true;
            }
        }
        /// <summary>
        /// 播放按钮
        /// </summary>
        private void PlayButton_Click(object sender, RoutedEventArgs e) {
            Player player = Player.getInstance(Handle);
            if(!player.openedFile) {
                PlaylistOpen(sender, null);
            } else {
                player.play();
                //时钟们
                clocks(true);
            }
            //暂停播放按钮
            PauseButton.Visibility = Visibility.Visible;
            PlayButton.Visibility = Visibility.Hidden;
            tii.ThumbButtonInfos[1].ImageSource = (DrawingImage)Resources["PauseButtonImage"];
            tii.ThumbButtonInfos[1].Command = MediaCommands.Pause;
            //任务栏进度条
            tii.ProgressState = TaskbarItemProgressState.Normal;
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
            tii.ThumbButtonInfos[1].ImageSource = (DrawingImage)Resources["PlayButtonImage"];
            tii.ThumbButtonInfos[1].Command = MediaCommands.Play;
            //任务栏进度条
            tii.ProgressState = TaskbarItemProgressState.Paused;
            //时钟们
            clocks(false);
        }
        /// <summary>
        /// 停止播放
        /// </summary>
        private void stop() {
            Player player = Player.getInstance(Handle);
            player.stop();
            //暂停播放按钮
            PauseButton.Visibility = Visibility.Hidden;
            PlayButton.Visibility = Visibility.Visible;
            tii.ThumbButtonInfos[1].ImageSource = (DrawingImage)Resources["PlayButtonImage"];
            tii.ThumbButtonInfos[1].Command = MediaCommands.Play;
            //任务栏进度条
            tii.ProgressState = TaskbarItemProgressState.None;
            //播放进度
            Progress.Value = 0;
            tii.ProgressValue = 0;
            //播放时间
            time_now.Text = Helper.Seconds2Time(Progress.Value);
            //时钟们
            clocks(false);
        }
        /// <summary>
        /// 启动/停止时钟们
        /// </summary>
        /// <param name="start"></param>
        private void clocks(bool start) {
            if(start) {
                progressClock.Start();
            } else {
                progressClock.Stop();
            }
        }
        /// <summary>
        /// 播放进度时钟
        /// </summary>
        private void ProgressClock(object sender, EventArgs e) {
            Player player = Player.getInstance(Handle);
            Config config = Config.getInstance();
            if(!draggingProgress) {
                //播放进度
                Progress.Value = player.position;
                //播放时间
                time_now.Text = Helper.Seconds2Time(Progress.Value);
                //任务栏进度条
                tii.ProgressValue = Progress.Value / Progress.Maximum;
            }
            if(player.status == Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED) {
                switch(config.playModel) {
                case Config.PlayModel.SingleCycle:  //单曲循环
                    List.SelectedIndex = config.playlistIndex;
                    PlaylistOpen(sender, null);
                    break;
                case Config.PlayModel.OrderPlay:  //顺序播放
                    //停止播放关闭文件
                    stop();
                    if(config.playlistIndex >= List.Items.Count - 1) {
                        //时钟们
                        clocks(false);
                        List.SelectedIndex = config.playlistIndex = 0;
                    } else {
                        List.SelectedIndex = ++config.playlistIndex;
                        PlaylistOpen(sender, null);
                    }
                    break;
                case Config.PlayModel.CirculationList:  //列表循环
                    //停止播放关闭文件
                    stop();
                    if(config.playlistIndex >= List.Items.Count - 1) {
                        List.SelectedIndex = config.playlistIndex = 0;
                    } else {
                        List.SelectedIndex = ++config.playlistIndex;
                    }
                    PlaylistOpen(sender, null);
                    break;
                case Config.PlayModel.ShufflePlayback:  //随机播放
                    //停止播放关闭文件
                    stop();
                    int rand;
                    do {
                        rand = Helper.random.Next(0, List.Items.Count);
                    } while(List.Items.Count > 1 && rand == config.playlistIndex);  //避免重复
                    List.SelectedIndex = rand;
                    PlaylistOpen(sender, null);
                    break;
                }
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
        /// 播放模式改变
        /// </summary>
        private void Model_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Config config = Config.getInstance();
            switch(Model.SelectedIndex) {
            case 0:  //列表循环
                config.playModel = Config.PlayModel.CirculationList;
                break;
            case 1:  //顺序播放
                config.playModel = Config.PlayModel.OrderPlay;
                break;
            case 2:  //单曲循环
                config.playModel = Config.PlayModel.SingleCycle;
                break;
            case 3:  //随机播放
                config.playModel = Config.PlayModel.ShufflePlayback;
                break;
            }
        }
        /// <summary>
        /// 上一曲
        /// </summary>
        private void LastButton_Click(object sender, RoutedEventArgs e) {
            Config config = Config.getInstance();
            //停止播放关闭文件
            stop();
            if(config.playlistIndex <= 0) {
                List.SelectedIndex = config.playlistIndex = List.Items.Count - 1;
            } else {
                List.SelectedIndex = --config.playlistIndex;
            }
            PlaylistOpen(sender, null);
        }
        /// <summary>
        /// 下一曲
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e) {
            Config config = Config.getInstance();
            //停止播放关闭文件
            stop();
            if(config.playlistIndex >= List.Items.Count - 1) {
                List.SelectedIndex = config.playlistIndex = 0;
            } else {
                List.SelectedIndex = ++config.playlistIndex;
            }
            PlaylistOpen(sender, null);
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
            if(List.Items.Count <= 0) {
                openFile(sender, e);
                return;
            }
            Player player = Player.getInstance(Handle);
            Config config = Config.getInstance();
            //打开文件
            if(List.SelectedIndex < 0) {
                List.SelectedIndex = 0;
            }
            string file = (string)((ListBoxItem)List.Items.GetItemAt(List.SelectedIndex)).ToolTip;
            List.ScrollIntoView(List.SelectedItem);
            player.openFile(file);
            if(player.play(true)) {
                //清除背景图片
                this.Background = defaultBackground;
                //记录
                config.playlistIndex = List.SelectedIndex;
                //进度条最大值
                Progress.Maximum = player.length;
                //音乐长度
                time_total.Text = "/" + Helper.Seconds2Time(Progress.Maximum);
                //暂停播放按钮
                PauseButton.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Hidden;
                tii.ThumbButtonInfos[1].ImageSource = (DrawingImage)Resources["PauseButtonImage"];
                tii.ThumbButtonInfos[1].Command = MediaCommands.Pause;
                //任务栏进度条
                tii.ProgressState = TaskbarItemProgressState.Normal;
                //音乐信息
                MusicID3 information = player.information;
                TitleLabel.Content = information.title;
                SingerLabel.Content = information.artist;
                AlbumLabel.Content = information.album;
                //任务栏描述
                tii.Description = information.title + " - " + information.artist;
                //歌词
                loadLyric(information.title, information.artist, Helper.getHash(file), (int)Math.Round(player.length * 1000), file);
                //时钟们
                clocks(true);
                //加载背景图片
                loadImage(information.artist);
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
        /// <summary>
        /// 删除播放列表选中项
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e) {
            //删除已存在项
            ArrayList deleted = new ArrayList();
            ArrayList deletedItems = new ArrayList();
            foreach(ListBoxItem lbi in List.SelectedItems) {
                foreach(Playlist.Music music in play_list.list) {
                    if(music.path == (string)lbi.ToolTip) {
                        deleted.Add(music);
                        break;
                    }
                }
                deletedItems.Add(lbi);
            }
            //删除显示列表
            foreach(ListBoxItem item in deletedItems) {
                List.Items.Remove(item);
            }
            //删除存储列表
            foreach(Playlist.Music music in deleted) {
                play_list.list.Remove(music);
            }
            //保存播放列表
            Playlist.saveFile(ref play_list, App.workPath + "\\Playlist.db");
        }
        /// <summary>
        /// 文件拖入
        /// </summary>
        private void dragEnter(object sender, DragEventArgs e) {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
        }
        /// <summary>
        /// 得到文件
        /// </summary>
        private void drop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if(files.Length == 0)  //没有选择文件
                return;
            //添加到播放列表
            List.SelectedIndex = addToPlaylist(files);
            if(sender != List) {  //不是播放列表得到的
                //打开第一个文件
                PlaylistOpen(sender, null);
            }
            //已处理，防止冒泡事件
            e.Handled = true;
        }
        /// <summary>
        /// 加载歌词到窗口显示
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="artist">艺术家</param>
        /// <param name="hash">文件Hash</param>
        /// <param name="time">音乐时长</param>
        /// <param name="path">文件路径</param>
        private void loadLyric(string title, string artist, string hash, int time, string path) {
            //删除歌词
            lyric = null;
            Lrc.Children.Clear();
            addedLyric = false;
            //序列化路径不存在
            if(!Directory.Exists(App.workPath + "\\lyrics")) {
                Directory.CreateDirectory(App.workPath + "\\lyrics");
            }
            string t = Helper.pathClear(title);
            string a = Helper.pathClear(artist);
            //查找歌词文件
            if(File.Exists(App.workPath + "\\lyrics\\" + a + " - " + t + ".srcx")) {
                lyric = Lyric.loadSRCX(App.workPath + "\\lyrics\\" + a + " - " + t + ".srcx");
                if(lyric != null) {
                    return;
                }
            }
            if(File.Exists(App.workPath + "\\lyrics\\" + a + " - " + t + ".src")) {
                lyric = new Lyric(App.workPath + "\\lyrics\\" + a + " - " + t + ".src");
                //序列化保存
                Lyric.saveSRCX(App.workPath + "\\lyrics\\" + a + " - " + t + ".srcx", lyric);
            } else if(File.Exists(App.workPath + "\\lyrics\\" + a + " - " + t + ".lrc")) {
                lyric = new Lyric(App.workPath + "\\lyrics\\" + a + " - " + t + ".lrc");
                //序列化保存
                Lyric.saveSRCX(App.workPath + "\\lyrics\\" + a + " - " + t + ".srcx", lyric);
            } else if(File.Exists(path.Remove(path.LastIndexOf('.') + 1) + "src")) {
                lyric = new Lyric(path.Remove(path.LastIndexOf('.') + 1) + "src");
                //序列化保存
                Lyric.saveSRCX(App.workPath + "\\lyrics\\" + a + " - " + t + ".srcx", lyric);
            } else if(File.Exists(path.Remove(path.LastIndexOf('.') + 1) + "lrc")) {
                lyric = new Lyric(path.Remove(path.LastIndexOf('.') + 1) + "lrc");
                //序列化保存
                Lyric.saveSRCX(App.workPath + "\\lyrics\\" + a + " - " + t + ".srcx", lyric);
            } else {
                lyric = new Lyric(title, artist, hash, time, App.workPath + "\\lyrics\\" + a + " - " + t + ".src");
            }
            //序列化保存
            lyric.srcxPath = App.workPath + "\\lyrics\\" + a + " - " + t + ".srcx";
        }
        /// <summary>
        /// 歌词处理线程
        /// </summary>
        private void LyricWorker_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;
            Player player = playerForLyric;
            while(true) {
                if(lyric != null) {
                    if(addedLyric) {
                        valueLyric = lyric.FindLrc((int)(player.position * 1000), out indexLyric, out lrcLyric, out lenLyric, out progressLyric);
                    }
                    worker.ReportProgress(0);
                }
                System.Threading.Thread.Sleep(50);
            }
        }
        /// <summary>
        /// 歌词变化处理
        /// </summary>
        private void LyricWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            Config config = Config.getInstance();
            if(!addedLyric) {
                if(!lyric.Ready) {
                    Lrc.Children.Clear();
                    ProgressBar pb = new ProgressBar();
                    pb.SetResourceReference(StyleProperty, "LyricText");
                    pb.Value = 1;
                    pb.Tag = "正在加载歌词";
                    pb.Foreground = new SolidColorBrush(Colors.Yellow);
                    pb.Background = new SolidColorBrush(Colors.White);
                    pb.HorizontalAlignment = HorizontalAlignment.Right;
                    pb.Maximum = 1;
                    Lrc.Children.Add(pb);
                } else if(lyric.Lines == 0) {
                    Lrc.Children.Clear();
                    ProgressBar pb = new ProgressBar();
                    pb.SetResourceReference(StyleProperty, "LyricText");
                    pb.Value = 0;
                    pb.Tag = "无歌词";
                    pb.Foreground = new SolidColorBrush(Colors.Yellow);
                    pb.Background = new SolidColorBrush(Colors.White);
                    pb.HorizontalAlignment = HorizontalAlignment.Right;
                    pb.Maximum = 1;
                    Lrc.Children.Add(pb);
                    addedLyric = true;
                } else {
                    Lrc.Children.Clear();
                    for(int i = 0; i < lyric.Lines; i++) {
                        ProgressBar pb = new ProgressBar();
                        pb.SetResourceReference(StyleProperty, "LyricText");
                        pb.Value = 0;
                        pb.Tag = lyric.GetLine((uint)i);
                        pb.Foreground = new SolidColorBrush(Colors.Yellow);
                        pb.Background = new SolidColorBrush(Colors.White);
                        pb.HorizontalAlignment = HorizontalAlignment.Right;
                        pb.Maximum = 1;
                        Lrc.Children.Add(pb);
                    }
                    addedLyric = true;
                }
            } else {
                foreach(ProgressBar p in Lrc.Children) {
                    p.Value = 0;
                }
                ProgressBar pb = (ProgressBar)Lrc.Children[indexLyric];
                pb.Value = config.lyricAnimation ? valueLyric : 1;
                if(indexLyric > 3) {
                    Lrc.SetValue(Canvas.TopProperty, -(indexLyric - 4) * 68 / 3 - (config.lyricAnimation ? progressLyric * 68 / 3 : 0));
                } else {
                    Lrc.SetValue(Canvas.TopProperty, 0.0);
                }
            }
        }
        /// <summary>
        /// 加载歌手图片到窗口显示
        /// </summary>
        /// <param name="artist">歌手</param>
        private void loadImage(string artist) {
            SingerImage.getImage(artist, ++SingerImage.getid, (string filepath) => {
                singerBackground.ImageSource = new BitmapImage(new Uri(filepath));
                this.Background = singerBackground;
            });
        }
    }
}
