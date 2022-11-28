#pragma once

#include <cstdint>
#include <functional>

typedef uint64_t Address;
typedef uint64_t Size;
typedef uint8_t Byte;

template <typename T>
class IArray {
 public:
  virtual T* Raw() = 0;
  virtual T& operator[](uint64_t idx) = 0;
  virtual Size Count() = 0;
  virtual bool Contains(std::function<bool(T)> predict) = 0;
  virtual bool Contains(T t) = 0;
  virtual ~IArray() = default;
};
typedef IArray<Byte> IByteArray;

class BufferedByteArray : public IByteArray {
  std::vector<Byte> data_;

 public:
  BufferedByteArray(Size size) { data_.resize(size); }
  Byte* Raw() { return &data_[0]; }
  Byte& operator[](uint64_t idx) { return data_[idx]; }
  Size Count() { return data_.size(); }
  bool Contains(std::function<bool(Byte)> predict) {
    bool pred = false;
    std::for_each(data_.begin(), data_.end(), [&](Byte t) {
      if (predict(t)) pred = true;
    });
    return pred;
  }
  bool Contains(Byte t) { return data_.end() != std::find(data_.begin(), data_.end(), t); }
};

class InMemoryByteArray : public IByteArray {
  Byte* data_;
  Size length_;

 public:
  InMemoryByteArray(Byte* t, Size size) : data_(t), length_(size) {}
  Byte* Raw() { return data_; }
  Byte& operator[](uint64_t idx) { return data_[idx]; }
  Size Count() { return length_; }
  bool Contains(std::function<bool(Byte)> predict) {
    bool pred = false;
    for (int i = 0; i < length_; i++) {
      if (predict(data_[i])) pred = true;
    }
    return pred;
  }
  bool Contains(Byte t) {
    bool pred = false;
    for (int i = 0; i < length_; i++) {
      if (data_[i] == t) pred = true;
    }
    return pred;
  }
};

struct MemoryRegion {
  Address base_address;
  Size length;
};
struct PyDictEntry {
  uint64_t hash;
  uint64_t key;
  uint64_t value;
};
