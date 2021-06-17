using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WeChatCover
{
    public partial class FormMain : Form
    {
        [DllImport("user32")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        private Bitmap Mosaic(Bitmap bmpOrigin, int radius)
        {
            Bitmap bmpResult = new Bitmap(bmpOrigin.Width, bmpOrigin.Height);

            for (int y = radius; y < bmpOrigin.Height; y += radius * 2 + 1)
            {
                for (int x = radius; x < bmpOrigin.Width; x += radius * 2 + 1)
                {
                    int sumA = 0;
                    int sumR = 0;
                    int sumG = 0;
                    int sumB = 0;
                    int pixelCount = 0;

                    for (int y1 = y - radius; y1 < y + radius + 1; ++y1)
                    {
                        if (y1 >= bmpOrigin.Height)
                            break;

                        for (int x1 = x - radius; x1 < x + radius + 1; ++x1)
                        {
                            if (x1 >= bmpOrigin.Width)
                                break;

                            Color pixel = bmpOrigin.GetPixel(x1, y1);
                            sumA += pixel.A;
                            sumR += pixel.R;
                            sumG += pixel.G;
                            sumB += pixel.B;
                            ++pixelCount;
                        }
                    }

                    int avgA = sumA / pixelCount;
                    int avgR = sumR / pixelCount;
                    int avgG = sumG / pixelCount;
                    int avgB = sumB / pixelCount;

                    Color newColor = Color.FromArgb(avgA, avgR, avgG, avgB);

                    for (int y1 = y - radius; y1 < y + radius + 1; ++y1)
                    {
                        if (y1 >= bmpOrigin.Height)
                            break;

                        for (int x1 = x - radius; x1 < x + radius + 1; ++x1)
                        {
                            if (x1 >= bmpOrigin.Width)
                                break;

                            bmpResult.SetPixel(x1, y1, newColor);
                        }
                    }
                }
            }

            return bmpResult;
        }

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private IntPtr _weChatHandle;

        private int _screenShotCount = 0;
        // 快照更新间隔
        private const int ScreenShotInterval = 30 * 1000;

        public FormMain()
        {
            this.InitializeComponent();

            Shown += (s1, e1) => this.Hide();

            new Thread(() =>
            {
                while (true)
                {
                    IntPtr p = GetForegroundWindow();
                    StringBuilder sbClassName = new StringBuilder(50);
                    GetClassName(p, sbClassName, sbClassName.Capacity);

                    if (sbClassName.ToString().Contains("WeChatMainWndForPC"))
                    {
                        this._weChatHandle = p;

                        if (this.Disposing || this.IsDisposed)
                            return;

                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            SetParent(this.Handle, IntPtr.Zero);
                            this.Hide();
                        }));

                        GetWindowRect(p, out RECT rect);
                        //Debug.Print(rect.Left.ToString());

                        Bitmap bmp = new Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top);
                        using (Graphics g = Graphics.FromImage(bmp))
                            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);

                        Bitmap bmpMosaic = this.Mosaic(bmp, 10);

                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            this.Size = bmp.Size;
                            this.Location = new Point(0, 0);
                            this.BackgroundImage = bmpMosaic;
                        }));
                    }
                    else
                    {
                        if (this._weChatHandle == IntPtr.Zero)
                            continue;

                        if (this.Disposing || this.IsDisposed)
                            return;

                        StringBuilder sbText = new StringBuilder(50);
                        GetWindowText(p, sbText, sbText.Capacity);
                        if (sbText.ToString().Contains("WeChatCover"))
                        {
                            this.BeginInvoke(new MethodInvoker(() =>
                            {
                                SetParent(this.Handle, IntPtr.Zero);
                                SetForegroundWindow(this._weChatHandle);
                                this.Hide();
                            }));
                            continue;
                        }

                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            IntPtr hActived = GetActiveWindow();
                            this.Visible = true;
                            SetParent(this.Handle, this._weChatHandle);
                            SetActiveWindow(hActived);
                        }));
                    }
                    Thread.Sleep(100);
                }
            }).Start();

            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}