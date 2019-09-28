// Hooks.cpp : Defines the exported functions for the DLL application.
//
#define NOMINMAX
#include "stdafx.h"
#include "asmjit/asmjit.h"

using namespace asmjit;

typedef int(*Func)(void);

JitRuntime rt;

LPVOID connectHook;
LPVOID connectHookTramp;

DWORD64 sockBackup;
DWORD64 sockAddrBackup;

void _hookConnect()
{
	std::cout << "WTF??" << std::endl;
}

void HookFunc(LPCWSTR moduleName, LPCSTR funcName, CodeHolder& hookCode, LPVOID trampoline, const int jmpLen)
{
	DWORD64 funcAddr = (DWORD64)GetProcAddress(GetModuleHandle(moduleName), funcName);
	DWORD prev;
	VirtualProtect((LPVOID)funcAddr, jmpLen, PAGE_EXECUTE_READWRITE, &prev);
	BYTE trampBytes[128];

	CodeHolder jmpCode, trampCode;
	jmpCode.init(rt.codeInfo());
	trampCode.init(rt.codeInfo());
	
	// Trampoline code
	x86::Assembler t(&trampCode);
	t.push(x86::rax);
	t.mov(x86::rax, funcAddr + 12);
	t.jmp(x86::rax);
	
	size_t trampLen = trampCode.codeSize() + hookCode.codeSize() + jmpLen;
	// Allocate memory for trampoline
	trampoline = VirtualAlloc(NULL, trampLen, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

	// Jump code
	x86::Assembler a(&jmpCode);
	a.mov(x86::rax, (DWORD64)trampoline);
	a.jmp(x86::rax);
	a.pop(x86::rax);

	Func asmJump, asmTramp, asmHook;

	Error error =+ rt.add(&asmJump, &jmpCode);
	error =+ rt.add(&asmTramp, &trampCode);
	error =+ rt.add(&asmHook, &hookCode);

	if (error) 
	{
		std::cout << "An error occured" << std::endl;
		return;
	}

	// Copy hook func
	memcpy(trampBytes, (LPVOID)asmHook, hookCode.codeSize());
	// Copy original bytes
	memcpy(&trampBytes[hookCode.codeSize()], (LPVOID)funcAddr, jmpLen);
	// Copy trampoline jump
	memcpy(&trampBytes[hookCode.codeSize() + jmpLen], (LPVOID)asmTramp, trampCode.codeSize());
	// Build trampoline [Func Hook + Original Bytes + (Return + Offset)]
	memcpy(trampoline, trampBytes, trampLen);

	// Patch jump
	memcpy((LPVOID)funcAddr, asmJump, jmpLen);
	
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
	hookConnectCode.init(rt.codeInfo());

	x86::Assembler hcp(&hookConnectCode);
	hcp.nop();
	hcp.nop();

	HookFunc(L"ws2_32.dll", "connect", hookConnectCode, &connectHookTramp, 15);
}