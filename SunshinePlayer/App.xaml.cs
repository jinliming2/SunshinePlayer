using System.Windows;
using System.Windows.Media.Animation;

namespace SunshinePlayer {
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            SunshinePlayer.MainWindow.Args = e.Args;
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata {
                DefaultValue = 25
            });	//设置WPF动画默认帧数
        }
    }
}
