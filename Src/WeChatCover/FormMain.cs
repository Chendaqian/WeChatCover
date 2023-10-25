using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using WeChatCover.Properties;

using static WeChatCover.NativeCodes;
using static WeChatCover.Utilities;

namespace WeChatCover
{
    public partial class FormMain : Form
    {
        private IntPtr _weChatHandle;
        private List<IntPtr> _arrSubscriptionWndHandles = new List<IntPtr>();

        private readonly Font _ftNotice = new Font("微软雅黑", 80);
        private readonly string _strNotice;

        public FormMain()
        {
            InitializeComponent();

            _strNotice = Settings.Default.Notice.Trim();

            Shown += (s1, e1) => Hide();

            new Thread(() =>
            {
                while (true)
                {
                    IntPtr hForeground = GetForegroundWindow();
                    StringBuilder sbClassName = new StringBuilder(50);
                    GetClassName(hForeground, sbClassName, sbClassName.Capacity);

                    if (sbClassName.ToString().Contains("WeChatMainWndForPC"))
                    {
                        _weChatHandle = hForeground;

                        if (Disposing || IsDisposed)
                            return;

                        BeginInvoke(new MethodInvoker(() =>
                        {
                            SetParent(Handle, IntPtr.Zero);
                            Hide();
                        }));

                        GetWindowRect(hForeground, out RECT rect);
                        //Debug.Print(rect.Left.ToString());

                        Bitmap bmp = new Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top);
                        using Graphics g = Graphics.FromImage(bmp);
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);

                        Bitmap bmpMosaic = Mosaic(bmp, 10);

                        BeginInvoke(new MethodInvoker(() =>
                        {
                            Size = bmp.Size;
                            Location = new Point(0, 0);
                            BackgroundImage = bmpMosaic;

                            bmp.Dispose();
                            bmpMosaic.Dispose();
                        }));
                    }
                    else
                    {
                        if (_weChatHandle == IntPtr.Zero)
                            continue;

                        if (Disposing || IsDisposed)
                            return;

                        StringBuilder sbText = new StringBuilder(50);
                        GetWindowText(hForeground, sbText, sbText.Capacity);
                        if (sbText.ToString() == "WeChatCover")
                        {
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                ShowWindows(_arrSubscriptionWndHandles, true);

                                SetParent(Handle, IntPtr.Zero);
                                SetForegroundWindow(_weChatHandle);
                                Hide();
                            }));
                            continue;
                        }

                        BeginInvoke(new MethodInvoker(() =>
                        {
                            GetSubscriptionWndHandle();

                            IntPtr hActived = GetActiveWindow();
                            Visible = true;
                            SetParent(Handle, _weChatHandle);
                            SetActiveWindow(hActived);

                            ShowWindows(_arrSubscriptionWndHandles, false);
                        }));
                    }
                    Thread.Sleep(300);
                }
            }).Start();

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        private void GetSubscriptionWndHandle()
        {
            if (_weChatHandle == IntPtr.Zero)
                return;

            EnumWindows(EmProc, IntPtr.Zero);
            return;

            bool EmProc(IntPtr hWnd, IntPtr lParam)
            {
                StringBuilder sbClassName = new StringBuilder(50);
                GetClassName(hWnd, sbClassName, sbClassName.Capacity);

                if (sbClassName.ToString() != "CWebviewControlHostWnd")
                    return true;

                if (GetWindow(hWnd, GW_OWNER) != _weChatHandle)
                    return true;

                if (!_arrSubscriptionWndHandles.Contains(hWnd))
                    _arrSubscriptionWndHandles.Add(hWnd);

                return true;
            }
        }

        private void ShowWindows(List<IntPtr> lstHandles, bool show)
        {
            // TODO:订阅号窗口被我直接干掉了，恢复订阅号窗口需要手动点订阅号
            foreach (IntPtr hWnd in lstHandles)
                SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, show ? SWP_SHOWWINDOW : SWP_NOACTIVATE);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (string.IsNullOrEmpty(_strNotice))
                return;

            SizeF szNotice = e.Graphics.MeasureString(_strNotice, _ftNotice);
            e.Graphics.DrawString(_strNotice, _ftNotice, Brushes.Crimson, (Width - szNotice.Width) / 2, (Height - szNotice.Height) / 2);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}