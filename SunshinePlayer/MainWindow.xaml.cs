using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace SunshinePlayer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// 启动参数
        /// </summary>
        public static string[] Args;
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

            //窗口拖动
            this.MouseLeftButtonDown += delegate { this.MouseMove += dragWindow; };
            this.MouseUp += delegate { this.MouseMove -= dragWindow; };

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
                //打开第一个文件
                Player player = Player.getInstance(Handle);
                player.openFile(files[0]);
                if(player.play(true)) {
                    //进度条最大值
                    Progress.Maximum = player.length;
                    //音乐长度
                    time_total.Text = "/" + Helper.Seconds2Time(Progress.Maximum);
                    //暂停播放按钮
                    PauseButton.Visibility = Visibility.Visible;
                    PlayButton.Visibility = Visibility.Hidden;
                } else {
                    Error error = player.error;
                    MessageBox.Show(error.content, error.title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private void ProgressClock(object sender, EventArgs e) {
            Player player = Player.getInstance(Handle);
            //播放进度
            Progress.Value = player.position;
            //播放时间
            time_now.Text = Helper.Seconds2Time(Progress.Value);
        }
    }
}
