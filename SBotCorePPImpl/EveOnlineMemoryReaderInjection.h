#pragma once

#include <inttypes.h>
#include <omp.h>

#include <algorithm>
#include <any>
#include <atomic>
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
#include "conversions.h"
#include "types.h"

namespace EveOnlineMemoryReading {

class EveOnlineMemoryReaderInjection {
 public:
  HANDLE h;

  UITreeNodePoolFixed* UITNPF;
  std::set<std::string> dict_entries_of_interest_keys;
  std::map<std::string, std::function<std::any(Address)>> specialized_reading_from_python_type;
  MemoryReadingCache cache;
  IMemoryReader* memory_reader;

  std::string ReadPythonStringValue(Address address, Size maxlength) {
    //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/stringobject.h

    auto stringObjectMemory = memory_reader->ReadBytes(address, 0x20);

    if (!stringObjectMemory) return "";

    auto stringObject_ob_size = Convert::ToUInt64(stringObjectMemory->Raw(), 0x10);

    if (0 < maxlength && maxlength < stringObject_ob_size) return "";

    auto stringBytes = memory_reader->ReadBytes(address + 8 * 4, stringObject_ob_size);

    if (!stringBytes) return "";
    std::string res;
    for (auto& c : stringBytes->data) {
      res.push_back(c);
    }
    return res;
  }
  double ReadPythonFloatObjectValue(Address address) {
    //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/floatobject.h

    auto pythonObjectMemory = memory_reader->ReadBytes(address, 0x20);

    if (!pythonObjectMemory) throw std::exception("Read memory failed");

    return Convert::ToDouble(pythonObjectMemory->Raw(), 0x10);
  }
  std::string ReadPythonStringValueMaxLength4000(Address strObjectAddress) {
    return cache.GetPythonStringValueMaxLength4000(
        strObjectAddress, [&](Address strObjectAddress) -> std::string { return ReadPythonStringValue(strObjectAddress, 4000); });
  }

  std::shared_ptr<std::vector<PyDictEntry>> ReadActiveDictionaryEntriesFromDictionaryAddress(Address dictionaryAddress) {
    auto default_res = std::make_shared<std::vector<PyDictEntry>>(std::vector<PyDictEntry>());
    /*
    Sources:
    https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/dictobject.h
    https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Objects/dictobject.c
    */

    auto data = memory_reader->ReadBytes(dictionaryAddress, 0x30);
    if (!data) return default_res;
    std::vector<uint64_t> dict_memory_as_long_array;
    dict_memory_as_long_array = Convert::AsUInt64Array(data);

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
    data = memory_reader->ReadBytes(ma_table, slotsMemorySize);
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
    auto typeObjectMemory = memory_reader->ReadBytes(typeObjectAddress, 0x20);
    if (!typeObjectMemory) return "";
    auto tp_name = Convert::ToUInt64(typeObjectMemory->Raw(), 0x18);
    auto nameBytes = memory_reader->ReadBytes(tp_name, 30);
    if (!nameBytes) return "";
    return Convert::AsASCIIString(nameBytes);
  }
  std::string GetPythonTypeNameFromPythonObjectAddress(Address objectAddress) {
    return cache.GetPythonTypeNameFromPythonObjectAddress(objectAddress, [&](Address objectAddress) -> std::string {
      auto objectMemory = memory_reader->ReadBytes(objectAddress, 0x10);
      if (!objectMemory) return "";
      return GetPythonTypeNameFromPythonTypeObjectAddress(Convert::ToUInt64(objectMemory->Raw(), 8));
    });
  }
  std::any GetDictEntryValueRepresentation(Address valueOjectAddress) {
    return cache.GetDictEntryValueRepresentation(valueOjectAddress, [&](Address valueOjectAddress) -> std::any {
      auto genericRepresentation = UITreeNode::DictEntryValueGenericRepresentation{valueOjectAddress, ""};

      auto value_pythonTypeName = cache.GetPythonTypeNameFromPythonObjectAddress(valueOjectAddress, [&](Address address) -> std::string {
        auto objectMemory = memory_reader->ReadBytes(address, 0x10);

        if (!objectMemory) throw std::exception("Read memory failed");
        return GetPythonTypeNameFromPythonTypeObjectAddress(Convert::ToUInt64(objectMemory->Raw(), 8));
      });

      genericRepresentation.pythonObjectTypeName = value_pythonTypeName;

      if (value_pythonTypeName.empty()) return genericRepresentation;

      auto specializedRepresentation = specialized_reading_from_python_type.find(value_pythonTypeName);
      if (specializedRepresentation == specialized_reading_from_python_type.end()) return genericRepresentation;
      return specializedRepresentation->second(genericRepresentation.address);
    });
  }

