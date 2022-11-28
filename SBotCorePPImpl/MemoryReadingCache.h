#pragma once

#include <algorithm>
#include <any>
#include <functional>
#include <map>
#include <string>

#include "types.h"

namespace EveMemoryReading {
class MemoryReadingCache {
  std::unordered_map<Address, std::string> python_type_name_from_python_object_address_;
  std::unordered_map<Address, std::string> python_string_value_max_length_4000_;
  std::unordered_map<Address, std::any> dict_entry_value_representation_;

 public:
  MemoryReadingCache() {
    python_type_name_from_python_object_address_ = std::unordered_map<Address, std::string>();
    python_string_value_max_length_4000_ = std::unordered_map<Address, std::string>();
    dict_entry_value_representation_ = std::unordered_map<Address, std::any>();
  }
  template <typename TValue>
  TValue GetOrUpdate(Address key, std::function<TValue(Address)> getFresh, std::unordered_map<Address, TValue>& c) {
    if (c.find(key) != c.end()) return c.find(key)->second;

    auto fresh = getFresh(key);
    c[key] = fresh;
    return fresh;
  }
  std::string GetPythonTypeNameFromPythonObjectAddress(Address address, std::function<std::string(Address)> getFresh) {
    return GetOrUpdate(address, getFresh, python_type_name_from_python_object_address_);
  }

  std::string GetPythonStringValueMaxLength4000(Address address, std::function<std::string(Address)> getFresh) {
    return GetOrUpdate(address, getFresh, python_string_value_max_length_4000_);
  }
  std::any GetDictEntryValueRepresentation(Address address, std::function<std::any(Address)> getFresh) {
    return GetOrUpdate(address, getFresh, dict_entry_value_representation_);
  }
};
}  // namespace EveMemoryReading
