#pragma once

#include <codecvt>
#include <locale>
#include <string>

#include "types.h"

class Convert {
 public:
  static int64_t ToInt64(void* src, int64_t offset) { return *(int64_t*)((char*)src + offset); }
  static uint64_t ToUInt64(void* src, int64_t offset) { return *(uint64_t*)((char*)src + offset); }
  static double ToDouble(void* src, int64_t offset) { return *(double*)((char*)src + offset); }
  static std::vector<uint64_t> AsUInt64Array(std::shared_ptr<IByteArray> src) {
    std::vector<uint64_t> res(src->Count() / 8);
    memcpy(&res[0], src->Raw(), src->Count());
    return res;
  }
  static std::vector<int64_t> AsInt64Array(std::shared_ptr<IByteArray> src) {
    std::vector<int64_t> res(src->Count() / 8);
    memcpy(&res[0], src->Raw(), src->Count());
    return res;
  }
  static std::string AsASCIIString(std::shared_ptr<IByteArray> src) {
    if (!src->Contains(0)) return "";
    std::string res;

    for (int i = 0; i < src->Count(); i++) {
      auto& c = (*src)[i];
      if (c <= 0) break;
      res.push_back(c);
    }
    return res;
  }
  static std::wstring ToWString(std::string s) {
    std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
    return converter.from_bytes(s);
  }
  static std::string ToString(std::wstring s) {
    std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
    return converter.to_bytes(s);
  }
  static std::string AnyStringToBytes(std::any in) {
    if (in.type() == typeid(std::string)) {
      return std::string("0") + std::any_cast<std::string>(in);
    }
    if (in.type() == typeid(std::wstring)) {
      return std::string("1") + std::string((char*)std::any_cast<std::wstring>(in).c_str(), std::any_cast<std::wstring>(in).length() * 2);
    }
    return "2";
  }
};
