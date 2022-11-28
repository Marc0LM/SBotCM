#pragma once

#include <windows.h>

#include "IMemoryReader.h"

class MemoryReaderInProcess  //: public IMemoryReader {
{
 public:
  std::shared_ptr<IByteArray> ReadBytes(Address start_address, Size length) {
    if (length == 0) return nullptr;
    std::shared_ptr<IByteArray> buf = std::make_shared<InMemoryByteArray>((Byte*)start_address, length);
    return buf;
  }
};
