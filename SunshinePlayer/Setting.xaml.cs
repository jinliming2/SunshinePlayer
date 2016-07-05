using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SunshinePlayer {
    /// <summary>
    /// Setting.xaml 的交互逻辑
    /// </summary>
    public partial class Setting : Window {
        /// <summary>
        /// 构造函数 初始化窗口
        /// </summary>
        public Setting() {
            //窗口初始化事件
            this.Loaded += initialize;
            InitializeComponent();
        }
        /// <summary>
        /// 窗口初始化
        /// </summary>
        private void initialize(object sender, RoutedEventArgs e) {
            //窗口样式模板
            ControlTemplate baseWindowTemplate = (ControlTemplate)this.Resources["settingTemplate"];
            //关闭按钮
            Button closeButton = (Button)baseWindowTemplate.FindName("closeButton", this);
            closeButton.Click += delegate { this.Close(); };  //窗口关闭

            //窗口拖动
            this.MouseLeftButtonDown += delegate { this.MouseMove += dragWindow; };
        }
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
        }
        /// <summary>
        /// 窗口拖动
        /// </summary>
        private void dragWindow(object sender, MouseEventArgs e) {
            if(e.LeftButton == MouseButtonState.Pressed) {
                this.DragMove();  //拖动窗口
            } else {
                this.MouseMove -= dragWindow;
            }
        }
    }
}
