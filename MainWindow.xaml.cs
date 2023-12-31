﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace BarcodeWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LowLevelKeyboardListener _listener;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _listener = new LowLevelKeyboardListener();
            _listener.OnKeyPressed += _listener_OnKeyPressed;

            _listener.HookKeyboard();
        }

        void _listener_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            this.BarcodeResultLabel.Content = e.KeyPressed.ToString();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _listener.UnHookKeyboard();
        }


        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }
    }






    public class LowLevelKeyboardListener
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<KeyPressedArgs> OnKeyPressed;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        DateTime _lastKeystroke = new DateTime(0);
        List<string> _barcode = new List<string>();

        public LowLevelKeyboardListener()
        {
            _proc = HookCallback;
        }

        public void HookKeyboard()
        {
            _hookID = SetHook(_proc);

            _lastKeystroke = new DateTime(0);
            _barcode = new List<string>();

        }

        public void UnHookKeyboard()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = KeyInterop.KeyFromVirtualKey(vkCode);


                TimeSpan elapsed = (DateTime.Now - _lastKeystroke);

                var afterLongTime = elapsed.TotalMilliseconds > 25;

                if (afterLongTime)
                {
                    if (_barcode.Count > 9)
                        Invoke();
                    _barcode.Clear();
                }


                if (Keys.IsNumber.Any(x => x == (int)key))
                    _barcode.Add(key.ToString().Replace("D", ""));

                if (Keys.IsAlpha.Any(x => x == (int)key))
                    _barcode.Add(key.ToString());

                _lastKeystroke = DateTime.Now;

                if ((int)key == 6 && _barcode.Count > 9)
                {
                    ControlTyping(true, string.Join("", _barcode));
                    Invoke();
                    _barcode.Clear();
                }



                void Invoke()
                {
                    var f = string.Join("", _barcode.Select(x => x.Replace("D", ""))) + Environment.NewLine;
                    //f += Environment.NewLine;
                    if (OnKeyPressed != null)
                        OnKeyPressed(this, new KeyPressedArgs(f));
                }

                void ControlTyping(bool status = false, string txt = "")
                {
                    var f = FocusManager.GetFocusedElement(Application.Current.Windows[0]);
                    if (f is TextBox)
                    {
                        var t = (TextBox)f;
                        if (status == true && txt.Length > 0)
                        {
                            t.Text = t.Text.Replace(txt, "");
                            t.Select(t.Text.Length, 0);
                        }
                    }
                }

            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }

    public static class Ext
    {
        public static void SetText(this RichTextBox richTextBox, string text)
        {
            richTextBox.Document.Blocks.Clear();
            richTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
        }

        public static string GetText(this RichTextBox richTextBox)
        {
            return new TextRange(richTextBox.Document.ContentStart,
                richTextBox.Document.ContentEnd).Text;
        }
    }

    public class KeyPressedArgs : EventArgs
    {
        public string KeyPressed { get; private set; }

        public KeyPressedArgs(string key)
        {
            KeyPressed = key;
        }
    }


    public class Keys
    {
        //public static List<int> IsValid = IsNumber.Concat(IsAlpha).ToList();
        public static List<int> IsNumber = new List<int>
        {
            //numbers
            34,
            35,
            36,
            37,
            38,
            39,
            40,
            41,
            42,
            43,
        };
        public static List<int> IsAlpha = new List<int>
        {
            //Alpha
            44,
            45,
            46,
            47,
            48,
            49,
            50,
            51,
            52,
            53,
            54,
            55,
            56,
            57,
            58,
            59,
            60,
            61,
            62,
            63,
            64,
            65,
            66,
            67,
            68,
            69,
        };
    }
}
