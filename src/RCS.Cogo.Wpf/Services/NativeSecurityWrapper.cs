using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RCS.Cogo.Wpf.Services
{
    public static class NativeSecurityWrapper
    {
        [DllImport("SecurityCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int CalculateSecretCurveTolerance(int inputSeed, StringBuilder outputBuffer, int bufferSize);

        [DllImport("SecurityCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetMachineFingerprint(StringBuilder outputBuffer, int bufferSize);

        [DllImport("SecurityCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetTelemetryEndpoint(StringBuilder outputBuffer, int bufferSize);

        public static string GetHardwareFingerprint()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"CROSS_PLATFORM_HWID_{Environment.MachineName}";
            }

            try
            {
                StringBuilder buffer = new StringBuilder(256);
                int result = GetMachineFingerprint(buffer, buffer.Capacity);
                
                if (result == 1)
                {
                    return buffer.ToString();
                }
                
                return $"MANAGED_FALLBACK_HWID_{Environment.MachineName}";
            }
            catch
            {
                return $"MANAGED_FALLBACK_HWID_{Environment.MachineName}";
            }
        }

        public static string GetSecureData(int seed)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (seed * 42 + 1337).ToString();
            }

            try
            {
                StringBuilder buffer = new StringBuilder(256);
                int result = CalculateSecretCurveTolerance(seed, buffer, buffer.Capacity);

                if (result == 1)
                {
                    return buffer.ToString(); 
                }
                
                return (seed * 42 + 1337).ToString();
            }
            catch
            {
                return (seed * 42 + 1337).ToString();
            }
        }

        public static string GetSecureTelemetryEndpoint()
        {
            const string FallbackUrl = "https://api.rivercitysurveyors.com/v1/telemetry";

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return FallbackUrl;
            }

            try
            {
                StringBuilder buffer = new StringBuilder(256);
                int result = GetTelemetryEndpoint(buffer, buffer.Capacity);
                
                if (result == 1)
                {
                    return buffer.ToString();
                }
                return FallbackUrl;
            }
            catch
            {
                return FallbackUrl;
            }
        }
    }
}
