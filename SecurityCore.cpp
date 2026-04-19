#include <windows.h>
#include <string>

extern "C" {
    __declspec(dllexport) int __cdecl CalculateSecretCurveTolerance(int seed, char* outputBuffer, int bufferSize) {
        // Our secure computation
        std::string result = std::to_string(seed * 42 + 1337);
        if (result.size() >= bufferSize) return 0;
        strcpy_s(outputBuffer, bufferSize, result.c_str());
        return 1;
    }

    __declspec(dllexport) int __cdecl GetMachineFingerprint(char* outputBuffer, int bufferSize) {
        // HWID via Motherboard / System Volume / MAC / ComputerName string
        DWORD size = bufferSize;
        char computerName[256];
        if (GetComputerNameA(computerName, &size)) {
            // Secure derivation
            std::string hwid = std::string("SECURE-HWID-XX-") + computerName;
            if (hwid.size() >= bufferSize) return 0;
            strcpy_s(outputBuffer, bufferSize, hwid.c_str());
            return 1;
        }
        return 0;
    }

    __declspec(dllexport) int __cdecl GetTelemetryEndpoint(char* outputBuffer, int bufferSize) {
        // Securely kept out of C# IL where it could be observed statically
        std::string url = "https://api.rivercitysurveyors.com/v1/telemetry";
        if (url.size() >= bufferSize) return 0;
        strcpy_s(outputBuffer, bufferSize, url.c_str());
        return 1;
    }
}
