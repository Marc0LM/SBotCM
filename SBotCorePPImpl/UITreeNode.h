#pragma once

#include <any>
#include <cstdlib>
#include <map>
#include <memory>
#include <string>
#include <vector>

#include "types.h"

class UITreeNode {
 public:
  Address python_object_address_;

  std::string python_object_type_name_;

  std::map<std::string, std::any> dict_entries_of_interest_;
  std::vector<std::shared_ptr<UITreeNode>> children_;
  std::vector<UITreeNode*> EnumerateSelfAndDescendants() {
    auto res = std::vector<UITreeNode*>();
    int maxdepth = 0;
    EnumerateSelfAndDescendants(this, res, maxdepth);
    return res;
  }
  void EnumerateSelfAndDescendants(UITreeNode* node, std::vector<UITreeNode*>& nodes, int& maxdepth) {
    if (!node) return;
    nodes.push_back(node);
    if (node->children_.size() > 0) {
      maxdepth++;
      for (auto& c : node->children_) {
        EnumerateSelfAndDescendants(c.get(), nodes, maxdepth);
      }
    }
  }

  struct DictEntryValueGenericRepresentation {
    Address address;
    std::string python_object_type_name;
  };
};

struct alignas(8) UITreeNodeFixed {
  constexpr static int kMaxChildren = 64;
  // Address python_object_address;

  int64_t _top, _left, _width, _height, _displayX, _displayY;  // 6
  int64_t _selected;                                           // 1
  int8_t active;
  int8_t isDeactivating;
  int16_t quantity;
  int32_t padding;    // 1
  double _lastValue;  // 1

  char _name[512], _text[512], _setText[512], _hint[512];  // 256

  char python_object_type_name[512];  // 64
  uint64_t nc;                        // 1
  uint16_t children[kMaxChildren];    // 16
};
auto sizeofUITreeNodeFixed = sizeof(UITreeNodeFixed);
class UITreeNodeFixedPool {
 public:
  static UITreeNodeFixed* begin_;
  static uint64_t count;
  static UITreeNodeFixed* GetNew() {
    count++;
    return begin_ + count - 1;
  }
  static void Reset() {
    count = 0;
    memset(begin_, 0, 8000 * sizeof(UITreeNodeFixed));
  }
};
UITreeNodeFixed* UITreeNodeFixedPool::begin_ = new UITreeNodeFixed[8000];
uint64_t UITreeNodeFixedPool::count = 0;
