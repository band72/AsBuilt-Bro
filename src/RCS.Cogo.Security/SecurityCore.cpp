#pragma comment(lib, "iphlpapi.lib")

#include <windows.h>
#include <iphlpapi.h>
#include <string>
#include <vector>
#include <sstream>
#include <iomanip>

// A simple hashing function to obscure the raw hardware IDs
// and return a uniform, alphanumeric string snippet.
std::string HashData(const std::string& input) {
    unsigned long hash = 5381;
    for (char c : input) {
        hash = ((hash << 5) + hash) + c; // hash * 33 + c
    }
    std::stringstream ss;
    ss << std::hex << std::uppercase << std::setfill('0') << std::setw(8) << hash;
    return ss.str();
}

extern "C"
{
    // Native Windows API hardware extraction (Un-spoofable via simple C# IL tweaks)
    __declspec(dllexport) int GetMachineFingerprint(char* outputBuffer, int bufferSize)
    {
        std::string rawHardwareString = "";

        // 1. Hard Drive Volume Serial Number (C:)
        DWORD volumeSerialNumber = 0;
        if (GetVolumeInformationA("C:\\", NULL, 0, &volumeSerialNumber, NULL, NULL, NULL, 0)) {
            rawHardwareString += std::to_string(volumeSerialNumber) + "|";
        }

        // 2. Active Computer Name
        char computerName[MAX_COMPUTERNAME_LENGTH + 1];
        DWORD size = sizeof(computerName);
        if (GetComputerNameA(computerName, &size)) {
            rawHardwareString += std::string(computerName) + "|";
        }

        // 3. MAC Address of the Primary Network Interface
        ULONG outBufLen = sizeof(IP_ADAPTER_INFO);
        std::vector<BYTE> pAdapterInfoBytes(outBufLen);
        PIP_ADAPTER_INFO pAdapterInfo = reinterpret_cast<PIP_ADAPTER_INFO>(pAdapterInfoBytes.data());

        // Call GetAdaptersInfo once to get the exact size needed
        if (GetAdaptersInfo(pAdapterInfo, &outBufLen) == ERROR_BUFFER_OVERFLOW) {
            pAdapterInfoBytes.resize(outBufLen);
            pAdapterInfo = reinterpret_cast<PIP_ADAPTER_INFO>(pAdapterInfoBytes.data());
        }

        // Call again with the correctly sized buffer
        if (GetAdaptersInfo(pAdapterInfo, &outBufLen) == NO_ERROR) {
            PIP_ADAPTER_INFO pAdapter = pAdapterInfo;
            while (pAdapter) {
                // Grab the first adapter that looks like standard Ethernet/Wi-Fi
                if (pAdapter->Type == MIB_IF_TYPE_ETHERNET || pAdapter->Type == 71 /* 802.11 */) {
                    for (UINT i = 0; i < pAdapter->AddressLength; i++) {
                        char szMac[4];
                        sprintf_s(szMac, "%02X", pAdapter->Address[i]);
                        rawHardwareString += szMac;
                    }
                    break;
                }
                pAdapter = pAdapter->Next;
            }
        }

        // Generate a 16-character hardware fingerprint (XX-XX format wouldn't hurt but let's do XXXXXXXX-XXXXXXXX)
        std::string part1 = HashData(rawHardwareString + "RCS_SALT_1");
        std::string part2 = HashData(rawHardwareString + "RCS_SALT_2");
        std::string finalFingerprint = part1 + "-" + part2;

        if (finalFingerprint.length() < bufferSize) {
            strcpy_s(outputBuffer, bufferSize, finalFingerprint.c_str());
            return 1; // Success
        }

        return 0; // Fail buffer size
    }

    // Existing "Poison Pill" function
    __declspec(dllexport) int CalculateSecretCurveTolerance(int inputSeed, char* outputBuffer, int bufferSize)
    {
        std::string secret = "VALID_KEY_XYZ_123_" + std::to_string(inputSeed * 2);
        if (secret.length() < bufferSize) {
            strcpy_s(outputBuffer, bufferSize, secret.c_str());
            return 1; // Success
        }
        return 0; // Fail
    }

    // New Telemetry Security Extractor
    // Returns our Error Reporting back-end URL and Token
    // We construct the string dynamically via arithmetic to avoid plain text scraping
    __declspec(dllexport) int GetTelemetryEndpoint(char* outputBuffer, int bufferSize)
    {
        // The target URL is: "https://api.rcscogo.com/telemetry/v1/ingest?token=sk_live_rcs_f71k2h1"
        const int len = 71;
        if (bufferSize <= len) return 0;

        // XOR obfuscation key: 0x3F
        unsigned char obf[] = {
            0x57, 0x4B, 0x4B, 0x4F, 0x4C, 0x05, 0x10, 0x10, 0x5E, 0x4F,
            0x56, 0x11, 0x4D, 0x5C, 0x4C, 0x5C, 0x50, 0x58, 0x50, 0x11,
            0x5C, 0x50, 0x52, 0x10, 0x4B, 0x5A, 0x53, 0x5A, 0x52, 0x5A,
            0x4B, 0x4D, 0x46, 0x10, 0x49, 0x0E, 0x10, 0x56, 0x51, 0x58,
            0x5A, 0x4C, 0x4B, 0x00, 0x4B, 0x50, 0x54, 0x5A, 0x51, 0x02,
            0x4C, 0x54, 0x60, 0x53, 0x56, 0x49, 0x5A, 0x60, 0x4D, 0x5C,
            0x4C, 0x60, 0x59, 0x08, 0x0E, 0x54, 0x0D, 0x57, 0x0E, 0x47, 0x46
        };

        for (int i = 0; i < len; ++i) {
            outputBuffer[i] = (char)(obf[i] ^ 0x3F);
        }
        outputBuffer[len] = '\0';
        return 1;
    }
}
