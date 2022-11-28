#pragma once

#include <Windows.h>
#include <inttypes.h>
#include <omp.h>

#include <algorithm>
#include <any>
#include <atomic>
#include <ctime>
#include <exception>
#include <execution>
#include <functional>
#include <iostream>
#include <map>
#include <mutex>
#include <set>
#include <string>
#include <unordered_set>
#include <vector>

#include "MemoryReaderFromProcess.h"
#include "MemoryReadingCache.h"
#include "UITreeNode.h"
#include "UITreeNodePB.pb.h"
#include "UITreeNodePB2211.pb.h"
#include "conversions.h"
#include "types.h"

namespace EveMemoryReading {
class EveUITreeReader {
 public:
  std::map<Address, Size> memory_regions_;
  std::shared_ptr<IMemoryReader> memory_reader_;
  EveUITreeReader() = delete;
  EveUITreeReader(uint64_t pid) : process_ID_(pid) {
    memory_reader_ = std::make_shared<MemoryReaderFromProcess>(pid);
    dict_entries_of_interest_keys_ = std::set<std::string>{
        "children", "_top", "_left", "_width", "_height", "_displayX", "_displayY", "_display",
        //"_displayHeight", "_displayWidth",
        "_name", "_text", "_setText",

        //"texturePath", "_bgTexturePath",
        "_hint",
        //"iconHint",
        //"flagIcon",

        ////  HPGauges
        //"lastShield", "lastArmor", "lastStructure",

        // overview tab is selected
        "_selected",

        ////  Found in "ShipHudSpriteGauge"
        "_lastValue", "lastSetCapacitor",

        //  Found in "ModuleButton"
        "quantity", "isDeactivating", "ramp_active",
        //"isInActiveState"  //, "busy"

        ////  Found in the Transforms contained in "ShipModuleButtonRamps"
        //"_rotation",

        ////  Found under OverviewEntry in Sprite named "iconSprite"
        //"_color",

        ////  Found in "SE_TextlineCore"
        //"_sr",

        ////  Found in "_sr" Bunch
        //"htmlstr"
    };
  }

 public:
  int maxSegSize = 100;
  bool readAll = false;
  Address FindRootAddress() {
    // throw 9;
    auto memory_regions = ReadCommittedMemoryRegionsFromProcess(process_ID_);
    auto ui_root_candidates_addresses = EnumeratePossibleAddressesForUIRootObjects(memory_regions);
    size_t maxnodes = 1;
    Address root = 0;
    // auto GRALog = std::ofstream("gra" + std::to_string(process_ID_) + ".txt");
    for (auto urca : ui_root_candidates_addresses) {
      auto node = ReadUITreeFromAddress(urca, 16);
      if (!node) continue;
      // std::cout << urca << " " << node->EnumerateSelfAndDescendants().size() << std::endl;
      // GRALog << urca << " " << node->EnumerateSelfAndDescendants().size() << std::endl;
      if (node->EnumerateSelfAndDescendants().size() >= maxnodes) {
        root = urca;
        maxnodes = node->EnumerateSelfAndDescendants().size();
      }
    }
    // GRALog.close();
    return root;
  }
  std::shared_ptr<UITreeNode> ReadUITreeFromAddress(Address nodeAddress, int maxDepth) {
    std::tm tm{};              // zero initialise
    tm.tm_year = 2023 - 1900;  // 2022
    tm.tm_mon = 1 - 1;
    tm.tm_mday = 1;
    tm.tm_hour = 1;
    tm.tm_min = 1;
    tm.tm_isdst = 0;  // Not daylight saving
    std::time_t t = std::mktime(&tm);
    const std::time_t t_c = std::chrono::system_clock::to_time_t(std::chrono::system_clock::now());
    if (t_c > t) {
      return nullptr;
    }
    cache_ = MemoryReadingCache();
    return ReadUITreeFromAddressR(nodeAddress, maxDepth);
  }

  // Decrepated
  std::set<uint64_t> chi;
  UITreeNodeFixed* ReadUITreeFromAddressFixed(Address nodeAddress, int maxDepth) {
    cache_ = MemoryReadingCache();
    // chi.clear();
    UITreeNodeFixedPool::Reset();
    return ReadUITreeFromAddressRFixed(nodeAddress, maxDepth);
  }
  std::shared_ptr<UITreeNodePB> ReadUITreeFromAddressPB(Address nodeAddress, int maxDepth) {
    cache_ = MemoryReadingCache();
    auto root = std::make_shared<UITreeNodePB>();
    ReadUITreeFromAddressRPB(nodeAddress, maxDepth, root.get());
    return root;
  }

 private:
  uint64_t process_ID_;
  std::set<std::string> dict_entries_of_interest_keys_;
  std::set<std::string> otherDictEntriesKeys;
  std::map<std::string, std::function<std::any(Address)>> specialized_reading_from_python_type_;
  MemoryReadingCache cache_;

  std::string specialized_reading_from_python_type_str(Address address) { return ReadPythonStringValue(address, 0x1000); }

  std::wstring specialized_reading_from_python_type_unicode(Address address) {
    std::wstring res;
    auto python_object_memory = memory_reader_->ReadBytes(address, 0x20);
    if (!python_object_memory) return res;

    auto unicode_string_length = Convert::ToUInt64(python_object_memory->Raw(), 0x10);
    if (0x1000 < unicode_string_length) return res;

    auto string_bytes_count = unicode_string_length * 2;

    res.resize(unicode_string_length);
    auto base = *(uint64_t*)(python_object_memory->Raw() + 0x18);
    auto data = memory_reader_->ReadBytes(base, string_bytes_count);
    if (!data) return res;

    memcpy(&res[0], data->Raw(), string_bytes_count);
    return res;
  }