 public:
  EveOnlineMemoryReading() {
    UITNPF = new UITreeNodePoolFixed();
    dict_entries_of_interest_keys = std::set<std::string>{
        "children", "_top", "_left", "_width", "_height", "_displayX", "_displayY",
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
        "_lastValue",

        //  Found in "ModuleButton"
        "ramp_active"  //, "busy"

        ////  Found in the Transforms contained in "ShipModuleButtonRamps"
        //"_rotation",

        ////  Found under OverviewEntry in Sprite named "iconSprite"
        //"_color",

        ////  Found in "SE_TextlineCore"
        //"_sr",

        ////  Found in "_sr" Bunch
        //"htmlstr"
    };
    specialized_reading_from_python_type["str"] = [this](Address address) -> std::string { return ReadPythonStringValue(address, 0x1000); };
    specialized_reading_from_python_type["unicode"] = [this](Address address) -> std::wstring {
      auto python_object_memory = memory_reader->ReadBytes(address, 0x20);
      if (!python_object_memory) throw std::exception("Read memory failed");

      auto unicode_string_length = Convert::ToUInt64(python_object_memory->Raw(), 0x10);
      if (0x1000 < unicode_string_length) throw std::exception("Unicode string too long");

      auto string_bytes_count = unicode_string_length * 2;
      std::wstring res;
      res.resize(unicode_string_length);
      auto base = *(uint64_t*)(python_object_memory->Raw() + 0x18);
      auto data = memory_reader->ReadBytes(base, string_bytes_count);
      if (!data) throw std::exception("Read memory failed");

      memcpy(&res[0], data->Raw(), string_bytes_count);
      return res;
    };
    specialized_reading_from_python_type["int"] = [this](Address address) -> int64_t {
      auto int_object_memory = memory_reader->ReadBytes(address, 0x18);
      if (!int_object_memory) throw std::exception("Read memory failed");

      return (int32_t)Convert::ToInt64(int_object_memory->Raw(), 0x10);
    };
    specialized_reading_from_python_type["bool"] = [this](Address address) -> int64_t {
      auto data = memory_reader->ReadBytes(address, 0x18);
      if (!data) throw std::exception("Read memory failed");
      return Convert::ToInt64(data->Raw(), 0x10);
    };
    specialized_reading_from_python_type["float"] = [this](Address address) -> double { return ReadPythonFloatObjectValue(address); };
    specialized_reading_from_python_type["Bunch"] = [this](Address address) -> std::map<std::string, std::any> {
      auto dictionaryEntries = GetDictionaryEntriesWithStringKeys(address);
      if (!dictionaryEntries) throw std::exception("Failed to read dictionary entries.");

      auto entriesOfInterest = std::map<std::string, std::any>();
      for (auto& entry : *dictionaryEntries) {
        if (dict_entries_of_interest_keys.end() == dict_entries_of_interest_keys.find(entry.first)) {
          continue;
        }
        entriesOfInterest[entry.first] = (GetDictEntryValueRepresentation(entry.second));
      }
      return entriesOfInterest;
    };
  }
  std::shared_ptr<UITreeNode> ReadUITreeFromAddress(Address nodeAddress, int maxDepth) {
    cache = MemoryReadingCache();
    return ReadUITreeFromAddressR(nodeAddress, maxDepth);
  }
  std::shared_ptr<UITreeNode> ReadUITreeFromAddressR(Address nodeAddress, int maxDepth) {
    auto uiNodeObjectMemory = memory_reader->ReadBytes(nodeAddress, 0x30);
    if (!uiNodeObjectMemory) return nullptr;

    auto pythonObjectTypeName = GetPythonTypeNameFromPythonObjectAddress(nodeAddress);
    if (pythonObjectTypeName.empty()) return nullptr;

    auto dictAddress = Convert::ToUInt64(uiNodeObjectMemory->Raw(), 0x10);
    auto dictionaryEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(dictAddress);
    if (!dictionaryEntries) return nullptr;

    auto dictEntriesOfInterest = std::map<std::string, std::any>();
    for (auto& dictionaryEntry : *dictionaryEntries) {
      auto keyObject_type_name = GetPythonTypeNameFromPythonObjectAddress(dictionaryEntry.key);

      if (keyObject_type_name != "str") continue;

      auto keyString = ReadPythonStringValueMaxLength4000(dictionaryEntry.key);

      if (dict_entries_of_interest_keys.find(keyString) == dict_entries_of_interest_keys.end()) {
        // otherDictEntriesKeys.Add(keyString);
        continue;
      }
      auto t = GetDictEntryValueRepresentation(dictionaryEntry.value);
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

      //  Console.WriteLine($"'children' dict entry of 0x{nodeAddress:X} points to 0x{childrenEntryObjectAddress:X}.");

      auto pyChildrenListMemory = memory_reader->ReadBytes(childrenEntryObjectAddress, 0x18);
      if (!pyChildrenListMemory) throw std::exception("Read memory failed");

      auto pyChildrenDictAddress = Convert::ToUInt64(pyChildrenListMemory->Raw(), 0x10);

      auto pyChildrenDictEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(pyChildrenDictAddress);

      //  Console.WriteLine($"Found {(pyChildrenDictEntries == null ? "no" : "some")} children dictionary entries for 0x{nodeAddress:X}");

      if (!pyChildrenDictEntries) throw std::exception("Read memory failed");

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

      auto pythonListObjectMemory = memory_reader->ReadBytes(childrenEntry.value, 0x20);

      if (!pythonListObjectMemory) throw std::exception("Read memory failed");

      //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/listobject.h

      auto list_ob_size = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x10);

      if (4000 < list_ob_size || 0 >= list_ob_size) return res;

      auto listEntriesSize = list_ob_size * 8;

      auto list_ob_item = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x18);

