﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chickweed
{
    /// <summary>
    /// プラグインで実装するインターフェイス
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// プラグインの名前
        /// </summary>
        string Name { get; }

        /// <summary>
        /// プラグインのバージョン
        /// </summary>
        string Version { get; }

        /// <summary>
        /// プラグインの説明
        /// </summary>
        string Description { get; }

        /// <summary>
        /// プラグインのホスト
        /// </summary>
        IPluginHost Host { get; set; }

        /// <summary>
        /// プラグインを実行する
        /// </summary>
        void Run();
    }

    public interface IPluginHost
    {
        /// <summary>
        /// ホストのメインウィンドウ
        /// </summary>
        Window MainWindow { get; }

        /// <summary>
        /// ホストのTabControl
        /// </summary>
        TabControl MainTabControl { get; }
    }
    public partial class MainWindow : Window
    {
        /// <summary>
        /// MainWindow.xaml の相互作用ロジック
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// MainWindowのウィンドウハンドル取得
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                return helper.Handle;
            }
        }

        public TabControl GetTabControl
        {
            get
            {
                return MainTabControl;
            }
        }

        public struct SupportData
        {
            public bool IsSupportAvailable;
            public string EvaluationText;
            public DateTime WExpireDate;
            public DateTime DExpireDate;
        }

        /// <summary>
        /// 各クラスの初期化
        /// </summary>
        
        NetworkAdapter network = new NetworkAdapter();
        GetRegistryKeys reg = new GetRegistryKeys();
        Utilities util = new Utilities();

        int result = 0;

        /// <summary>
        /// データの取得処理
        /// </summary>
        private void Setup()
        {
            int errorcount;
            DateTime dateTime = DateTime.Now;
            
            do
            {
                errorcount = 0;
                try
                {
                    appversion.Text = "バージョン "+ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

                    string release_id    = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
                    string major_version = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild", "").ToString();
                    string minor_version = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR", "").ToString();

                    windows.Text = reg.GetOSFullName();
                    version.Text = "バージョン " + release_id + " (OSビルド " + major_version + "." + minor_version + ")";

                    releaseid.Text = release_id;

                    SupportData data = JudgeVersion(release_id, dateTime);
                    support.Text = data.WExpireDate.ToString("yyyy年MM月dd日まで");
                    active.Text  = data.DExpireDate.ToString("yyyy年MM月31日まで");

                    if (JudgeWindowsVersion(reg.GetOSFullName()) == "10")
                    {
                        evaluation.Text = data.EvaluationText;
                    }
                    else {
                        evaluation.Foreground = new SolidColorBrush(Colors.Red);
                        evaluation.Text = "IT管理委員は資料を見て評価してください";
                    }
                    maker.Text   = reg.GetHardwareVendorName();
                    sysname.Text = reg.GetHardwareModelName();

                    NetworkInterface adapter = network.SearchAdapterTypeFromString(NetworkInterfaceType.Wireless80211, "Wi-Fi");

                    adaptername.Text = network.GetAdapterName(adapter);
                    vendorname.Text = network.GetAdapterVendor(adapter);
                    phynumber.Text = network.GetMacAddressFromAdapter(adapter);
                }
                catch (Exception)
                {
                    errorcount = 1;
                }

                if (errorcount != 0)
                {
                    using (TaskDialog errordialog = new TaskDialog())
                    {
                        result = 0;

                        errordialog.Caption             = "Chickweed™ 評価システム";
                        errordialog.InstructionText     = "一部の情報を取得できませんでした";
                        errordialog.Text                = "評価に必要な情報が不足しています。タスクを選択してください。";
                        errordialog.Icon                = TaskDialogStandardIcon.Error;
                        errordialog.OwnerWindowHandle   = Handle;

                        var retry = new TaskDialogCommandLink("retry", "再度評価を実施する(&R)\n一時的な問題はこれらによって解決する可能性があります。");
                        retry.Default = true;
                        retry.Click += (sender, e) =>
                        {
                            result = 0;
                            errordialog.Close();
                        };
                        errordialog.Controls.Add(retry);

                        var cancel = new TaskDialogCommandLink("cancel", "評価を終了する(&X)");
                        cancel.Click += (sender, e) =>
                        {
                            result = 1;
                            errordialog.Close();
                        };
                        errordialog.Controls.Add(cancel);

                        errordialog.Show();
                    }
                    switch (result)
                    {
                        case 0:
                            continue;
                        case 1:
                            errorcount = 0;
                            break;
                    }
                }
            } while (errorcount != 0);
        }
        private void InitPlugins()
        {
            //インストールされているプラグインを調べる
            PluginInfo[] pis = PluginInfo.FindPlugins();

            //すべてのプラグインクラスのインスタンスを作成する
            IPlugin[] plugins = new IPlugin[pis.Length];
            for (int i = 0; i < plugins.Length; i++) plugins[i] = pis[i].CreateInstance(this);

            for (int number = 0; number < plugins.Length; number++)
            {
                //プラグインのRunメソッドを呼び出し
                plugins[number].Run();
            }
        }
        private void Disp_Loaded(object sender, RoutedEventArgs e)
        {
            InitPlugins();
            Setup();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Setup();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using (TaskDialog savedialog = new TaskDialog())
            {
                result = 0;

                savedialog.Caption              = "Chickweed™";
                savedialog.InstructionText      = "評価の結果に対するタスクを選択してください。";
                savedialog.Cancelable           = true;
                savedialog.OwnerWindowHandle    = Handle;

                var savenormal = new TaskDialogCommandLink("savenormal", "既定のファイルに保存(&E)\n既定のファイルに追記します。通常はこれを選択してください。");
                savenormal.Default = true;
                savenormal.Click += (sender, e) =>
                {
                    result = 0;
                    savedialog.Close();
                };
                savedialog.Controls.Add(savenormal);

                var savenew = new TaskDialogCommandLink("savenew", "名前を付けて保存(&A)\n別名で保存します。上書きの場合は元のデータが消去されます。");
                savenew.Click += (sender, e) =>
                {
                    result = 1;
                    savedialog.Close();
                };

                savedialog.Controls.Add(savenew);

                var exit = new TaskDialogCommandLink("exit", "キャンセル(&C)");
                exit.Click += (sender, e) =>
                {
                    result = 2;
                    savedialog.Close();
                };
                savedialog.Controls.Add(exit);

                savedialog.Show();
            }
            switch (result)
            {
                case 0:
                    SaveWindow saveNormal = new SaveWindow("評価を保存",SaveMode.SAVE);
                    saveNormal.ShowDialog();
                    break;
                case 1:
                    SaveWindow saveAnother = new SaveWindow("名前を付けて評価を保存",SaveMode.OVERWRITE);
                    saveAnother.ShowDialog();
                    break;
                case 2:
                    break;
            }
        }
        private void Disp_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            using (TaskDialog savedialog = new TaskDialog())
            {
                result = 0;

                savedialog.Caption              = "Chickweed™";
                savedialog.InstructionText      = "評価の結果に対するタスクを選択してください。";
                savedialog.Cancelable           = true;
                savedialog.OwnerWindowHandle    = Handle;

                var savenormal = new TaskDialogCommandLink("savenormal", "既定のファイルに保存(&E)\n既定のファイルに追記します。通常はこれを選択してください。");
                savenormal.Default = true;
                savenormal.Click += (sender, e) =>
                {
                    result = 1;
                    savedialog.Close();
                };
                savedialog.Controls.Add(savenormal);

                var savenew = new TaskDialogCommandLink("savenew", "名前を付けて保存(&A)\n別名で保存します。上書きの場合は元のデータが消去されます。");
                savenew.Click += (sender, e) =>
                {
                    result = 2;
                    savedialog.Close();
                };

                savedialog.Controls.Add(savenew);

                var exit = new TaskDialogCommandLink("exit", "保存せずに終了(&X)");
                exit.Click += (sender, e) =>
                {
                    result = 3;
                    savedialog.Close();
                };
                savedialog.Controls.Add(exit);

                savedialog.Show();
            }
            switch (result)
            {
                case 0:
                    e.Cancel = true;
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    Application.Current.Shutdown();
                    break;
            }
        }

        private void GoToProxySetting_Click(object sender, RoutedEventArgs e)
        {
            string winver = JudgeWindowsVersion(reg.GetOSFullName());
            if (winver == "10") 
            {
                Process.Start("ms-settings:network-proxy");
            }
            /* Windows 8.1 以下だった場合の処理
            else
            {
                Process.Start("control.exe", "inetcpl.cpl,,4");
            }
            */
        }

        private void GoToUpdateSetting_Click(object sender, RoutedEventArgs e)
        {
            string winver = JudgeWindowsVersion(reg.GetOSFullName());
            if (winver == "10")
            {
                Process.Start("ms-settings:windowsupdate-action");
            }
            /*
            else
            {
                Process.Start("control.exe", "wuaucpl.cpl");
            }
            */
        }

        private void CreateProxyShortcutToDesktop_Click(object sender, RoutedEventArgs e)
        {
            string winver = JudgeWindowsVersion(reg.GetOSFullName());
            if (winver == "10")
            {
                CreateShortcut("プロキシ設定", "ms-settings:network-proxy", "0", "C:\\Windows\\System32\\Shell32.dll" + ",316");
            }
            /* 謎のダブルクォーテーションが入るlinkPath
            else
            {
                CreateShortcut("プロキシ設定", @"C:\\Windows\\System32\\rundll32.exe shell32.dll,Control_RunDLL inetcpl.cpl,,4", "C:\\WINDOWS\\system32", "C:\\Windows\\System32\\Shell32.dll" + ",316");
            }
            */
        }

        private void CreateUpdateShortcutToDesktop_Click(object sender, RoutedEventArgs e)
        {
            string winver = JudgeWindowsVersion(reg.GetOSFullName());
            if (winver == "10")
            {
                CreateShortcut("Windows Update", "ms-settings:windowsupdate-action", "0", "C:\\Windows\\System32\\Shell32.dll" + ",316");
            }
            /* 
            else
            {
                CreateShortcut("Windows Update", @"C:\\Windows\\System32\\rundll32.exe shell32.dll,Control_RunDLL /name Microsoft.WindowsUpdate", "C:\\WINDOWS\\system32", "C:\\Windows\\System32\\Shell32.dll" + ",316");
            }
            */
        }

        public void CreateShortcut(string Name, string linkPath, string workingPath, string iconPath)
        {
            // 作成するショートカットのパス
            string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),@Name+".lnk");

            // WshShellを作成
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
            dynamic shell = Activator.CreateInstance(t);

            // WshShortcutを作成
            var shortcut = shell.CreateShortcut(shortcutPath);

            // リンク先
            shortcut.TargetPath = linkPath;

            // ワーキングディレクトリ
            if (workingPath != "0")
            {
                shortcut.WorkingDirectory = workingPath;
            }

            // アイコンのパス
            shortcut.IconLocation = iconPath;

            // ショートカットを作成
            shortcut.Save();

            // 後始末
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }

        public SupportData JudgeVersion(string id, DateTime date) 
        {
            SupportData support = new SupportData();
            //サポート期間の定数
            const int supportPeriod  = 18;
            //チェックするタイミング
            DateTime checkSpan = new DateTime(date.Year,4,1,0,0,0);

            //VersionIDから取得したリリース年月
            int idYear      = int.Parse(util.SubstringAtCount(id, 2)[0]);
            int idMonth     = int.Parse(util.SubstringAtCount(id, 2)[1]);

            //そのバージョンのサポート期間の計算
            int yearExpire  = util.DateCount(idYear, idMonth, 0, supportPeriod)[0];
            int monthExpire = util.DateCount(idYear, idMonth, 0, supportPeriod)[1];

            //DateTime型に変換したサポート期間
            DateTime expire = DateTime.Parse(string.Format("20{0:00}/{1}/31", yearExpire, monthExpire));

            //年度換算
            DateTime checkpoint = new DateTime(date.Year, 3, 31, 0, 0, 0);
            switch (date.CompareTo(checkSpan))
            {
                case 1:
                    checkpoint.AddYears(1);
                    break;
            }

            //年度末までサポートがあるか確認
            switch (checkpoint.CompareTo(expire))
            {
                case -1:
                    evaluation.Foreground = new SolidColorBrush(Colors.Green);
                    support.IsSupportAvailable = true;
                    support.EvaluationText = "申請許可";
                    break;
                case 0:
                    evaluation.Foreground = new SolidColorBrush(Colors.Red);
                    support.IsSupportAvailable = false;
                    support.EvaluationText = "申請不可(アップデートが必要です)";
                    break;
                case 1:
                    evaluation.Foreground = new SolidColorBrush(Colors.Red);
                    support.IsSupportAvailable = false;
                    support.EvaluationText = "申請不可(アップデートが必要です)";
                    break;
            }

            support.DExpireDate = checkpoint;
            support.WExpireDate = expire;

            return support;

            /*
            int Base_year, Year_end, Version_support, Version_year, Version_month;
            // 期限用数値の作成
            DateTime dateTime = DateTime.Now;
            Base_year = int.Parse(dateTime.ToString("yy")) + 1;
            Year_end = int.Parse(Convert.ToString(Base_year) + "03");

            // バージョンの計算
            Version_year = int.Parse(Version_id.Substring(0, 2));
            Version_month = int.Parse(Version_id.Substring(3));
            for (int i = 0; i < 18; i++) {
                Version_month++;
                if (Version_month == 12) {
                    Version_month = 1;
                    Version_year++;
                }
            }
            Version_support = int.Parse(Convert.ToString(Version_year) + string.Format("{0:00}", Version_month));
            
            // 申請判定
            if (Year_end < Version_support)
            {
                evaluation.Foreground = new SolidColorBrush(Colors.Green);
                return "申請許可";
            }
            else
            {
                evaluation.Foreground = new SolidColorBrush(Colors.Red);
                return "申請不可(Windows Updateが必要です)";
            }*/
        }

        public string JudgeWindowsVersion(string winver) 
        {
            if (winver.Contains("10"))
            {
                return "10";
            }
            else if(winver.Contains("8"))
            {
                return "8";
            }
            else
            {
                return null;
            }
            //string[] winver_splitted = winver.Split(' ');
            //return "8"; //winver_splitted[2];
        }
    }
}
