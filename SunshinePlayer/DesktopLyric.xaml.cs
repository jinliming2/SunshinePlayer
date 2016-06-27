using System.Windows;

namespace SunshinePlayer {
    /// <summary>
    /// DesktopLyric.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopLyric : Window {
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
    }
}
