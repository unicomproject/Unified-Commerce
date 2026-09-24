using System;
using System.IO;

namespace E_POS.Api.Realtime {
    public static class DebugLogger {
        public static void Log(string msg) {
            File.AppendAllText(@"C:\Users\User\Desktop\pos final wep\backend_debug.txt", DateTime.Now.ToString() + " " + msg + Environment.NewLine);
        }
    }
}
