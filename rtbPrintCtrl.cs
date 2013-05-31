namespace Telnet
{
    using System;
    using System.Drawing.Printing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class rtbPrintCtrl : RichTextBox
    {
        private const double anInch = 14.4;
        private const int EM_FORMATRANGE = 0x439;
        private const int WM_USER = 0x400;

        public int Print(int charFrom, int charTo, PrintPageEventArgs e)
        {
            RECT rect;
            RECT rect2;
            FORMATRANGE formatrange;
            rect.Top = (int)(e.MarginBounds.Top * 14.4);
            rect.Bottom = (int)(e.MarginBounds.Bottom * 14.4);
            rect.Left = (int)(e.MarginBounds.Left * 14.4);
            rect.Right = (int)(e.MarginBounds.Right * 14.4);
            rect2.Top = (int)(e.PageBounds.Top * 14.4);
            rect2.Bottom = (int)(e.PageBounds.Bottom * 14.4);
            rect2.Left = (int)(e.PageBounds.Left * 14.4);
            rect2.Right = (int)(e.PageBounds.Right * 14.4);
            IntPtr hdc = e.Graphics.GetHdc();
            formatrange.chrg.cpMax = charTo;
            formatrange.chrg.cpMin = charFrom;
            formatrange.hdc = hdc;
            formatrange.hdcTarget = hdc;
            formatrange.rc = rect;
            formatrange.rcPage = rect2;
            IntPtr zero = IntPtr.Zero;
            IntPtr wp = IntPtr.Zero;
            wp = new IntPtr(1);
            IntPtr ptr = IntPtr.Zero;
            ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(formatrange));
            Marshal.StructureToPtr(formatrange, ptr, false);
            zero = SendMessage(base.Handle, 0x439, wp, ptr);
            Marshal.FreeCoTaskMem(ptr);
            e.Graphics.ReleaseHdc(hdc);
            return zero.ToInt32();
        }

        [DllImport("USER32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE
        {
            public int cpMin;
            public int cpMax;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc;
            public IntPtr hdcTarget;
            public rtbPrintCtrl.RECT rc;
            public rtbPrintCtrl.RECT rcPage;
            public rtbPrintCtrl.CHARRANGE chrg;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);

        }
    }
}