      auto listEntriesMemory = memory_reader->ReadBytes(list_ob_item, listEntriesSize);

      if (!listEntriesMemory) throw std::exception("Read memory failed");

      auto listEntries = Convert::AsUInt64Array(listEntriesMemory);
      for (auto a : listEntries) {
        res.push_back(ReadUITreeFromAddressR(a, maxDepth - 1));
      }
      return res;
    };

    return std::make_shared<UITreeNode>(UITreeNode{nodeAddress, pythonObjectTypeName, dictEntriesOfInterest, ReadChildren()});
  }

  UITreeNodeFixed* ReadUITreeFromAddressFixed(Address nodeAddress, int maxDepth) {
    cache = MemoryReadingCache();
    UITNPF->Reset();
    return ReadUITreeFromAddressRFixed(nodeAddress, maxDepth);
  }

  UITreeNodeFixed* ReadUITreeFromAddressRFixed(Address nodeAddress, int maxDepth) {
    auto uiNodeObjectMemory = memory_reader->ReadBytes(nodeAddress, 0x30);
    if (!uiNodeObjectMemory) return nullptr;

    auto pythonObjectTypeName = GetPythonTypeNameFromPythonObjectAddress(nodeAddress);
    if (pythonObjectTypeName.empty()) return nullptr;

    auto dictAddress = Convert::ToUInt64(uiNodeObjectMemory->Raw(), 0x10);
    auto dictionaryEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(dictAddress);
    if (!dictionaryEntries) return nullptr;

    int64_t _top = 0, _left = 0, _width = 0, _height = 0, _displayX = 0, _displayY = 0;
    std::wstring _name, _text, _setText, hint;
    uint64_t _selected = 0, ramp_active = 0;
    double _lastValue = 0;

    std::any childrenDictEntry;
    for (auto& dictionaryEntry : *dictionaryEntries) {
      auto keyObject_type_name = GetPythonTypeNameFromPythonObjectAddress(dictionaryEntry.key);

      if (keyObject_type_name != "str") continue;

      auto keyString = ReadPythonStringValueMaxLength4000(dictionaryEntry.key);

      if (dict_entries_of_interest_keys.find(keyString) == dict_entries_of_interest_keys.end()) {
        // otherDictEntriesKeys.Add(keyString);
        continue;
      }
      auto t = GetDictEntryValueRepresentation(dictionaryEntry.value);
      if (keyString == "children") {
        childrenDictEntry = t;
      }
      if (keyString == "_top") {
        if (t.type() == typeid(int64_t)) _top = std::any_cast<int64_t>(t);
      }
      if (keyString == "_left") {
        if (t.type() == typeid(int64_t)) _left = std::any_cast<int64_t>(t);
      }
      if (keyString == "_width") {
        if (t.type() == typeid(int64_t)) _width = std::any_cast<int64_t>(t);
      }
      if (keyString == "_height") {
        if (t.type() == typeid(int64_t)) _height = std::any_cast<int64_t>(t);
      }
      if (keyString == "_displayX") {
        if (t.type() == typeid(int64_t)) _displayX = std::any_cast<int64_t>(t);
      }
      if (keyString == "_displayY") {
        if (t.type() == typeid(int64_t)) _displayY = std::any_cast<int64_t>(t);
      }
      if (keyString == "_name") {
        if (t.type() == typeid(std::wstring)) _name = std::any_cast<std::wstring>(t);
      }
      if (keyString == "_text") {
        if (t.type() == typeid(std::wstring)) _text = std::any_cast<std::wstring>(t);
      }
      if (keyString == "_setText") {
        if (t.type() == typeid(std::wstring)) _setText = std::any_cast<std::wstring>(t);
      }
      if (keyString == "hint") {
        if (t.type() == typeid(std::wstring)) hint = std::any_cast<std::wstring>(t);
      }
      if (keyString == "_selected") {
        if (t.type() == typeid(uint64_t)) _selected = std::any_cast<std::uint64_t>(t);
      }
      if (keyString == "ramp_active") {
        if (t.type() == typeid(uint64_t)) ramp_active = std::any_cast<std::uint64_t>(t);
      }
      if (keyString == "_lastValue") {
        if (t.type() == typeid(double)) _lastValue = std::any_cast<double>(t);
      }
    }
    auto res = new (UITNPF->Get())
        UITreeNodeFixed{nodeAddress, pythonObjectTypeName, _top,      _left, _width, _height, _displayX, _displayY, _name, _text, _setText, hint,
                        _selected,   ramp_active,          _lastValue};

    auto ReadChildren = [&]() -> void {
      if (maxDepth < 1) return;

      //  https://github.com/Arcitectus/Sanderling/blob/b07769fb4283e401836d050870121780f5f37910/guide/image/2015-01.eve-online-python-ui-tree-structure.png

      if (!childrenDictEntry.has_value()) return;
      // FIX
      Address childrenEntryObjectAddress;
      if (childrenDictEntry._Cast<UITreeNode::DictEntryValueGenericRepresentation>()->address != 0) {
        childrenEntryObjectAddress = childrenDictEntry._Cast<UITreeNode::DictEntryValueGenericRepresentation>()->address;
      } else {
        return;
      }

      //  Console.WriteLine($"'children' dict entry of 0x{nodeAddress:X} points to 0x{childrenEntryObjectAddress:X}.");

      auto pyChildrenListMemory = memory_reader->ReadBytes(childrenEntryObjectAddress, 0x18);
      if (!pyChildrenListMemory) throw std::exception("Read memory failed");

      auto pyChildrenDictAddress = Convert::ToUInt64(pyChildrenListMemory->Raw(), 0x10);

      auto pyChildrenDictEntries = ReadActiveDictionaryEntriesFromDictionaryAddress(pyChildrenDictAddress);

      //  Console.WriteLine($"Found {(pyChildrenDictEntries == null ? "no" : "some")} children dictionary entries for 0x{nodeAddress:X}");

      if (!pyChildrenDictEntries) throw std::exception("Read memory failed");

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

      if (childrenEntry.value == 0) return;

      auto pythonListObjectMemory = memory_reader->ReadBytes(childrenEntry.value, 0x20);

      if (!pythonListObjectMemory) throw std::exception("Read memory failed");

      //  https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/listobject.h

      auto list_ob_size = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x10);

      if (4000 < list_ob_size || 0 >= list_ob_size) return;

      auto listEntriesSize = list_ob_size * 8;

      auto list_ob_item = Convert::ToUInt64(pythonListObjectMemory->Raw(), 0x18);

      auto listEntriesMemory = memory_reader->ReadBytes(list_ob_item, listEntriesSize);

      if (!listEntriesMemory) throw std::exception("Read memory failed");

      auto listEntries = Convert::AsUInt64Array(listEntriesMemory);
      res->nc = 0;
      for (auto a : listEntries) {
        if (++(res->nc) > UITreeNodeFixed::max_children) return;
        res->children[res->nc - 1] = (ReadUITreeFromAddressRFixed(a, maxDepth - 1));
      }
    };
    ReadChildren();
    return res;
  }
  Address GetRootAddress(int32_t processID) {
    auto memory_regions = ReadCommittedMemoryRegionsFromProcess(processID);
    memory_reader = new MemoryReaderFromProcess(processID);
    auto ui_root_candidates_addresses = EnumeratePossibleAddressesForUIRootObjects(memory_regions);
    size_t maxnodes = 0;
    Address root = 0;
    for (auto ura : ui_root_candidates_addresses) {
      auto node = ReadUITreeFromAddress(ura, 3);
      if (node->children.size() >= maxnodes) {
        root = ura;
        maxnodes = node->children.size();
      }
    }
    return root;
  }
  std::map<Address, Size> ReadCommittedMemoryRegionsFromProcess(int32_t processId) {
    MemoryReaderFromProcess mr(processId);

    Address address = 0;

    auto committed_regions = std::map<Address, Size>();

    do {
      MEMORY_BASIC_INFORMATION m;
      auto _ = VirtualQueryEx(mr.handle, (LPCVOID*)address, &m, sizeof(MEMORY_BASIC_INFORMATION64));

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

    return committed_regions;
  }

  std::vector<Address> EnumeratePossibleAddressesForUIRootObjects(const std::map<Address, Size>& mmrgs) {
    std::vector<std::pair<Address, Size>> mrgs;
    for (auto& mrg : mmrgs) {
      mrgs.push_back(mrg);
    }
    auto ReadNullTerminatedAsciiStringFromAddressUpTo255 = [&](Address address) -> std::string {
      auto bytes_before_truncate = memory_reader->ReadBytes(address, 0x100);
      if (!bytes_before_truncate) return "";

      return Convert::AsASCIIString(bytes_before_truncate);
    };
    auto ReadMemoryRegionContentAsULongArray = [&](const std::pair<Address, Size>& mrg) {
      auto length = mrg.second;
      auto data = memory_reader->ReadBytes(mrg.first, length);
      if (!data) return std::vector<uint64_t>();

      return Convert::AsUInt64Array(data);
    };

    auto EnumerateCandidatesForPythonTypeObjectType = [&]() {
      std::unordered_set<Address> res;
      std::atomic<int> idx = 0;
      std::mutex m;
#pragma omp parallel for
      for (int i = 0; i < mrgs.size(); i++) {
        auto& mrg = mrgs[i];
        // std::for_each(std::execution::unseq, mrgs.begin(), mrgs.end(), [&](auto& mrg) {
        //  for (auto& mrg : mrgs) {
        // std::cout << ++idx << "/" << mrgs.size() << std::endl;
        // std::cout << omp_get_thread_num() << std::endl;

        auto mr_content_as_uint64_t = ReadMemoryRegionContentAsULongArray(mrg);

        if (mr_content_as_uint64_t.size() > 0) {
          for (auto candidate_address_index = 0; candidate_address_index < mr_content_as_uint64_t.size() - 4; ++candidate_address_index) {
            auto candidate_address_in_process = mrg.first + candidate_address_index * 8;

            auto candidate_ob_type = mr_content_as_uint64_t[candidate_address_index + 1];

            if (candidate_ob_type != candidate_address_in_process) continue;

            auto candidate_tp_name = ReadNullTerminatedAsciiStringFromAddressUpTo255(mr_content_as_uint64_t[candidate_address_index + 3]);
            // if (candidate_tp_name.size() > 0) std::cout << candidate_tp_name << std::endl;
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
          auto mr_content_as_uint64_t = ReadMemoryRegionContentAsULongArray(mrg);
          if (mr_content_as_uint64_t.size() > 0) {
            for (auto candidate_address_index = 0; candidate_address_index < mr_content_as_uint64_t.size() - 4; ++candidate_address_index) {
              auto candidate_address_in_process = mrg.first + candidate_address_index * 8;

              auto candidate_ob_type = mr_content_as_uint64_t[candidate_address_index + 1];

              if (type_object_candidates_addresses.find(candidate_ob_type) == type_object_candidates_addresses.end()) continue;

              auto candidate_tp_name = ReadNullTerminatedAsciiStringFromAddressUpTo255(mr_content_as_uint64_t[candidate_address_index + 3]);

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
          auto mr_content_as_uint64_t = ReadMemoryRegionContentAsULongArray(mrg);
          if (mr_content_as_uint64_t.size() > 0) {
            for (auto candidate_address_index = 0; candidate_address_index < mr_content_as_uint64_t.size() - 4; ++candidate_address_index) {
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
};
}  // namespace EveOnlineMemoryReading
