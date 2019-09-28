#include "pch.h"
#include <iostream>
#include <stdio.h>
#include <tchar.h>
#include <stdlib.h>
#include <sstream>

HANDLE GetProcessByName(const wchar_t* procName)
{
	DWORD pid = 0;
	PROCESSENTRY32 entry;
	entry.dwSize = sizeof(PROCESSENTRY32);

	HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, NULL);

	if (Process32First(snapshot, &entry))
	{
		do
		{
			if (_tcscmp(entry.szExeFile, procName) == 0)
			{
				pid = entry.th32ProcessID;
				break;
			}

		} while (Process32Next(snapshot, &entry));
	}

	return (pid != 0) ? OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid) : NULL;
}

wchar_t* charToWChar(const char* text)
{
	const size_t size = strlen(text) + 1;
	wchar_t* wText = new wchar_t[size];
	mbstowcs(wText, text, size);

	return wText;
}

void EnableDebugPriv(HANDLE hProc)
{
	HANDLE hToken;
	LUID luid;
	TOKEN_PRIVILEGES tokenPriv;

	OpenProcessToken(hProc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken);
	LookupPrivilegeValue(NULL, SE_DEBUG_NAME, &luid);
	tokenPriv.PrivilegeCount = 1;
	tokenPriv.Privileges[0].Luid = luid;
	tokenPriv.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
	AdjustTokenPrivileges(hToken, FALSE, &tokenPriv, sizeof(tokenPriv), NULL, NULL);
	CloseHandle(hToken);
}

bool Inject(const char* procName, const char* dll)
{
	HANDLE procHandle = GetProcessByName(charToWChar(procName));

	if (!procHandle)
	{
		std::cout << "Process handle is null" << std::endl;
		return FALSE;
	}


	DWORD64 LLAddress = (DWORD64)GetProcAddress(GetModuleHandle(L"kernel32.dll"), "LoadLibraryA");
	PVOID memory = VirtualAllocEx(procHandle, NULL, strlen(dll) + 1, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

	WriteProcessMemory(procHandle, memory, dll, strlen(dll) + 1, NULL);


	if (!CreateRemoteThread(procHandle, NULL, NULL, (LPTHREAD_START_ROUTINE)LLAddress, memory, NULL, NULL))
	{
		std::cout << "Failed to create thread" << std::endl;
		return FALSE;
	}


	CloseHandle(procHandle);
	VirtualFreeEx(procHandle, memory, 0, MEM_FREE | MEM_COMMIT);

	return 1;
}


int main()
{
	while (true) 
	{
		std::string procName;
		std::cout << "Process name: ";
		std::cin >> procName;

		bool result = Inject(procName.c_str(), "D:/SoftwareDev/CSharp/BotBase/x64/Debug/Hooks.dll");
		if (result)
		{
			std::cout << "Injected successfully" << std::endl;
		}
	}

	return 0;
}