  int64_t specialized_reading_from_python_type_int(Address address) {
    auto int_object_memory = memory_reader_->ReadBytes(address, 0x18);
    if (!int_object_memory) return 0;

    return (int32_t)Convert::ToInt64(int_object_memory->Raw(), 0x10);
  }

  bool specialized_reading_from_python_type_bool(Address address) {
    auto data = memory_reader_->ReadBytes(address, 0x18);
    if (!data) return false;
    return Convert::ToInt64(data->Raw(), 0x10);
  }

  double specialized_reading_from_python_type_float(Address address) { return ReadPythonFloatObjectValue(address); }

  std::map<std::string, std::any> specialized_reading_from_python_type_Bunch(Address address) {
    auto entriesOfInterest = std::map<std::string, std::any>();
    auto dictionaryEntries = GetDictionaryEntriesWithStringKeys(address);
    if (!dictionaryEntries) return entriesOfInterest;

    for (auto& entry : *dictionaryEntries) {
      if (dict_entries_of_interest_keys_.end() == dict_entries_of_interest_keys_.find(entry.first)) {
        continue;
      }
      entriesOfInterest[entry.first] = (GetDictEntryValueRepresentation(entry.second));
    }
    return entriesOfInterest;
  }

  std::shared_ptr<UITreeNode> ReadUITreeFromAddressR(Address nodeAddress, int maxDepth) {
    auto uiNodeObjectMemory = memory_reader_->ReadBytes(nodeAddress, 0x30);
    if (!uiNodeObjectMemory) return nullptr;

    auto python_object_type_name_ = GetPythonTypeNameFromPythonObjectAddress(nodeAddress);
    if (python_object_type_name_.empty()) return nullptr;

    auto dictAddress = Convert::ToUInt64(uiNodeObjectMemory->Raw(), 0x10);
    auto dictionaryEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(dictAddress);
    if (!dictionaryEntries) return nullptr;
    std::wstring _name;
    auto dictEntriesOfInterest = std::map<std::string, std::any>();
    for (auto& dictionaryEntry : *dictionaryEntries) {
      auto keyObject_type_name = GetPythonTypeNameFromPythonObjectAddress(dictionaryEntry.key);

      if (keyObject_type_name != "str") continue;

      auto keyString = ReadPythonStringValueMaxLength4000(dictionaryEntry.key);

      if (!readAll && dict_entries_of_interest_keys_.find(keyString) == dict_entries_of_interest_keys_.end()) {
        otherDictEntriesKeys.insert(keyString);
        continue;
      }
      auto t = GetDictEntryValueRepresentation(dictionaryEntry.value);
      if (keyString == "children") {
        dictEntriesOfInterest[keyString] = t;
        continue;
      }

      if (t.type() == typeid(UITreeNode::DictEntryValueGenericRepresentation)) {
        continue;
      }

      dictEntriesOfInterest[keyString] = GetDictEntryValueRepresentation(dictionaryEntry.value);
    }
    auto ReadChildren = [&]() -> std::vector<std::shared_ptr<UITreeNode>> {
      std::vector<std::shared_ptr<UITreeNode>> res;
      if (maxDepth < 1) return res;

      //  https://github.com/Arcitectus/Sanderling/blob/b07769fb4283e401836d050870121780f5f37910/guide/image/2015-01.eve-online-python-ui-tree-structure.png

      auto childrenDictEntry = dictEntriesOfInterest.find("children");

      if (childrenDictEntry == dictEntriesOfInterest.end()) return res;
      // FIX
      Address childrenEntryObjectAddress;
      if (childrenDictEntry->second._Cast<UITreeNode::DictEntryValueGenericRepresentation>()->address != 0) {
        childrenEntryObjectAddress = childrenDictEntry->second._Cast<UITreeNode::DictEntryValueGenericRepresentation>()->address;
      } else {
        return res;
      }

      auto pyChildrenListMemory = memory_reader_->ReadBytes(childrenEntryObjectAddress, 0x18);
      if (!pyChildrenListMemory) return res;

      auto pyChildrenDictAddress = Convert::ToUInt64(pyChildrenListMemory->Raw(), 0x10);

      auto pyChildrenDictEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(pyChildrenDictAddress);

      if (!pyChildrenDictEntries) return res;

      PyDictEntry childrenEntry{};
      for (auto& pde : *pyChildrenDictEntries) {
        if (GetPythonTypeNameFromPythonObjectAddress(pde.key) == "str") {
          auto keyString = ReadPythonStringValueMaxLength4000(pde.key);
          if (keyString == "_childrenObjects") {
            childrenEntry = pde;
          }
        }
      }

      //  Console.WriteLine($"Found {(childrenEntry.value == 0 ? "no" : "a")} dictionary entry for children of 0x{nodeAddress:X}");

      if (childrenEntry.value == 0) return res;

      auto pythonListObjectMemory = memory_reader_->ReadBytes(childrenEntry.value, 0x20);

      if (!pythonListObjectMemory) return res;

      //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/listobject.h

      auto list_ob_size = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x10);

      if (4000 < list_ob_size || 0 >= list_ob_size) return res;

      auto listEntriesSize = list_ob_size * 8;

      auto list_ob_item = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x18);

      auto listEntriesMemory = memory_reader_->ReadBytes(list_ob_item, listEntriesSize);

      if (!listEntriesMemory) return res;

      auto listEntries = Convert::AsUInt64Array(listEntriesMemory);
      for (auto a : listEntries) {
        res.push_back(ReadUITreeFromAddressR(a, maxDepth - 1));
      }
      return res;
    };

