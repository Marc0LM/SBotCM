#pragma once

#include <windows.h>

#include "IMemoryReader.h"

class MemoryReaderFromProcess : public IMemoryReader {
 public:
  HANDLE handle_;

  MemoryReaderFromProcess(uint32_t pid) { handle_ = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid); }
  std::shared_ptr<IByteArray> ReadBytes(Address start_address, Size length) {
    if (length == 0) return nullptr;
    std::shared_ptr<IByteArray> buf = std::make_shared<BufferedByteArray>(length);
    if (!ReadProcessMemory(handle_, (LPCVOID)start_address, buf->Raw(), length, nullptr)) {
      return nullptr;
    }
    return buf;
  }
  ~MemoryReaderFromProcess() { CloseHandle(handle_); }
};
