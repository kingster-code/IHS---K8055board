using System;
using System.Runtime.InteropServices;

namespace K8055Test
{
    static class K8055
    {
        [DllImport("K8055D.dll")]
        public static extern int OpenDevice(int CardAdress);

        [DllImport("K8055D.dll")]
        public static extern void CloseDevice();

        [DllImport("K8055D.dll")]
        public static extern int ReadAnalogChannel(int Channel);

        [DllImport("K8055D.dll")]
        public static extern void ReadAllAnalog(ref int Data1, ref int Data2);

        [DllImport("K8055D.dll")]
        public static extern void ClearAnalogChannel(int Channel);

        [DllImport("K8055D.dll")]
        public static extern void ClearAllAnalog();

        [DllImport("K8055D.dll")]
        public static extern void OutputAnalogChannel(int Channel, int Data);

        [DllImport("K8055D.dll")]
        public static extern void OutputAllAnalog(int Data1, int Data2);

        [DllImport("K8055D.dll")]
        public static extern void SetAnalogChannel(int Channel);

        [DllImport("K8055D.dll")]
        public static extern void SetAllAnalog();

        [DllImport("K8055D.dll")]
        public static extern void ClearAllDigital();

        [DllImport("K8055D.dll")]
        public static extern void ClearDigitalChannel(int Channel);

        [DllImport("K8055D.dll")]
        public static extern void SetAllDigital();

        [DllImport("K8055D.dll")]
        public static extern void SetDigitalChannel(int Channel);

        [DllImport("K8055D.dll")]
        public static extern void WriteAllDigital(int Data);

        [DllImport("K8055D.dll")]
        public static extern bool ReadDigitalChannel(int Channel);

        [DllImport("K8055D.dll")]
        public static extern int ReadAllDigital();

        [DllImport("K8055D.dll")]
        public static extern int ReadCounter(int CounterNr);

        [DllImport("K8055D.dll")]
        public static extern void ResetCounter(int CounterNr);

        [DllImport("K8055D.dll")]
        public static extern void SetCounterDebounceTime(int CounterNr, int DebounceTime);

        //ADDED

        [DllImport("K8055D.dll")]
        public static extern void SetDigitalInputChannel(int Channel, bool Status);

        [DllImport("K8055D.dll")]
        public static extern void SetAnalogInputChannel(int Channel, int Data);
    }
}
