#pragma once
template <typename T>
struct ObjectAllocator {
  typedef T value_type;
  ObjectAllocator() noexcept {}
  ObjectAllocator(const custom_allocator&) noexcept {}
  T* allocate(std::size_t n) { return reinterpret_cast(::operator new(n * sizeof(T))); }
  void deallocate(T* p, std::size_t n) { ::operator delete(p); }
};
