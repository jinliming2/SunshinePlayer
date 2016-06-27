﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SunshinePlayer {
    /// <summary>
    /// DesktopLyric.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopLyric : Window, IDisposable {
        /// <summary>
        /// 歌词加载时钟
        /// </summary>
        private BackgroundWorker timer = new BackgroundWorker();
        /// <summary>
        /// 播放对象
        /// </summary>
        private Player player;
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
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Config config = Config.getInstance();
            //位置
            if(config.desktopLyricPosition.X == double.MinValue || config.desktopLyricPosition.Y == double.MinValue) {
                move((SystemParameters.WorkArea.Width - this.Width) / 2, SystemParameters.WorkArea.Bottom - this.Height);
                config.desktopLyricPosition.Y = this.Top;
                config.desktopLyricPosition.X = this.Left;
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
        }
        /// <summary>
        /// 更新显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void display(object sender, ProgressChangedEventArgs e) {
            LrcTop.SetValue(Canvas.TopProperty, this.Height - 2 * LrcTop.ActualHeight - 8);
            LrcBottom.SetValue(Canvas.TopProperty, this.Height - LrcTop.ActualHeight);
            if(e.ProgressPercentage == 1 || MainWindow.lyric.Lines == 0) {
                LrcTop.Tag = "Sunshine Player";
                LrcTop.Value = 1;
                LrcBottom.Tag = "";
                LrcTop.UpdateLayout();
                LrcTop.SetValue(Canvas.LeftProperty, (this.Width - LrcTop.ActualWidth) / 2);
                return;
            }
            Config config = Config.getInstance();
            ProgressBar current, another;
            if(indexLyric % 2 == 0) {
                current = LrcBottom;
                another = LrcTop;
            } else {
                current = LrcTop;
                another = LrcBottom;
            }
            current.Tag = MainWindow.lyric.GetLine((uint)indexLyric);
            current.Value = config.lyricAnimation ? valueLyric : 0;
            current.UpdateLayout();
            if(progressLyric < 0.5) {
                //uint在小于0时溢出，得到最大值
                string tag = MainWindow.lyric.GetLine((uint)indexLyric - 1);
                if((string)another.Tag != tag) {
                    another.Tag = tag;
                    another.Value = 0.99;
                    another.UpdateLayout();
                }
                another.Value = 1;
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
                    worker.ReportProgress(0);
                } else {
                    worker.ReportProgress(1);
                }
                System.Threading.Thread.Sleep(50);
            }
        }
    }
}