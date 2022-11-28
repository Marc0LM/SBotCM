// CSanderling.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <Windows.h>

#include <chrono>
#include <exception>
#include <fstream>
#include <iostream>
#include <thread>

#include "EveUITreeReader.h"
#include "MemoryReaderInProcess.h"

using namespace EveMemoryReading;
uint64_t doread;
std::thread* read;
HANDLE hMapFile, sr, sw;
PVOID pBuf;
EveUITreeReader emr(0);
Address ra;
std::string sznamebase = "Local\\";
std::ofstream logger;
constexpr auto szsize = 1024 + 1024 * 1024;
std::string PB_buf;
std::vector<char> PBArray;
struct UIN {
  uint64_t children[16]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
};
static UITreeNodeFixed tree[8000];
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
int main() {
  // emr = EveUITreeReader(29508);
  //  ra = 2665438592080;
  while (true) {
    int pid;
    std::cin >> pid;
    emr = EveUITreeReader(pid);
    ra = emr.GetRootAddress();
  }
  return 0;
  //    auto ra = 1950930178568;
  auto depth = 64;
  time_t s;
  while (1) {
    auto UITree = emr.ReadUITreeFromAddress(ra, depth);
    auto UITreePB = std::make_unique<UITreeNodePB>();
    TreeToTreePB(UITree.get(), UITreePB.get());
    auto bytesize2 = UITreePB->ByteSizeLong();
    // if (UITreePB->ByteSize() > PB_buf.size()) {
    //   PB_buf.resize(UITreePB->ByteSize() * 2);
    // }
    UITreePB->SerializeToString(&PB_buf);
    std::cout << PB_buf.size() << std::endl;
    std::cout << UITreePB->ByteSizeLong() << std::endl;
    auto t = new char[PB_buf.size()];
    memcpy(t, &PB_buf[0], UITreePB->ByteSizeLong());
  }
  return 0;
}
extern "C" __declspec(dllexport) uint64_t GetUITreeFixed() { return (uint64_t)UITreeNodeFixedPool::begin_; }
extern "C" __declspec(dllexport) uint64_t ReadEveUITree(int32_t pid, uint64_t root_address, uint32_t depth) {
  EveUITreeReader emr(pid);
  emr.ReadUITreeFromAddressFixed(root_address, depth);
  return GetUITreeFixed();
}
extern "C" __declspec(dllexport) uint64_t GetRootAddress(int32_t pid) {
  EveUITreeReader temr(pid);
  auto ra = temr.GetRootAddress();
  return ra;
}

