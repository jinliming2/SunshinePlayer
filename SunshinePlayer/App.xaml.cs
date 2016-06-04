using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;

namespace SunshinePlayer {
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    [System.Security.Permissions.EnvironmentPermission(System.Security.Permissions.SecurityAction.Demand)]
    public partial class App : Application {
        /// <summary>
        /// 启动参数
        /// </summary>
        private static string[] args;
        /// <summary>
        /// 启动参数
        /// </summary>
        public static string[] Args {
            get {
                return args;
            }
        }
        /// <summary>
        /// 启动目录
        /// </summary>
        public static string workPath {
            get {
                return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            }
        }
        /// <summary>
        /// 程序启动
        /// </summary>
        protected override void OnStartup(StartupEventArgs e) {
            args = e.Args; //启动参数
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata {
                DefaultValue = 25
            });	//设置WPF动画默认帧数
        }
    }
}
