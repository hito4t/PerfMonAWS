#include "stdafx.h"


extern "C" {

	static PDH_HQUERY hQuery = INVALID_HANDLE_VALUE;
	static PDH_HCOUNTER hCpuCounter = INVALID_HANDLE_VALUE;
	static PDH_HCOUNTER hMemoryCounter = INVALID_HANDLE_VALUE;
	static TCHAR lastError[512];

	BOOL HandleError(LPCTSTR operation, PDH_STATUS result) {
		// Error codes are defined in the following site.
		// https://docs.microsoft.com/en-us/windows/win32/perfctrs/pdh-error-codes

		_stprintf_s(lastError, L"%s failed : %08X", operation, result);
		_tprintf(lastError);
		_tprintf(L"\r\n");
		return FALSE;
	}

	__declspec(dllexport) void __stdcall PdhGetLastError(LPTSTR buffer, size_t bufferSize)
	{
		_tcscpy_s(buffer, bufferSize, lastError);
	}

	__declspec(dllexport) BOOL __stdcall PdhOpen()
	{
		PDH_STATUS result;

		result = PdhOpenQuery(NULL, 0, &hQuery);
		if (result != ERROR_SUCCESS) {
			return HandleError(L"PdhOpenQuery", result);
		}

		result = PdhAddCounter(hQuery, L"\\Processor(_Total)\\% Processor Time", 0, &hCpuCounter);
		if (result != ERROR_SUCCESS) {
			return HandleError(L"PdhAddCounter(Processor)", result);
		}

		result = PdhAddCounter(hQuery, L"\\Memory\\Available MBytes", 0, &hMemoryCounter);
		if (result != ERROR_SUCCESS) {
			return HandleError(L"PdhAddCounter(Memory)", result);
		}

		// Need to call PdhCollectQueryData before calling PdhGetFormattedCounterValue.
		result = PdhCollectQueryData(hQuery);
		if (result != ERROR_SUCCESS) {
			return HandleError(L"PdhCollectQueryData", result);
		}

		return TRUE;
	}

	__declspec(dllexport) BOOL __stdcall PdhGetValues(double* processorUtilization, double* availableMemoryMB,
		LPTSTR activeProcess, size_t activeProcessSize)
	{
		PDH_FMT_COUNTERVALUE value;
		PDH_STATUS result;

		activeProcess[0] = '\0';
		HWND hWnd = GetForegroundWindow();
		if (hWnd != NULL) {
			DWORD processId;
			GetWindowThreadProcessId(hWnd, &processId);
			HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId);
			if (hProcess != INVALID_HANDLE_VALUE) {
				GetProcessImageFileName(hProcess, activeProcess, activeProcessSize);
			}
		}

		result = PdhCollectQueryData(hQuery);
		if (result != ERROR_SUCCESS) {
			return HandleError(L"PdhCollectQueryData", result);
		}

		result = PdhGetFormattedCounterValue(hCpuCounter, PDH_FMT_DOUBLE, NULL, &value);
		if (result != ERROR_SUCCESS) {
			return HandleError(L"PdhGetFormattedCounterValue(Processor)", result);
		}
		*processorUtilization = value.doubleValue;

		result = PdhGetFormattedCounterValue(hMemoryCounter, PDH_FMT_DOUBLE, NULL, &value);
		if (result != ERROR_SUCCESS) {
			return HandleError(L"PdhGetFormattedCounterValue(Memory)", result);
		}
		*availableMemoryMB = value.doubleValue;

		return TRUE;
	}

	__declspec(dllexport) void __stdcall PdhClose()
	{
		PDH_STATUS result;

		if (hQuery != INVALID_HANDLE_VALUE) {
			result = PdhCloseQuery(hQuery);
			hQuery = INVALID_HANDLE_VALUE;
		}
	}
}