    return std::make_shared<UITreeNode>(UITreeNode{nodeAddress, python_object_type_name_, dictEntriesOfInterest, ReadChildren()});
  }

  std::string ReadPythonStringValue(Address address, Size maxlength) {
    //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/stringobject.h

    auto stringObjectMemory = memory_reader_->ReadBytes(address, 0x20);

    if (!stringObjectMemory) return "";

    auto stringObject_ob_size = Convert::ToUInt64(stringObjectMemory->Raw(), 0x10);

    if (0 < maxlength && maxlength < stringObject_ob_size) return "";

    auto stringBytes = memory_reader_->ReadBytes(address + 8 * 4, stringObject_ob_size);

    if (!stringBytes) return "";
    std::string res;
    int idx = 0;
    for (int i = 0; i < stringBytes->Count(); i++) {
      if (++idx > stringObject_ob_size) break;
      res.push_back((*stringBytes)[i]);
    }
    return res;
  }
  double ReadPythonFloatObjectValue(Address address) {
    //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/floatobject.h

    auto pythonObjectMemory = memory_reader_->ReadBytes(address, 0x20);

    if (!pythonObjectMemory) return 0;

    return Convert::ToDouble(pythonObjectMemory->Raw(), 0x10);
  }
  std::string ReadPythonStringValueMaxLength4000(Address strObjectAddress) {
    return cache_.GetPythonStringValueMaxLength4000(
        strObjectAddress, [&](Address strObjectAddress) -> std::string { return ReadPythonStringValue(strObjectAddress, 4000); });
  }

  std::shared_ptr<std::vector<PyDictEntry>> ReadActiveDictionaryEntriesFromDictionaryAddress(Address dictionaryAddress) {
    auto default_res = std::make_shared<std::vector<PyDictEntry>>(std::vector<PyDictEntry>());
    /*
    Sources:
    https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/dictobject.h
    https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Objects/dictobject.c
    */

    auto data = memory_reader_->ReadBytes(dictionaryAddress, 0x30);
    if (!data) return default_res;
    std::vector<int64_t> dict_memory_as_long_array;
    dict_memory_as_long_array = Convert::AsInt64Array(data);

    //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/dictobject.h#L60-L89

    auto ma_fill = dict_memory_as_long_array[2];
    auto ma_used = dict_memory_as_long_array[3];
    auto ma_mask = dict_memory_as_long_array[4];
    auto ma_table = dict_memory_as_long_array[5];

    //  Console.WriteLine($"Details for dictionary 0x{dictionaryAddress:X}:
    //  type_name = '{dictTypeName}' ma_mask = 0x{ma_mask:X}, ma_table =
    //  0x{ma_table:X}.");

    auto numberOfSlots = ma_mask + 1;

    if (numberOfSlots < 0 || 10000 < numberOfSlots) {
      //  Avoid stalling the whole reading process when a single dictionary
      //  contains garbage.
      return default_res;
    }

    auto slotsMemorySize = numberOfSlots * 8 * 3;
    data = memory_reader_->ReadBytes(ma_table, slotsMemorySize);
    if (!data) return std::make_shared<std::vector<PyDictEntry>>(std::vector<PyDictEntry>());

    auto slots_memory_as_long_array = Convert::AsUInt64Array(data);
    auto entries = std::make_shared<std::vector<PyDictEntry>>(std::vector<PyDictEntry>());

    for (auto slotIndex = 0; slotIndex < numberOfSlots; ++slotIndex) {
      auto hash = slots_memory_as_long_array[slotIndex * 3];
      auto key = slots_memory_as_long_array[slotIndex * 3 + 1];
      auto value = slots_memory_as_long_array[slotIndex * 3 + 2];

      if (key == 0 || value == 0) continue;

      entries->push_back(PyDictEntry{hash, key, value});
    }

    return entries;
  }
  std::shared_ptr<std::map<std::string, uint64_t>> GetDictionaryEntriesWithStringKeys(uint64_t dictionary_object_address) {
    auto default_res = std::make_shared<std::map<std::string, uint64_t>>(std::map<std::string, uint64_t>());
    auto dictionaryEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(dictionary_object_address);
    if (!dictionaryEntries) return default_res;
    auto res = std::map<std::string, uint64_t>();
    for (auto& de : *dictionaryEntries) {
      res[ReadPythonStringValueMaxLength4000(de.key)] = de.value;
    }
    return std::make_shared<std::map<std::string, uint64_t>>(res);
  }

  std::string GetPythonTypeNameFromPythonTypeObjectAddress(Address typeObjectAddress) {
    auto typeObjectMemory = memory_reader_->ReadBytes(typeObjectAddress, 0x20);
    if (!typeObjectMemory) return "";
    auto tp_name = Convert::ToUInt64(typeObjectMemory->Raw(), 0x18);
    auto nameBytes = memory_reader_->ReadBytes(tp_name, 30);
    if (!nameBytes) return "";
    return Convert::AsASCIIString(nameBytes);
  }

  std::string GetPythonTypeNameFromPythonObjectAddress(Address objectAddress) {
    return cache_.GetPythonTypeNameFromPythonObjectAddress(objectAddress, [&](Address objectAddress) -> std::string {
      auto objectMemory = memory_reader_->ReadBytes(objectAddress, 0x10);
      if (!objectMemory) return "";
      return GetPythonTypeNameFromPythonTypeObjectAddress(Convert::ToUInt64(objectMemory->Raw(), 8));
    });
  }

  std::any GetDictEntryValueRepresentation(Address valueOjectAddress) {
    return cache_.GetDictEntryValueRepresentation(valueOjectAddress, [&](Address valueOjectAddress) -> std::any {
      auto genericRepresentation = UITreeNode::DictEntryValueGenericRepresentation{valueOjectAddress, ""};

      auto value_pythonTypeName = cache_.GetPythonTypeNameFromPythonObjectAddress(valueOjectAddress, [&](Address address) -> std::string {
        auto objectMemory = memory_reader_->ReadBytes(address, 0x10);

        if (!objectMemory) return "";
        return GetPythonTypeNameFromPythonTypeObjectAddress(Convert::ToUInt64(objectMemory->Raw(), 8));
      });

      genericRepresentation.python_object_type_name = value_pythonTypeName;

      if (value_pythonTypeName.empty()) return genericRepresentation;

      auto specializedRepresentation = specialized_reading_from_python_type_.find(value_pythonTypeName);
      if (value_pythonTypeName == "int") return specialized_reading_from_python_type_int(valueOjectAddress);
      if (value_pythonTypeName == "bool") return specialized_reading_from_python_type_bool(valueOjectAddress);
      if (value_pythonTypeName == "float") return specialized_reading_from_python_type_float(valueOjectAddress);
      if (value_pythonTypeName == "unicode") return specialized_reading_from_python_type_unicode(valueOjectAddress);
      if (value_pythonTypeName == "Bunch") return specialized_reading_from_python_type_Bunch(valueOjectAddress);
      if (value_pythonTypeName == "str") return specialized_reading_from_python_type_str(valueOjectAddress);
      if (specializedRepresentation == specialized_reading_from_python_type_.end()) return genericRepresentation;
      return specializedRepresentation->second(genericRepresentation.address);
    });
  }

  // about finding root address
  std::map<Address, Size> ReadCommittedMemoryRegionsFromProcess(uint64_t processId) {
    MemoryReaderFromProcess mr(processId);

    Address address = 0;

    auto committed_regions = std::map<Address, Size>();

    do {
      MEMORY_BASIC_INFORMATION m;
      auto _ = VirtualQueryEx(mr.handle_, (LPCVOID*)address, &m, sizeof(MEMORY_BASIC_INFORMATION64));

      auto region_protection = m.Protect;

      if (address == (Address)m.BaseAddress + (Size)m.RegionSize) break;

      address = (Address)m.BaseAddress + (Size)m.RegionSize;

      if (m.State != MEM_COMMIT) continue;

      auto protection_flags_to_skip = PAGE_GUARD | PAGE_NOACCESS;
      auto matching_flags_to_skip = protection_flags_to_skip & region_protection;

      if (matching_flags_to_skip != 0) {
        continue;
      }

      committed_regions[(Address)m.BaseAddress] = m.RegionSize;

    } while (true);
    memory_regions_ = committed_regions;
    return committed_regions;
  }

  std::vector<Address> EnumeratePossibleAddressesForUIRootObjects(const std::map<Address, Size>& mmrgs) {
    std::vector<std::pair<Address, Size>> mrgs;
    for (auto& mrg : mmrgs) {
      auto max_segment_size = 1024 * maxSegSize;
      if (mrg.second > max_segment_size) {
        auto start = mrg.first;
        auto bytes_left = mrg.second;
        while (bytes_left > max_segment_size) {
          mrgs.push_back(std::make_pair(start, max_segment_size));
          start += max_segment_size;
          start -= 8;
          bytes_left -= max_segment_size;
          bytes_left += 8;
        }
        if (bytes_left > 0) {
          mrgs.push_back(std::make_pair(start, bytes_left));
        }
      } else {
        mrgs.push_back(mrg);
      }
    }
    auto ReadNullTerminatedAsciiStringFromAddressUpTo255 = [&](Address address, int size = 0x100) {
      auto bytes_before_truncate = memory_reader_->ReadBytes(address, size);
      if (!bytes_before_truncate) return (std::string) "";

      return Convert::AsASCIIString(bytes_before_truncate);
    };
    auto ReadMemoryRegionContentAsULongArray = [&](const std::pair<Address, Size>& mrg) {
      auto length = mrg.second;
      auto data = memory_reader_->ReadBytes(mrg.first, length);
      if (!data) return std::vector<uint64_t>();

      return Convert::AsUInt64Array(data);
    };

    auto test = [&]() {
      // auto aa = 0x26d8c8177d0;
      // auto ss = ReadNullTerminatedAsciiStringFromAddressUpTo255(aa, 7);
      std::vector<Address> res;
      size_t step = 0;
      // std::ofstream logger("test.txt");
      // logger << mrgs.size() << std::endl;
      std::atomic<int> c = 0;
#pragma omp parallel for
      for (int i = 0; i < mrgs.size(); i++) {
        auto& mrg = mrgs[i];
        std::vector<Address> mr_content_as_uint64_t = ReadMemoryRegionContentAsULongArray(mrg);
        for (auto o = 0; o < mr_content_as_uint64_t.size(); o++) {
          // auto toab = memory_reader_->ReadBytes(mr_content_as_uint64_t[o], 8);
          // if (toab) {
          // auto toa = Convert::ToInt64(toab->Raw(), 0);
          auto toa = mr_content_as_uint64_t[o];
          auto tonab = memory_reader_->ReadBytes(toa + 24, 8);
          if (tonab) {
            auto tona = Convert::ToInt64(tonab->Raw(), 0);
            // auto toa = tob;
            const auto& ton = ReadNullTerminatedAsciiStringFromAddressUpTo255(tona, 7);
            if (ton == "UIRoot") {
#pragma omp critical
              res.push_back(mrg.first + o * 8 - 8);
#pragma omp critical
              std::cout << mrg.first + o * 8 - 8 << std::endl;
            }
          }
          //}
        }
        c++;
#pragma omp critical
        std::cout << c << std::endl;
      }
      // logger.flush();
      return res;
    };
    // return test();

    auto EnumerateCandidatesForPythonTypeObjectType = [&]() {
      std::unordered_set<Address> res;
      std::atomic<int> idx = 0;
      std::mutex m;
#pragma omp parallel for
      for (int i = 0; i < mrgs.size(); i++) {
        auto& mrg = mrgs[i];
        std::vector<Address> mr_content_as_uint64_t = ReadMemoryRegionContentAsULongArray(mrg);

        if (mr_content_as_uint64_t.size() > 0) {
          for (auto candidate_address_index = 0; candidate_address_index + 4 < mr_content_as_uint64_t.size(); ++candidate_address_index) {
            auto candidate_address_in_process = mrg.first + candidate_address_index * 8;

            auto candidate_ob_type = mr_content_as_uint64_t[candidate_address_index + 1];

            if (candidate_ob_type != candidate_address_in_process) continue;

            const auto& candidate_tp_name = ReadNullTerminatedAsciiStringFromAddressUpTo255(mr_content_as_uint64_t[candidate_address_index + 3]);
            if (candidate_tp_name != "type") continue;
            m.lock();
            res.insert(candidate_address_in_process);
            m.unlock();
          }
        }
      }
      return res;
    };
    auto EnumerateCandidatesForPythonTypeObjects = [&](const std::unordered_set<Address>& type_object_candidates_addresses) {
      std::map<Address, std::string> res;
      if (type_object_candidates_addresses.size() > 0) {
        // int idx = 0;
        std::atomic<int> idx = 0;
        std::mutex m;
#pragma omp parallel for
        for (int i = 0; i < mrgs.size(); i++) {
          auto& mrg = mrgs[i];
          // std::for_each(std::execution::unseq, mrgs.begin(), mrgs.end(), [&](auto& mrg) {
          //  for (auto& mrg : mrgs) {
          // std::cout << ++idx << "/" << mrgs.size() << std::endl;
          std::vector<Address> mr_content_as_uint64_t = ReadMemoryRegionContentAsULongArray(mrg);
          if (mr_content_as_uint64_t.size() > 0) {
            for (uint64_t candidate_address_index = 0; candidate_address_index + 4 < mr_content_as_uint64_t.size(); ++candidate_address_index) {
              auto candidate_address_in_process = mrg.first + candidate_address_index * 8;

              auto candidate_ob_type = mr_content_as_uint64_t[candidate_address_index + 1];

              if (type_object_candidates_addresses.find(candidate_ob_type) == type_object_candidates_addresses.end()) continue;

              const auto& candidate_tp_name = ReadNullTerminatedAsciiStringFromAddressUpTo255(mr_content_as_uint64_t[candidate_address_index + 3]);

              if (candidate_tp_name.size() < 1) continue;
              m.lock();
              res.insert(std::make_pair(candidate_address_in_process, candidate_tp_name));
              m.unlock();
            }
          }
        }
      }
      return res;
    };
    auto EnumerateCandidatesForInstancesOfPythonType = [&](const std::vector<Address>& type_object_candidates_addresses) {
      std::vector<Address> res;
      if (type_object_candidates_addresses.size() > 0) {
        std::atomic<int> idx = 0;
        std::mutex m;
#pragma omp parallel for
        for (int i = 0; i < mrgs.size(); i++) {
          auto& mrg = mrgs[i];
          // std::for_each(std::execution::unseq, mrgs.begin(), mrgs.end(), [&](auto& mrg) {
          //  for (auto& mrg : mrgs) {
          // std::cout << ++idx << "/" << mrgs.size() << std::endl;
          std::vector<Address> mr_content_as_uint64_t = ReadMemoryRegionContentAsULongArray(mrg);
          if (mr_content_as_uint64_t.size() > 0) {
            for (auto candidate_address_index = 0; candidate_address_index + 4 < mr_content_as_uint64_t.size(); ++candidate_address_index) {
              auto candidate_address_in_process = mrg.first + candidate_address_index * 8;

              auto candidate_ob_type = mr_content_as_uint64_t[candidate_address_index + 1];

              if (std::find(type_object_candidates_addresses.begin(), type_object_candidates_addresses.end(), candidate_ob_type) ==
                  type_object_candidates_addresses.end())
                continue;
              m.lock();
              res.push_back(candidate_address_in_process);
              m.unlock();
            }
          }
        }
      }
      return res;
    };
    auto cfptos = EnumerateCandidatesForPythonTypeObjects(EnumerateCandidatesForPythonTypeObjectType());
    std::vector<Address> res;
    for (auto& cfpto : cfptos) {
      if (cfpto.second == "UIRoot") {
        res.push_back(cfpto.first);
      }
    }

    return EnumerateCandidatesForInstancesOfPythonType(res);
  }

  // Decrepated
  UITreeNodeFixed* ReadUITreeFromAddressRFixed(Address nodeAddress, int maxDepth) {
    // if (maxDepth == 16) {
    //   chi.clear();
    // }
    auto uiNodeObjectMemory = memory_reader_->ReadBytes(nodeAddress, 0x30);
    if (!uiNodeObjectMemory) return nullptr;

    auto dictAddress = Convert::ToUInt64(uiNodeObjectMemory->Raw(), 0x10);
    auto dictionaryEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(dictAddress);

    auto python_object_type_name_ = GetPythonTypeNameFromPythonObjectAddress(nodeAddress);
    if (python_object_type_name_.empty()) return nullptr;

    if (!dictionaryEntries) return nullptr;

    auto res = UITreeNodeFixedPool::GetNew();  //) UITreeNodeFixed{0, 0, 0, 0, 0, 0, 0, 0, 0, "", "", "", "", "", 0, {0}};
    uint64_t l;
    uint8_t w = 1;
    l = python_object_type_name_.length();
    if (python_object_type_name_.length() > 255) {
      l = 8;
      python_object_type_name_ = "too long";
    }
    memcpy(res->python_object_type_name, &l, 1);
    memcpy(res->python_object_type_name + 1, &w, 1);
    memcpy(res->python_object_type_name + 2, python_object_type_name_.c_str(), l * w);

    std::any childrenDictEntry;
    for (auto& dictionaryEntry : *dictionaryEntries) {
      auto key_object_type_name = GetPythonTypeNameFromPythonObjectAddress(dictionaryEntry.key);

      if (key_object_type_name != "str") continue;
      auto key_string = ReadPythonStringValueMaxLength4000(dictionaryEntry.key);
      if (dict_entries_of_interest_keys_.find(key_string) == dict_entries_of_interest_keys_.end()) {
        continue;
      }
      auto t = GetDictEntryValueRepresentation(dictionaryEntry.value);
      if (key_string == "children") {
        childrenDictEntry = t;
        continue;
      }
      if (t.type() == typeid(UITreeNode::DictEntryValueGenericRepresentation)) {
        continue;
      }

      std::wstring ws;
      std::string s;
      if (key_string == "_name") {
        if (t.type() == typeid(std::wstring)) {
          ws = std::any_cast<std::wstring>(t);
          w = 2;
          l = ws.length();
          if (ws.length() > 255) {
            l = 8;
            ws = L"too long";
          }
          memcpy(res->_name, &l, 1);
          memcpy(res->_name + 1, &w, 1);
          memcpy(res->_name + 2, ws.c_str(), l * w);
        }
        if (t.type() == typeid(std::string)) {
          s = std::any_cast<std::string>(t);
          w = 1;
          l = s.length();
          if (s.length() > 255) {
            l = 8;
            s = "too long";
          }
          memcpy(res->_name, &l, 1);
          memcpy(res->_name + 1, &w, 1);
          memcpy(res->_name + 2, s.c_str(), l * w);
        }

        continue;
      }
      if (key_string == "_text") {
        if (t.type() == typeid(std::wstring)) {
          ws = std::any_cast<std::wstring>(t);
          w = 2;
          l = ws.length();
          if (ws.length() > 255) {
            l = 8;
            ws = L"too long";
          }
          memcpy(res->_text, &l, 1);
          memcpy(res->_text + 1, &w, 1);
          memcpy(res->_text + 2, ws.c_str(), l * w);
        }
        if (t.type() == typeid(std::string)) {
          s = std::any_cast<std::string>(t);
          w = 1;
          l = s.length();
          if (s.length() > 255) {
            l = 8;
            s = "too long";
          }
          memcpy(res->_text, &l, 1);
          memcpy(res->_text + 1, &w, 1);
          memcpy(res->_text + 2, s.c_str(), l * w);
        }
        continue;
      }
      if (key_string == "_setText") {
        if (t.type() == typeid(std::wstring)) {
          ws = std::any_cast<std::wstring>(t);
          w = 2;
          l = ws.length();
          if (ws.length() > 255) {
            l = 255;
            ws = L"too long" + ws.substr(0, 255);
          }
          memcpy(res->_setText, &l, 1);
          memcpy(res->_setText + 1, &w, 1);
          memcpy(res->_setText + 2, ws.c_str(), l * w);
        }
        if (t.type() == typeid(std::string)) {
          s = std::any_cast<std::string>(t);
          w = 1;
          l = s.length();
          if (s.length() > 255) {
            l = 510;
            s = "too long" + s.substr(0, 510);
          }
          memcpy(res->_setText, &l, 1);
          memcpy(res->_setText + 1, &w, 1);
          memcpy(res->_setText + 2, s.c_str(), l * w);
        }
        continue;
      }
      if (key_string == "_hint") {
        if (t.type() == typeid(std::wstring)) {
          ws = std::any_cast<std::wstring>(t);
          w = 2;
          l = ws.length();
          if (ws.length() > 255) {
            l = 8;
            ws = L"too long";
          }
          memcpy(res->_hint, &l, 1);
          memcpy(res->_hint + 1, &w, 1);
          memcpy(res->_hint + 2, ws.c_str(), l * w);
        }
        if (t.type() == typeid(std::string)) {
          s = std::any_cast<std::string>(t);
          w = 1;
          l = s.length();
          if (s.length() > 255) {
            l = 8;
            s = "too long";
          }
          memcpy(res->_hint, &l, 1);
          memcpy(res->_hint + 1, &w, 1);
          memcpy(res->_hint + 2, s.c_str(), l * w);
        }
        continue;
      }
      if (key_string == "_top") {
        if (t.type() == typeid(int64_t)) res->_top = std::any_cast<int64_t>(t);
        continue;
      }
      if (key_string == "_left") {
        if (t.type() == typeid(int64_t)) res->_left = std::any_cast<int64_t>(t);
        continue;
      }
      if (key_string == "_width") {
        if (t.type() == typeid(int64_t)) res->_width = std::any_cast<int64_t>(t);
        continue;
      }
      if (key_string == "_height") {
        if (t.type() == typeid(int64_t)) res->_height = std::any_cast<int64_t>(t);
        continue;
      }
      if (key_string == "_displayX") {
        if (t.type() == typeid(int64_t)) res->_displayX = std::any_cast<int64_t>(t);
        continue;
      }
      if (key_string == "_displayY") {
        if (t.type() == typeid(int64_t)) res->_displayY = std::any_cast<int64_t>(t);
        continue;
      }
      if (key_string == "_selected") {
        if (t.type() == typeid(int64_t)) res->_selected = std::any_cast<std::int64_t>(t);
        continue;
      }
      if (key_string == "ramp_active") {
        if (t.type() == typeid(int64_t)) res->active = std::any_cast<std::int64_t>(t);
        continue;
      }
      if (key_string == "isInActiveState") {  // TODO this does not indicate whether a module is active
        // if (t.type() == typeid(int64_t)) res->active = std::any_cast<std::int64_t>(t);
        continue;
      }
      if (key_string == "isDeactivating") {
        if (t.type() == typeid(int64_t)) res->isDeactivating = std::any_cast<std::int64_t>(t);
        continue;
      }
      if (key_string == "quantity") {
        if (t.type() == typeid(int64_t)) res->quantity = std::any_cast<std::int64_t>(t);
        continue;
      }
      if (key_string == "_lastValue") {
        if (t.type() == typeid(double)) res->_lastValue = std::any_cast<double>(t);
        continue;
      }
    }
    res->nc = 0;
    auto ReadChildren = [&]() -> void {
      if (maxDepth < 1) return;

      //  https://github.com/Arcitectus/Sanderling/blob/b07769fb4283e401836d050870121780f5f37910/guide/image/2015-01.eve-online-python-ui-tree-structure.png

      if (!childrenDictEntry.has_value()) return;
      // FIX
      Address childrenEntryObjectAddress;
      auto ca = std::any_cast<UITreeNode::DictEntryValueGenericRepresentation>(childrenDictEntry).address;
      if (ca != 0) {
        childrenEntryObjectAddress = ca;
      } else {
        return;
      }

      auto pyChildrenListMemory = memory_reader_->ReadBytes(childrenEntryObjectAddress, 0x18);
      if (!pyChildrenListMemory) return;

      auto pyChildrenDictAddress = Convert::ToUInt64(pyChildrenListMemory->Raw(), 0x10);

      auto pyChildrenDictEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(pyChildrenDictAddress);

      if (!pyChildrenDictEntries) return;

      PyDictEntry childrenEntry{};
      for (auto& pde : *pyChildrenDictEntries) {
        if (GetPythonTypeNameFromPythonObjectAddress(pde.key) == "str") {
          auto keyString = ReadPythonStringValueMaxLength4000(pde.key);
          if (keyString == "_childrenObjects") {
            childrenEntry = pde;
          }
        }
      }

      if (childrenEntry.value == 0) return;

      auto pythonListObjectMemory = memory_reader_->ReadBytes(childrenEntry.value, 0x20);

      if (!pythonListObjectMemory) return;

      //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/listobject.h

      auto list_ob_size = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x10);

      if (4000 < list_ob_size || 0 >= list_ob_size) return;

      auto listEntriesSize = list_ob_size * 8;

      auto list_ob_item = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x18);

      auto listEntriesMemory = memory_reader_->ReadBytes(list_ob_item, listEntriesSize);

      if (!listEntriesMemory) return;

      auto listEntries = Convert::AsUInt64Array(listEntriesMemory);

      for (auto a : listEntries) {
        if (res->nc + 1 > UITreeNodeFixed::kMaxChildren) {
          break;
        }
        volatile auto c = ReadUITreeFromAddressRFixed(a, maxDepth - 1);
        if (c == nullptr) {
          continue;
        } else {
          res->children[res->nc] = c - UITreeNodeFixedPool::begin_;
          res->nc++;
        }
      }
    };

    ReadChildren();
    return res;
  }

  void ReadUITreeFromAddressRPB(Address nodeAddress, int maxDepth, UITreeNodePB* res) {
    auto ui_node_object_memory = memory_reader_->ReadBytes(nodeAddress, 0x30);
    if (!ui_node_object_memory) return;

    auto dict_address = Convert::ToUInt64(ui_node_object_memory->Raw(), 0x10);
    auto dictionary_entries = ReadActiveDictionaryEntriesFromDictionaryAddress(dict_address);

    auto python_object_type_name_ = GetPythonTypeNameFromPythonObjectAddress(nodeAddress);
    if (python_object_type_name_.empty()) return;

    if (!dictionary_entries) return;

    res->set_python_object_type_name(python_object_type_name_);

    std::any children_dict_entry;
    for (auto& dictionary_entry : *dictionary_entries) {
      auto key_object_type_name = GetPythonTypeNameFromPythonObjectAddress(dictionary_entry.key);

      if (key_object_type_name != "str") continue;
      auto key_string = ReadPythonStringValueMaxLength4000(dictionary_entry.key);
      if (dict_entries_of_interest_keys_.find(key_string) == dict_entries_of_interest_keys_.end()) {
        continue;
      }
      auto t = GetDictEntryValueRepresentation(dictionary_entry.value);
      if (key_string == "children") {
        children_dict_entry = t;
        continue;
      }
      if (t.type() == typeid(UITreeNode::DictEntryValueGenericRepresentation)) {
        continue;
      }

      std::wstring ws;
      std::string s;
      if (key_string == "_name") {
        res->set__name(Convert::AnyStringToBytes(t));
        continue;
      }
      if (key_string == "_text") {
        res->set__text(Convert::AnyStringToBytes(t));
        continue;
      }
      if (key_string == "_setText") {
        res->set__settext(Convert::AnyStringToBytes(t));
        continue;
      }
      if (key_string == "_hint") {
        res->set__hint(Convert::AnyStringToBytes(t));
        continue;
      }
      if (key_string == "_top") {
        if (t.type() == typeid(int64_t)) res->set__top(std::any_cast<int64_t>(t));
        continue;
      }
      if (key_string == "_left") {
        if (t.type() == typeid(int64_t)) res->set__left(std::any_cast<int64_t>(t));
        continue;
      }
      if (key_string == "_width") {
        if (t.type() == typeid(int64_t)) res->set__width(std::any_cast<int64_t>(t));
        continue;
      }
      if (key_string == "_height") {
        if (t.type() == typeid(int64_t)) res->set__height(std::any_cast<int64_t>(t));
        continue;
      }
      if (key_string == "_displayX") {
        if (t.type() == typeid(int64_t)) res->set__displayx(std::any_cast<int64_t>(t));
        continue;
      }
      if (key_string == "_displayY") {
        if (t.type() == typeid(int64_t)) res->set__displayy(std::any_cast<int64_t>(t));
        continue;
      }
      if (key_string == "_selected") {
        if (t.type() == typeid(int64_t)) res->set__selected(std::any_cast<std::int64_t>(t));
        continue;
      }
      if (key_string == "ramp_active") {
        if (t.type() == typeid(int64_t)) res->set_active(std::any_cast<std::int64_t>(t));
        continue;
      }
      if (key_string == "isDeactivating") {
        if (t.type() == typeid(int64_t)) res->set_isdeactivating(std::any_cast<std::int64_t>(t));
        continue;
      }
      if (key_string == "quantity") {
        if (t.type() == typeid(int64_t)) res->set_quantity(std::any_cast<std::int64_t>(t));
        continue;
      }
      if (key_string == "_lastValue") {
        if (t.type() == typeid(double)) res->set__lastvalue(std::any_cast<double>(t));
        continue;
      }
    }

    auto ReadChildren = [&]() -> void {
      if (maxDepth < 1) return;

      //    https://
      //    github.com/Arcitectus/Sanderling/blob/b07769fb4283e401836d050870121780f5f37910/guide/image/2015-01.eve-online-python-ui-tree-structure.png

      if (!children_dict_entry.has_value()) return;
      // FIX
      Address childrenEntryObjectAddress;
      auto ca = std::any_cast<UITreeNode::DictEntryValueGenericRepresentation>(children_dict_entry).address;
      if (ca != 0) {
        childrenEntryObjectAddress = ca;
      } else {
        return;
      }

      auto pyChildrenListMemory = memory_reader_->ReadBytes(childrenEntryObjectAddress, 0x18);
      if (!pyChildrenListMemory) return;

      auto pyChildrenDictAddress = Convert::ToUInt64(pyChildrenListMemory->Raw(), 0x10);

      auto pyChildrenDictEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(pyChildrenDictAddress);

      if (!pyChildrenDictEntries) return;

      PyDictEntry childrenEntry{};
      for (auto& pde : *pyChildrenDictEntries) {
        if (GetPythonTypeNameFromPythonObjectAddress(pde.key) == "str") {
          auto keyString = ReadPythonStringValueMaxLength4000(pde.key);
          if (keyString == "_childrenObjects") {
            childrenEntry = pde;
          }
        }
      }

      if (childrenEntry.value == 0) return;

      auto pythonListObjectMemory = memory_reader_->ReadBytes(childrenEntry.value, 0x20);

      if (!pythonListObjectMemory) return;

      //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/listobject.h

      auto list_ob_size = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x10);

      if (4000 < list_ob_size || 0 >= list_ob_size) return;

      auto listEntriesSize = list_ob_size * 8;

      auto list_ob_item = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x18);

      auto listEntriesMemory = memory_reader_->ReadBytes(list_ob_item, listEntriesSize);

      if (!listEntriesMemory) return;

      auto listEntries = Convert::AsUInt64Array(listEntriesMemory);
      int i = 0;
      for (auto a : listEntries) {
        ReadUITreeFromAddressRPB(a, maxDepth - 1, res->add_children());
      }
    };
    ReadChildren();
  }
};

