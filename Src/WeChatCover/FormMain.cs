﻿using System;
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

        private readonly HashSet<string> ClassNameBlackList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ImagePreviewWnd",
            "FileListMgrWnd",
            "IntermediateD3DWindow",
            "FileManagerWnd",
            "AudioWnd",
            "VideoLayerWnd",
            "BackupRestoreEntryWnd",
            "ExportDataWnd",
            "BackupRestoreWnd",
            "ContactManagerWindow"
        };

        public FormMain()
        {
            InitializeComponent();

            _strNotice = Settings.Default.Notice.Trim();

            Shown += (s1, e1) => Hide();

            new Thread(() =>
            {
                while (true)
                {
                    if (contextMenuStrip1.Visible)
                    {
                        Thread.Sleep(300);
                        continue;
                    }

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

                        using Bitmap bmp = new Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top);
                        using Graphics g = Graphics.FromImage(bmp);
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);

                        Bitmap bmpMosaic = Mosaic(bmp, 10);

                        using Graphics gBg = Graphics.FromImage(bmpMosaic);

                        if (!string.IsNullOrEmpty(_strNotice))
                        {
                            SizeF szNotice = gBg.MeasureString(_strNotice, _ftNotice);
                            gBg.DrawString(_strNotice, _ftNotice, Brushes.Crimson, (Width - szNotice.Width) / 2, (Height - szNotice.Height) / 2);
                        }

                        BeginInvoke(new MethodInvoker(() =>
                        {
                            Size = bmpMosaic.Size;
                            Location = new Point(0, 0);
                            BackgroundImage?.Dispose();
                            BackgroundImage = bmpMosaic;
                        }));
                    }
                    else
                    {
                        if (_weChatHandle == IntPtr.Zero)
                            continue;

                        if (Disposing || IsDisposed)
                            return;

                        if (ClassNameBlackList.Contains(sbClassName.ToString().Replace(" ",string.Empty)))
                            continue;

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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Environment.Exit(0);
        }
    }
}
