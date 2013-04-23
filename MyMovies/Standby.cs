using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MyMovies
{
    public static class Standby
    {
        public enum WIN7_EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_USER_PRESENT = 0x00000004,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern WIN7_EXECUTION_STATE SetThreadExecutionState(WIN7_EXECUTION_STATE flags);

        public static void Prevent()
        {
            SetThreadExecutionState(WIN7_EXECUTION_STATE.ES_SYSTEM_REQUIRED | WIN7_EXECUTION_STATE.ES_CONTINUOUS | WIN7_EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
        }

        public static void Allow()
        {
            SetThreadExecutionState(WIN7_EXECUTION_STATE.ES_CONTINUOUS);        
        }
    }
}
