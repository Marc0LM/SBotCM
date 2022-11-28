#pragma once

#include "types.h"

class IMemoryReader {
 public:
  std::map<Address, Size> mrgs;
  virtual std::shared_ptr<IByteArray> ReadBytes(Address startaddress, Size length) = 0;
  virtual ~IMemoryReader(){};
};
