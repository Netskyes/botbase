// Hooks.cpp : Defines the exported functions for the DLL application.
//
#define NOMINMAX
#include "stdafx.h"
#include "asmjit/asmjit.h"

using namespace asmjit;

LPVOID connectHookTramp;
LPVOID sendHookTramp;

sockaddr* __stdcall _hookConnect(SOCKET sock, sockaddr* sa)
{
	sockaddr* sockAddrNew = new sockaddr;
	*sockAddrNew = *sa;

	sockaddr_in* s = reinterpret_cast<sockaddr_in*>(sockAddrNew);
	std::cout << "Connect -> " << sock << " < " << inet_ntoa(s->sin_addr) << ":" << ntohs(s->sin_port) << std::endl;

	std::string hostname = inet_ntoa(s->sin_addr);

	if (hostname != "127.0.0.1")
	{
		((sockaddr_in*)sockAddrNew)->sin_port = htons(8089);
		((sockaddr_in*)sockAddrNew)->sin_addr.s_addr = inet_addr("127.0.0.1");
	}

	return sockAddrNew;
}

void HookFunc(LPCWSTR moduleName, LPCSTR funcName, CodeHolder& hookCode, LPVOID trampoline, const int jmpLen)
{
	DWORD64 funcAddr = (DWORD64)GetProcAddress(GetModuleHandle(moduleName), funcName);
	BYTE trampBytes[128];
	CodeHolder jmpCode, trampCode;

	DWORD prev;
	VirtualProtect((LPVOID)funcAddr, jmpLen, PAGE_EXECUTE_READWRITE, &prev);
	
	// Assemble trampoline
	trampCode.init(CodeInfo(ArchInfo::kIdX64));
	x86::Assembler trampAsm(&trampCode);
	trampAsm.push(x86::rax);
	trampAsm.mov(x86::rax, funcAddr + 12);
	trampAsm.jmp(x86::rax);
	
	size_t trampLen = trampCode.codeSize() + hookCode.codeSize() + jmpLen;
	// Allocate memory for trampoline
	trampoline = VirtualAlloc(NULL, trampLen, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

	memcpy(trampBytes, hookCode.sectionById(0)->buffer().data(), hookCode.codeSize());
	memcpy(&trampBytes[hookCode.codeSize()], (LPVOID)funcAddr, jmpLen);
	memcpy(&trampBytes[hookCode.codeSize() + jmpLen], trampCode.sectionById(0)->buffer().data(), trampCode.codeSize());

	// Build trampoline [Func Hook + Original Bytes + (Return + Offset)]
	memcpy(trampoline, trampBytes, trampLen);

	// Assemble jump
	jmpCode.init(CodeInfo(ArchInfo::kIdX64));
	x86::Assembler a(&jmpCode);
	a.mov(x86::rax, (DWORD64)trampoline);
	a.jmp(x86::rax);
	a.pop(x86::rax);
	
	// Patch jump
	memcpy((LPVOID)funcAddr, jmpCode.sectionById(0)->buffer().data(), jmpLen);
	
	// Fill difference with nops
	if (jmpLen > jmpCode.codeSize())
	{
		for (int i = 0; i < (jmpLen - jmpCode.codeSize()); i++)
		{
			memset((LPVOID)((funcAddr + jmpCode.codeSize()) + i), 0x90, 1);
		}
	}
	
	// Restore permissions
	VirtualProtect((LPVOID)funcAddr, jmpLen, prev, &prev);

	std::cout << "Function: " << funcName << " hooked!" << std::endl;
}

void WINAPI Entry()
{
	CodeHolder hookConnectCode;
	hookConnectCode.init(CodeInfo(ArchInfo::kIdX64));

	x86::Assembler hcp(&hookConnectCode);
	hcp.push(x86::rcx);
	hcp.push(x86::rdx);
	hcp.push(x86::r8);
	hcp.push(x86::r9);
	hcp.push(x86::r10);
	hcp.push(x86::r11);
	hcp.pushfq();
	
	hcp.mov(x86::rax, (DWORD64)_hookConnect);
	hcp.call(x86::rax);
	hcp.push(x86::rax);
	hcp.pop(x86::rdx);

	hcp.popfq();
	hcp.pop(x86::r11);
	hcp.pop(x86::r10);
	hcp.pop(x86::r9);
	hcp.pop(x86::r8);
	hcp.add(x86::rsp, 8);
	hcp.pop(x86::rcx);

	HookFunc(L"ws2_32.dll", "connect", hookConnectCode, &connectHookTramp, 15);
	//HookFunc(L"ws2_32.dll", "send", hookConnectCode, &sendHookTramp, 15);
}