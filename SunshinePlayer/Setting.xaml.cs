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
        /// 窗口拖动
        /// </summary>
        private void dragWindow(object sender, MouseEventArgs e) {
            if(e.LeftButton == MouseButtonState.Pressed) {
                this.DragMove();  //拖动窗口
            } else {
                this.MouseMove -= dragWindow;
            }
        }
        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //添加左侧菜单
            double t = mainFrame.Margin.Top;
            foreach(UIElement child in mainFrame.Children) {
                if(child is Label) {
                    Label lab = child as Label;
                    //菜单标题标签
                    if(lab.Style == Resources["labTitle"]) {
                        Label m = new Label();
                        m.Content = lab.Content;
                        m.Style = (Style)Resources["NormalMenuItem"];
                        m.Tag = t;
                        //菜单项点击滚动
                        m.MouseDown += delegate {
                            scrollFrame.ScrollToVerticalOffset((double)m.Tag);
                        };
                        leftMenu.Children.Add(m);
                        lab.Tag = t;
                    }
                }
                t += child.RenderSize.Height;
            }
            ((Label)leftMenu.Children[0]).Style = (Style)Resources["ActivityMenuItem"];
            //主界面滚动
            scrollFrame.ScrollChanged += (object s, ScrollChangedEventArgs se) => {
                Label n = null;
                foreach(UIElement child in mainFrame.Children) {
                    if(child is Label) {
                        Label lab = child as Label;
                        //菜单标题标签
                        if(lab.Style == Resources["labTitle"]) {
                            if((double)lab.Tag <= se.VerticalOffset + lab.ActualHeight) {
                                n = lab;
                            }
                        }
                    }
                }
                foreach(Label menu in leftMenu.Children) {
                    if((double)menu.Tag == (double)n.Tag) {
                        menu.Style = (Style)Resources["ActivityMenuItem"];
                    } else {
                        menu.Style = (Style)Resources["NormalMenuItem"];
                    }
                }
            };
            //加载配置
            Config config = Config.getInstance();
            desktopLyric.IsChecked = config.showDesktopLyric;  //桌面歌词
            autoPlay.IsChecked = config.autoPlay;  //自动播放
            lyricAnimation.IsChecked = config.lyricAnimation;  //歌词卡拉OK效果
            desktopLyricLock.IsChecked = config.desktopLyricLocked;  //锁定桌面歌词
        }
        /// <summary>
        /// 保存
        /// </summary>
        private void save(object sender, RoutedEventArgs e) {
        }
        /// <summary>
        /// 确定
        /// </summary>
        private void ok(object sender, RoutedEventArgs e) {
            save(sender, e);
            cancel(sender, e);
        }
        /// <summary>
        /// 取消
        /// </summary>
        private void cancel(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
