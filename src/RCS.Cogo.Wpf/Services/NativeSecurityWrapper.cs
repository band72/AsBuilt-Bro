using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RCS.Cogo.Wpf.Services
{
    public static class NativeSecurityWrapper
    {
        // Tell C# to look for the compiled "SecurityCore.dll" file. 
        // This MUST be copied to the output directory (.exe location)
        [DllImport("SecurityCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int CalculateSecretCurveTolerance(int inputSeed, StringBuilder outputBuffer, int bufferSize);

        [DllImport("SecurityCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetMachineFingerprint(StringBuilder outputBuffer, int bufferSize);

        /// <summary>
        /// Retrieves the unique hardware ID from the unmanaged C++ DLL.
        /// This creates a strong locking mechanism tied to Motherboard/HDD/MAC Addresses.
        /// </summary>
        public static string GetHardwareFingerprint()
        {
            try
            {
                StringBuilder buffer = new StringBuilder(256);
                int result = GetMachineFingerprint(buffer, buffer.Capacity);
                
                if (result == 1)
                {
                    return buffer.ToString();
                }
                
                return "FINGERPRINT_FAIL";
            }
            catch (DllNotFoundException)
            {
                return "DLL_MISSING";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>
        /// Calls the C++ Native Machine Code DLL to grab our "secure" encrypted/computed data.
        /// It is significantly harder for a hacker to reverse-engineer standard x64 Assembly 
        /// than it is to decompile IL C# code in dnSpy.
        /// </summary>
        public static string GetSecureData(int seed)
        {
            try
            {
                // Create a pre-allocated buffer for C++ to write its return string into
                StringBuilder buffer = new StringBuilder(256);
                
                // Call the machine-code C++ function!
                int result = CalculateSecretCurveTolerance(seed, buffer, buffer.Capacity);

                if (result == 1)
                {
                    return buffer.ToString(); 
                }
                
                return "SECURITY_FAIL";
            }
            catch (DllNotFoundException)
            {
                // The C++ DLL is missing
                return "DLL_MISSING";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }
    }
}
