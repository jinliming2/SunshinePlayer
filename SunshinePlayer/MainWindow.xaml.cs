using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SunshinePlayer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// 启动参数
        /// </summary>
        public static String[] Args;
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
        /// 播放列表按钮
        /// </summary>
        private void PlayListButton_Click(object sender, RoutedEventArgs e) {
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
    }
}