extern "C" __declspec(dllexport) bool WINAPI DllMain(HINSTANCE hInstDll, DWORD fdwReason, LPVOID lpvReserved) {
  switch (fdwReason) {
    case DLL_PROCESS_ATTACH: {
      logger = std::ofstream(std::to_string(GetCurrentProcessId()) + "_ppimpl.txt", std::ios::out);
      std::wstring szname = Convert::ToWString(sznamebase + std::to_string(GetCurrentProcessId()));
      hMapFile = CreateFileMappingW(INVALID_HANDLE_VALUE,  // use paging file
                                    NULL,                  // default security
                                    PAGE_READWRITE,        // read/write access
                                    0,                     // maximum object size (high-order DWORD)
                                    szsize,                // maximum object size (low-order DWORD)
                                    szname.c_str());       // name of mapping object
      if (!hMapFile) return true;
      pBuf = MapViewOfFile(hMapFile,             // handle to map object
                           FILE_MAP_ALL_ACCESS,  // read/write permission
                           0, 0, szsize);
      // pBuf: uint64 ra; uint64 bytesize; uint64 address_buf; uint64 dur; uint64 readmode; uint64 readcount;
      if (!pBuf) return false;
      auto srname = Convert::ToWString(std::to_string(GetCurrentProcessId()) + "R");
      auto swname = Convert::ToWString(std::to_string(GetCurrentProcessId()) + "W");
      sr = CreateSemaphoreW(0, 0, 1, srname.c_str());
      sw = CreateSemaphoreW(0, 0, 1, swname.c_str());
      if (!sr || !sw) return false;
      read = new std::thread([&]() {
        emr = EveUITreeReader(GetCurrentProcessId());
        auto rs = std::chrono::steady_clock::now();
        ra = emr.GetRootAddress();
        auto rdur = std::chrono::duration<double>(std::chrono::steady_clock::now() - rs).count();
        logger << "ra in " << rdur << std::endl;
        memcpy(pBuf, &ra, 8);

        doread = 1;
        auto depth = 20;
        uint64_t read_count = 0;
        while (true) {
          logger << read_count << " ----------------" << std::endl;
          logger << "WFSO" << std::endl;
          WaitForSingleObject(sw, INFINITE);
          // memset((char*)pBuf + 1024, 0, 1024 * 1024);
          logger << "read" << std::endl;
          auto s = std::chrono::steady_clock::now();
          int read_mode;
          memcpy(&read_mode, (char*)pBuf + 32, 8);
          switch (read_mode) {
            default:
              break;
            case 0:
              emr.ReadUITreeFromAddress(ra, depth);
              break;
            case 1: {
              emr.ReadUITreeFromAddressFixed(ra, depth);
              auto bytesize = UITreeNodeFixedPool::count * sizeof(UITreeNodeFixed);
              memcpy((char*)pBuf + 8, &bytesize, 8);
              memcpy((char*)pBuf + 16, &UITreeNodeFixedPool::begin_, 8);
            } break;
            case 2: {  // Decrepated!
              auto UITree = emr.ReadUITreeFromAddressPB(ra, depth);
              auto bytesize = UITree->ByteSize();
              UITree->SerializeToString(&PB_buf);
              memcpy((char*)pBuf + 8, &bytesize, 8);
              memcpy((char*)pBuf + 16, &PB_buf[0], 8);
            } break;
            case 3: {
              auto UITree = emr.ReadUITreeFromAddress(ra, depth);
              auto UITreePB = std::make_unique<UITreeNodePB>();
              TreeToTreePB(UITree.get(), UITreePB.get());
              auto bytesize = UITreePB->ByteSizeLong();
              UITreePB->SerializeToString(&PB_buf);
              memcpy((char*)pBuf + 8, &bytesize, 8);
              memcpy((char*)pBuf + 1024, &PB_buf[0], UITreePB->ByteSizeLong());
            } break;
            case 4: {
              auto UITree = emr.ReadUITreeFromAddress(ra, depth);
              auto UITreePB = std::make_unique<UITreeNodePB>();
              TreeToTreePB(UITree.get(), UITreePB.get());
              auto bytesize = UITreePB->ByteSizeLong();
              if (bytesize > PBArray.capacity()) {
                PBArray.reserve(bytesize * 2);
              }
              UITreePB->SerializeToArray(PBArray.data(), bytesize);
              memcpy((char*)pBuf + 8, &bytesize, 8);
              Address PB_buf_address = (Address)PBArray.data();
              logger << PB_buf_address << std::endl;
              memcpy((char*)pBuf + 16, &PB_buf_address, 8);
            } break;
          }
          read_count++;

          auto dur = std::chrono::duration<double>(std::chrono::steady_clock::now() - s).count();
          logger << dur << std::endl;
          memcpy((char*)pBuf + 24, &dur, 8);
          memcpy((char*)pBuf + 40, &read_count, 8);

          logger << "RS" << std::endl;
          ReleaseSemaphore(sr, 1, 0);
        }
        logger << "END" << std::endl;
      });
      break;
    }

    case DLL_PROCESS_DETACH:
      break;

    case DLL_THREAD_ATTACH:
      break;

    case DLL_THREAD_DETACH:
      break;
  }
  return true;
}
