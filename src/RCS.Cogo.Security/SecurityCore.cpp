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
}