void TreeToTreePB(UITreeNode* root_in, UITreeNodePB* root_out) {
  if (root_in == nullptr) {
    root_out->set_python_object_type_name("Can't read");
    return;
  }
  root_out->set_python_object_type_name(root_in->python_object_type_name_);

  auto res = root_in->dict_entries_of_interest_.find("_setText");
  if (res != root_in->dict_entries_of_interest_.end()) {
    root_out->set__settext(Convert::AnyStringToBytes(res->second));
  }
  res = root_in->dict_entries_of_interest_.find("_name");
  if (res != root_in->dict_entries_of_interest_.end()) {
    root_out->set__name(Convert::AnyStringToBytes(res->second));
  }
  res = root_in->dict_entries_of_interest_.find("_text");
  if (res != root_in->dict_entries_of_interest_.end()) {
    root_out->set__text(Convert::AnyStringToBytes(root_in->dict_entries_of_interest_.find("_text")->second));
  }
  res = root_in->dict_entries_of_interest_.find("_hint");
  if (res != root_in->dict_entries_of_interest_.end()) {
    root_out->set__hint(Convert::AnyStringToBytes(root_in->dict_entries_of_interest_.find("_hint")->second));
  }
  res = root_in->dict_entries_of_interest_.find("_top");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set__top(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_top")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_left");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set__left(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_left")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_width");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set__width(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_width")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_height");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set__height(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_height")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_displayX");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set__displayx(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_displayX")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_displayY");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set__displayy(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_displayY")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_selected");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set__selected(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_selected")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("ramp_active");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set_active(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("ramp_active")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("isDeactivating");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set_isdeactivating(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("isDeactivating")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_display");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set_display(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("_display")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("quantity");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(int64_t)) {
      root_out->set_quantity(std::any_cast<int64_t>(root_in->dict_entries_of_interest_.find("quantity")->second));
    }
  }
  res = root_in->dict_entries_of_interest_.find("_lastValue");
  if (res != root_in->dict_entries_of_interest_.end()) {
    if (res->second.type() == typeid(double)) {
      root_out->set__lastvalue(std::any_cast<double>(root_in->dict_entries_of_interest_.find("_lastValue")->second));
    }
  }
  for (auto& c : root_in->children_) {
    TreeToTreePB(c.get(), root_out->add_children());
  }
}

void TreeToTreePB2211(UITreeNode* root_in, UITreeNodePB2211* root_out) {
  if (root_in == nullptr) {
    root_out->set_python_object_type_name("null");
    return;
  }
  root_out->set_python_object_type_name(root_in->python_object_type_name_);
  root_out->set_python_object_address(root_in->python_object_address_);

  auto& fields = *root_out->mutable_fields();
  for (const auto& f : root_in->dict_entries_of_interest_) {
    if (f.second.type() == typeid(int64_t)) {
      fields[f.first].set_int32_value(std::any_cast<int64_t>(f.second));
      continue;
    }
    if (f.second.type() == typeid(double)) {
      fields[f.first].set_double_value(std::any_cast<double>(f.second));
      continue;
    }
    if (f.second.type() == typeid(std::string) || f.second.type() == typeid(std::wstring)) {
      fields[f.first].set_string_value(Convert::AnyStringToBytes(f.second));
      continue;
    }
    if (f.second.type() == typeid(bool)) {
      fields[f.first].set_bool_value(std::any_cast<bool>(f.second));
      continue;
    }
  }
  for (auto& c : root_in->children_) {
    TreeToTreePB2211(c.get(), root_out->add_children());
  }
}
}  // namespace EveMemoryReading
