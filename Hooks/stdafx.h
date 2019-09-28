#pragma once
#pragma comment(lib, "Ws2_32.lib")

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define _CRT_SECURE_NO_DEPRECATE
#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <windows.h>
#include <WinSock2.h>
#include <TlHelp32.h>
#include <winternl.h>

#include <fstream>
#include <sstream>
#include <iostream>
#include <string>
#include <typeinfo>
#include <vector>
#include <thread>
#include <mutex>