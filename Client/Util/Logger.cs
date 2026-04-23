using UnityEngine;

namespace KspConnected.Client.Util
{
    public static class KspLog
    {
        private const string Tag = "[KspConnected] ";

        public static void Log(string msg)   => Debug.Log(Tag + msg);
        public static void Warn(string msg)  => Debug.LogWarning(Tag + msg);
        public static void Error(string msg) => Debug.LogError(Tag + msg);
    }
}
