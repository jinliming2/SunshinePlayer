using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace SunshinePlayer {
    /// <summary>
    /// DesktopLyric.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopLyric : Window, IDisposable {
        /// <summary>
        /// 窗口句柄
        /// </summary>
        private IntPtr Handle { get { return new WindowInteropHelper(this).Handle; } }
        /// <summary>
        /// 歌词加载时钟
        /// </summary>
        private BackgroundWorker timer = new BackgroundWorker();
        /// <summary>
        /// 播放对象
        /// </summary>
        private Player player;
        /// <summary>
        /// 配置
        /// </summary>
        private Config config = Config.getInstance();
        /// <summary>
        /// 鼠标进入窗口延迟计算器
        /// </summary>
        private BackgroundWorker moveTimer = new BackgroundWorker();
        /// <summary>
        /// 半透明背景
        /// </summary>
        Brush backgroundBrush = new SolidColorBrush(Color.FromArgb(100, 100, 100, 100));
        #region 歌词数据
        private int indexLyric;
        private string lrcLyric;
        private double lenLyric, progressLyric, valueLyric;
        #endregion
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
                    this.timer.Dispose();
                    this.moveTimer.Dispose();
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
        /// 构造函数
        /// </summary>
        public DesktopLyric() {
            InitializeComponent();
        }
        #region 窗口鼠标穿透相关
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);
        private uint extendedStyle;
        /// <summary>
        /// 资源初始化 - 锁定桌面歌词
        /// </summary>
        private void Window_SourceInitialized(object sender, EventArgs e) {
            extendedStyle = NativeMethods.GetWindowLong(Handle, GWL_EXSTYLE);
            if(config.desktopLyricLocked) {
                NativeMethods.SetWindowLong(Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
        }
        #endregion
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //位置
            if(config.desktopLyricPosition.X == double.MinValue || config.desktopLyricPosition.Y == double.MinValue) {
                move((SystemParameters.WorkArea.Width - this.Width) / 2, SystemParameters.WorkArea.Bottom - this.Height);
            } else {
                move(config.desktopLyricPosition.X, config.desktopLyricPosition.Y);
            }
            //显示颜色
            if(config.lyricAnimation) {
                LrcTop.Background = LrcBottom.Background = new SolidColorBrush(Color.FromArgb(238, 136, 136, 136));
            } else {
                LrcTop.Background = LrcBottom.Background = new SolidColorBrush(Color.FromArgb(255, 0, 255, 197));
            }
            //播放对象
            player = Player.getInstance(MainWindow._this.Handle);
            //时钟设置
            timer.WorkerReportsProgress = true;
            timer.WorkerSupportsCancellation = true;
            timer.ProgressChanged += display;
            timer.DoWork += tick;
            timer.RunWorkerAsync();

            moveTimer.WorkerReportsProgress = true;
            moveTimer.WorkerSupportsCancellation = true;
            moveTimer.ProgressChanged += (object m_s, ProgressChangedEventArgs m_e) => {
                this.Background = backgroundBrush;
            };
            moveTimer.DoWork += (object m_s, DoWorkEventArgs m_e) => {
                Thread.Sleep(500);
                BackgroundWorker worker = m_s as BackgroundWorker;
                if(!worker.CancellationPending) {
                    worker.ReportProgress(0);
                }
            };
            //歌词拖动
            this.MouseLeftButtonDown += delegate { this.MouseMove += dragWindow; };
            this.MouseEnter += delegate {
                if(!moveTimer.IsBusy) {
                    moveTimer.RunWorkerAsync();
                }
            };
            this.MouseLeave += delegate {
                moveTimer.CancelAsync();
                this.Background = null;
            };
        }
        /// <summary>
        /// 移动窗口
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        private void move(double x, double y) {
            if(x < 0) {
                x = 0;
            }
            if(x > SystemParameters.WorkArea.Width - this.Width) {
                x = SystemParameters.WorkArea.Width - this.Width;
            }
            if(y < 0) {
                y = 0;
            }
            if(y > SystemParameters.WorkArea.Bottom - Height) {
                y = SystemParameters.WorkArea.Bottom - Height;
            }
            this.Top = y;
            this.Left = x;
            config.desktopLyricPosition.X = this.Left;
            config.desktopLyricPosition.Y = this.Top;
        }
        /// <summary>
        /// 更新显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void display(object sender, ProgressChangedEventArgs e) {
            double h = 2 * LrcTop.ActualHeight + 10;
            if(this.Height != h) {
                double d = this.Height - h;
                this.Height = h;
                move(this.Left, this.Top + d);
                LrcTop.SetValue(Canvas.TopProperty, this.Height - 2 * LrcTop.ActualHeight - 8);
                LrcBottom.SetValue(Canvas.TopProperty, this.Height - LrcTop.ActualHeight);
            }
            if(e.ProgressPercentage == 1 || MainWindow.lyric.Lines == 0) {
                LrcTop.Tag = "Sunshine Player";
                LrcTop.Value = 0.99;
                LrcBottom.Tag = "";
                LrcTop.UpdateLayout();
                LrcTop.Value = 1;
                LrcTop.UpdateLayout();
                LrcTop.SetValue(Canvas.LeftProperty, (this.Width - LrcTop.ActualWidth) / 2);
                return;
            }
            ProgressBar current, another;
            if(indexLyric % 2 == 0) {
                current = LrcBottom;
                another = LrcTop;
            } else {
                current = LrcTop;
                another = LrcBottom;
            }
            current.Tag = MainWindow.lyric.GetLine((uint)indexLyric);
            current.Value = config.lyricAnimation ? valueLyric : 1;
            current.UpdateLayout();
            if(progressLyric < 0.5) {
                //uint在小于0时溢出，得到最大值
                string tag = MainWindow.lyric.GetLine((uint)indexLyric - 1);
                if((string)another.Tag != tag) {
                    another.Tag = tag;
                    another.Value = 0.99;
                    another.UpdateLayout();
                }
                another.Value = config.lyricAnimation ? 1 : 0;
            } else {
                string tag = MainWindow.lyric.GetLine((uint)indexLyric + 1);
                if((string)another.Tag != tag) {
                    another.Tag = tag;
                    another.Value = 0.01;
                    another.UpdateLayout();
                }
                another.Value = 0;
            }
            another.UpdateLayout();
            if(another.ActualWidth < this.Width) {
                if(another == LrcTop) {
                    another.SetValue(Canvas.LeftProperty, 0d);
                    another.ClearValue(Canvas.RightProperty);
                } else {
                    another.SetValue(Canvas.RightProperty, 0d);
                    another.ClearValue(Canvas.LeftProperty);
                }
            } else {
                if(another.Value == 0) {
                    another.SetValue(Canvas.LeftProperty, 0d);
                    another.ClearValue(Canvas.RightProperty);
                } else {
                    another.SetValue(Canvas.RightProperty, 0d);
                    another.ClearValue(Canvas.LeftProperty);
                }
            }
            if(current.ActualWidth <= this.Width) {
                current.SetValue(Canvas.LeftProperty, 0d);
                current.SetValue(Canvas.RightProperty, 0d);
                LrcBottom.ClearValue(Canvas.LeftProperty);
            } else if(valueLyric * current.ActualWidth < this.Width / 2) {
                current.SetValue(Canvas.LeftProperty, 0d);
                current.ClearValue(Canvas.RightProperty);
            } else if(current.ActualWidth - valueLyric * current.ActualWidth < this.Width / 2) {
                current.SetValue(Canvas.LeftProperty, this.Width - current.ActualWidth);
                current.ClearValue(Canvas.RightProperty);
            } else {
                current.SetValue(Canvas.LeftProperty, this.Width / 2 - valueLyric * current.ActualWidth);
                current.ClearValue(Canvas.RightProperty);
            }
        }
        /// <summary>
        /// 时钟线程
        /// </summary>
        private void tick(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;
            while(true) {
                if(MainWindow.lyric != null) {
                    valueLyric = MainWindow.lyric.FindLrc((int)(player.position * 1000), out indexLyric, out lrcLyric, out lenLyric, out progressLyric);
                    if(double.IsInfinity(valueLyric)) {
                        valueLyric = 0;
                    }
                    worker.ReportProgress(0);
                } else {
                    worker.ReportProgress(1);
                }
                System.Threading.Thread.Sleep(50);
            }
        }
        /// <summary>
        /// 窗口拖动
        /// </summary>
        private void dragWindow(object sender, MouseEventArgs e) {
            if(e.LeftButton == MouseButtonState.Pressed) {
                this.DragMove();
                move(this.Left, this.Top);
            } else {
                this.MouseMove -= dragWindow;
            }
        }
        /// <summary>
        /// 锁定/解锁窗口歌词
        /// </summary>
        public void lockOrUnlock() {
            if(config.desktopLyricLocked) {
                NativeMethods.SetWindowLong(Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            } else {
                NativeMethods.SetWindowLong(Handle, GWL_EXSTYLE, extendedStyle);
            }
        }
    }
}
