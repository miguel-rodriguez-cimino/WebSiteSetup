// Bootstrapper.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

using namespace std;

INT Launch(
	__in PCWSTR ApplicationName,
	__in PCWSTR CommandLine,
	__in BOOL Wait
	)
{
	SHELLEXECUTEINFO Shex;
	ZeroMemory(&Shex, sizeof(SHELLEXECUTEINFO));
	Shex.cbSize = sizeof(SHELLEXECUTEINFO);
	Shex.fMask = SEE_MASK_FLAG_NO_UI | SEE_MASK_NOCLOSEPROCESS;
	Shex.lpVerb = L"runas";
	Shex.lpFile = ApplicationName;
	Shex.lpParameters = CommandLine;
	Shex.nShow = SW_SHOWNORMAL;

	if (!ShellExecuteEx(&Shex))
	{
		DWORD Err = GetLastError();
		return EXIT_FAILURE;
	}

	_ASSERTE(Shex.hProcess);

	if (Wait)
	{
		WaitForSingleObject(Shex.hProcess, INFINITE);
	}
	CloseHandle(Shex.hProcess);
	return EXIT_SUCCESS;
}

int APIENTRY _tWinMain(_In_ HINSTANCE hInstance,
	_In_opt_ HINSTANCE hPrevInstance,
	_In_ LPTSTR    lpCmdLine,
	_In_ int       nCmdShow)
{
	HANDLE tempFile = INVALID_HANDLE_VALUE;
	HANDLE logFile = INVALID_HANDLE_VALUE;
	TCHAR tempFilePath[MAX_PATH];
	TCHAR tempFileName[MAX_PATH];
	TCHAR localPath[MAX_PATH];
	TCHAR drive[3];
	TCHAR directory[MAX_PATH];
	TCHAR logPath[MAX_PATH] = { 0 };
	DWORD retVal = 0;
	UINT uRetVal = 0;
	DWORD dwBytesWritten = 0;
	WCHAR CommandLineBuffer[260] = { 0 };
	BOOL logEnabled = FALSE;

	// Get path for extracting the MSI
	retVal = GetTempPath(MAX_PATH, tempFilePath);
	uRetVal = GetTempFileName(tempFilePath, TEXT("TMP"), 0, tempFileName);

	// Get path for log file
	GetModuleFileName(NULL, localPath, MAX_PATH);
	_wsplitpath_s(localPath, drive, 3, directory, MAX_PATH, NULL, 0, NULL, 0);
	StringCchCat(logPath, _countof(logPath), drive);
	StringCchCat(logPath, _countof(logPath), directory);
	StringCchCat(logPath, _countof(logPath), L"log.txt");

	// Check if log is enabled
	if (lstrcmpi(lpCmdLine, L"/l") == 0)
	{
		logEnabled = TRUE;

		logFile = CreateFile(logPath, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		WriteFile(logFile, L"Resource Extracted to: ", 46, &dwBytesWritten, NULL);
		WriteFile(logFile, tempFileName, _tcslen(tempFileName) * 2, &dwBytesWritten, NULL);
		WriteFile(logFile, L"\r\n", 4, &dwBytesWritten, NULL);
		CloseHandle(logFile);
	}

	// Get resource
	TCHAR resNumber[5] = _T("#101");
	TCHAR resType[4] = _T("MSI");
	HRSRC hres = FindResource(NULL, resNumber, resType);
	HGLOBAL hBytes = LoadResource(NULL, hres);
	char* msiData = (char*)LockResource(hBytes);
	SIZE_T msiSize = SizeofResource(NULL, hres);

	// Write the MSI to temp folder
	tempFile = CreateFile(tempFileName, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

	if (logEnabled && tempFile == INVALID_HANDLE_VALUE)
	{
		logFile = CreateFile(logPath, FILE_APPEND_DATA, 0, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		WriteFile(logFile, L"Falied to write temp file", 50, &dwBytesWritten, NULL);
		CloseHandle(logFile);
		return 1;
	}
	else
	{
		WriteFile(tempFile, msiData, msiSize, &dwBytesWritten, NULL);
		CloseHandle(tempFile);
	}

	UnlockResource(hBytes);

	// Build the execution string
	StringCchCat(CommandLineBuffer, _countof(CommandLineBuffer), L"/i ");
	StringCchCat(CommandLineBuffer, _countof(CommandLineBuffer), tempFileName);

	if (logEnabled)
	{
		StringCchCat(CommandLineBuffer, _countof(CommandLineBuffer), L" /l+*v ");
		StringCchCat(CommandLineBuffer, _countof(CommandLineBuffer), logPath);
	}

	// Launch installer and wait
	Launch(L"msiexec", CommandLineBuffer, TRUE);

	// Delete temp file
	DeleteFile(tempFileName);

	return 0;
}