// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: UITreeNodePB2211.proto

#ifndef GOOGLE_PROTOBUF_INCLUDED_UITreeNodePB2211_2eproto
#define GOOGLE_PROTOBUF_INCLUDED_UITreeNodePB2211_2eproto

#include <limits>
#include <string>

#include <google/protobuf/port_def.inc>
#if PROTOBUF_VERSION < 3021000
#error This file was generated by a newer version of protoc which is
#error incompatible with your Protocol Buffer headers. Please update
#error your headers.
#endif
#if 3021009 < PROTOBUF_MIN_PROTOC_VERSION
#error This file was generated by an older version of protoc which is
#error incompatible with your Protocol Buffer headers. Please
#error regenerate this file with a newer version of protoc.
#endif

#include <google/protobuf/port_undef.inc>
#include <google/protobuf/io/coded_stream.h>
#include <google/protobuf/arena.h>
#include <google/protobuf/arenastring.h>
#include <google/protobuf/generated_message_util.h>
#include <google/protobuf/metadata_lite.h>
#include <google/protobuf/message_lite.h>
#include <google/protobuf/repeated_field.h>  // IWYU pragma: export
#include <google/protobuf/extension_set.h>  // IWYU pragma: export
#include <google/protobuf/map.h>  // IWYU pragma: export
#include <google/protobuf/map_entry_lite.h>
#include <google/protobuf/map_field_lite.h>
// @@protoc_insertion_point(includes)
#include <google/protobuf/port_def.inc>
#define PROTOBUF_INTERNAL_EXPORT_UITreeNodePB2211_2eproto
PROTOBUF_NAMESPACE_OPEN
namespace internal {
class AnyMetadata;
}  // namespace internal
PROTOBUF_NAMESPACE_CLOSE

// Internal implementation detail -- do not use these members.
struct TableStruct_UITreeNodePB2211_2eproto {
  static const uint32_t offsets[];
};
class UITreeNodePB2211;
struct UITreeNodePB2211DefaultTypeInternal;
extern UITreeNodePB2211DefaultTypeInternal _UITreeNodePB2211_default_instance_;
class UITreeNodePB2211_FieldsEntry_DoNotUse;
struct UITreeNodePB2211_FieldsEntry_DoNotUseDefaultTypeInternal;
extern UITreeNodePB2211_FieldsEntry_DoNotUseDefaultTypeInternal _UITreeNodePB2211_FieldsEntry_DoNotUse_default_instance_;
class Value;
struct ValueDefaultTypeInternal;
extern ValueDefaultTypeInternal _Value_default_instance_;
PROTOBUF_NAMESPACE_OPEN
template<> ::UITreeNodePB2211* Arena::CreateMaybeMessage<::UITreeNodePB2211>(Arena*);
template<> ::UITreeNodePB2211_FieldsEntry_DoNotUse* Arena::CreateMaybeMessage<::UITreeNodePB2211_FieldsEntry_DoNotUse>(Arena*);
template<> ::Value* Arena::CreateMaybeMessage<::Value>(Arena*);
PROTOBUF_NAMESPACE_CLOSE

// ===================================================================

class UITreeNodePB2211_FieldsEntry_DoNotUse : public ::PROTOBUF_NAMESPACE_ID::internal::MapEntryLite<UITreeNodePB2211_FieldsEntry_DoNotUse, 
    std::string, ::Value,
    ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::TYPE_STRING,
    ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::TYPE_MESSAGE> {
public:
  typedef ::PROTOBUF_NAMESPACE_ID::internal::MapEntryLite<UITreeNodePB2211_FieldsEntry_DoNotUse, 
    std::string, ::Value,
    ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::TYPE_STRING,
    ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::TYPE_MESSAGE> SuperType;
  UITreeNodePB2211_FieldsEntry_DoNotUse();
  explicit PROTOBUF_CONSTEXPR UITreeNodePB2211_FieldsEntry_DoNotUse(
      ::PROTOBUF_NAMESPACE_ID::internal::ConstantInitialized);
  explicit UITreeNodePB2211_FieldsEntry_DoNotUse(::PROTOBUF_NAMESPACE_ID::Arena* arena);
  void MergeFrom(const UITreeNodePB2211_FieldsEntry_DoNotUse& other);
  static const UITreeNodePB2211_FieldsEntry_DoNotUse* internal_default_instance() { return reinterpret_cast<const UITreeNodePB2211_FieldsEntry_DoNotUse*>(&_UITreeNodePB2211_FieldsEntry_DoNotUse_default_instance_); }
  static bool ValidateKey(std::string* s) {
    return ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::VerifyUtf8String(s->data(), static_cast<int>(s->size()), ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::PARSE, "UITreeNodePB2211.FieldsEntry.key");
 }
  static bool ValidateValue(void*) { return true; }
  friend struct ::TableStruct_UITreeNodePB2211_2eproto;
};

// -------------------------------------------------------------------

class UITreeNodePB2211 final :
    public ::PROTOBUF_NAMESPACE_ID::MessageLite /* @@protoc_insertion_point(class_definition:UITreeNodePB2211) */ {
 public:
  inline UITreeNodePB2211() : UITreeNodePB2211(nullptr) {}
  ~UITreeNodePB2211() override;
  explicit PROTOBUF_CONSTEXPR UITreeNodePB2211(::PROTOBUF_NAMESPACE_ID::internal::ConstantInitialized);

  UITreeNodePB2211(const UITreeNodePB2211& from);
  UITreeNodePB2211(UITreeNodePB2211&& from) noexcept
    : UITreeNodePB2211() {
    *this = ::std::move(from);
  }

  inline UITreeNodePB2211& operator=(const UITreeNodePB2211& from) {
    CopyFrom(from);
    return *this;
  }
  inline UITreeNodePB2211& operator=(UITreeNodePB2211&& from) noexcept {
    if (this == &from) return *this;
    if (GetOwningArena() == from.GetOwningArena()
  #ifdef PROTOBUF_FORCE_COPY_IN_MOVE
        && GetOwningArena() != nullptr
  #endif  // !PROTOBUF_FORCE_COPY_IN_MOVE
    ) {
      InternalSwap(&from);
    } else {
      CopyFrom(from);
    }
    return *this;
  }

  static const UITreeNodePB2211& default_instance() {
    return *internal_default_instance();
  }
  static inline const UITreeNodePB2211* internal_default_instance() {
    return reinterpret_cast<const UITreeNodePB2211*>(
               &_UITreeNodePB2211_default_instance_);
  }
  static constexpr int kIndexInFileMessages =
    1;

  friend void swap(UITreeNodePB2211& a, UITreeNodePB2211& b) {
    a.Swap(&b);
  }
  inline void Swap(UITreeNodePB2211* other) {
    if (other == this) return;
  #ifdef PROTOBUF_FORCE_COPY_IN_SWAP
    if (GetOwningArena() != nullptr &&
        GetOwningArena() == other->GetOwningArena()) {
   #else  // PROTOBUF_FORCE_COPY_IN_SWAP
    if (GetOwningArena() == other->GetOwningArena()) {
  #endif  // !PROTOBUF_FORCE_COPY_IN_SWAP
      InternalSwap(other);
    } else {
      ::PROTOBUF_NAMESPACE_ID::internal::GenericSwap(this, other);
    }
  }
  void UnsafeArenaSwap(UITreeNodePB2211* other) {
    if (other == this) return;
    GOOGLE_DCHECK(GetOwningArena() == other->GetOwningArena());
    InternalSwap(other);
  }

  // implements Message ----------------------------------------------

  UITreeNodePB2211* New(::PROTOBUF_NAMESPACE_ID::Arena* arena = nullptr) const final {
    return CreateMaybeMessage<UITreeNodePB2211>(arena);
  }
  void CheckTypeAndMergeFrom(const ::PROTOBUF_NAMESPACE_ID::MessageLite& from)  final;
  void CopyFrom(const UITreeNodePB2211& from);
  void MergeFrom(const UITreeNodePB2211& from);
  PROTOBUF_ATTRIBUTE_REINITIALIZES void Clear() final;
  bool IsInitialized() const final;

  size_t ByteSizeLong() const final;
  const char* _InternalParse(const char* ptr, ::PROTOBUF_NAMESPACE_ID::internal::ParseContext* ctx) final;
  uint8_t* _InternalSerialize(
      uint8_t* target, ::PROTOBUF_NAMESPACE_ID::io::EpsCopyOutputStream* stream) const final;
  int GetCachedSize() const final { return _impl_._cached_size_.Get(); }

  private:
  void SharedCtor(::PROTOBUF_NAMESPACE_ID::Arena* arena, bool is_message_owned);
  void SharedDtor();
  void SetCachedSize(int size) const;
  void InternalSwap(UITreeNodePB2211* other);

  private:
  friend class ::PROTOBUF_NAMESPACE_ID::internal::AnyMetadata;
  static ::PROTOBUF_NAMESPACE_ID::StringPiece FullMessageName() {
    return "UITreeNodePB2211";
  }
  protected:
  explicit UITreeNodePB2211(::PROTOBUF_NAMESPACE_ID::Arena* arena,
                       bool is_message_owned = false);
  public:

  std::string GetTypeName() const final;

  // nested types ----------------------------------------------------


  // accessors -------------------------------------------------------

  enum : int {
    kChildrenFieldNumber = 2,
    kFieldsFieldNumber = 3,
    kPythonObjectTypeNameFieldNumber = 1,
    kPythonObjectAddressFieldNumber = 4,
  };
  // repeated .UITreeNodePB2211 children = 2;
  int children_size() const;
  private:
  int _internal_children_size() const;
  public:
  void clear_children();
  ::UITreeNodePB2211* mutable_children(int index);
  ::PROTOBUF_NAMESPACE_ID::RepeatedPtrField< ::UITreeNodePB2211 >*
      mutable_children();
  private:
  const ::UITreeNodePB2211& _internal_children(int index) const;
  ::UITreeNodePB2211* _internal_add_children();
  public:
  const ::UITreeNodePB2211& children(int index) const;
  ::UITreeNodePB2211* add_children();
  const ::PROTOBUF_NAMESPACE_ID::RepeatedPtrField< ::UITreeNodePB2211 >&
      children() const;

  // map<string, .Value> fields = 3;
  int fields_size() const;
  private:
  int _internal_fields_size() const;
  public:
  void clear_fields();
  private:
  const ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >&
      _internal_fields() const;
  ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >*
      _internal_mutable_fields();
  public:
  const ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >&
      fields() const;
  ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >*
      mutable_fields();

  // string python_object_type_name = 1;
  void clear_python_object_type_name();
  const std::string& python_object_type_name() const;
  template <typename ArgT0 = const std::string&, typename... ArgT>
  void set_python_object_type_name(ArgT0&& arg0, ArgT... args);
  std::string* mutable_python_object_type_name();
  PROTOBUF_NODISCARD std::string* release_python_object_type_name();
  void set_allocated_python_object_type_name(std::string* python_object_type_name);
  private:
  const std::string& _internal_python_object_type_name() const;
  inline PROTOBUF_ALWAYS_INLINE void _internal_set_python_object_type_name(const std::string& value);
  std::string* _internal_mutable_python_object_type_name();
  public:

  // fixed64 python_object_address = 4;
  void clear_python_object_address();
  uint64_t python_object_address() const;
  void set_python_object_address(uint64_t value);
  private:
  uint64_t _internal_python_object_address() const;
  void _internal_set_python_object_address(uint64_t value);
  public:

  // @@protoc_insertion_point(class_scope:UITreeNodePB2211)
 private:
  class _Internal;

  template <typename T> friend class ::PROTOBUF_NAMESPACE_ID::Arena::InternalHelper;
  typedef void InternalArenaConstructable_;
  typedef void DestructorSkippable_;
  struct Impl_ {
    ::PROTOBUF_NAMESPACE_ID::RepeatedPtrField< ::UITreeNodePB2211 > children_;
    ::PROTOBUF_NAMESPACE_ID::internal::MapFieldLite<
        UITreeNodePB2211_FieldsEntry_DoNotUse,
        std::string, ::Value,
        ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::TYPE_STRING,
        ::PROTOBUF_NAMESPACE_ID::internal::WireFormatLite::TYPE_MESSAGE> fields_;
    ::PROTOBUF_NAMESPACE_ID::internal::ArenaStringPtr python_object_type_name_;
    uint64_t python_object_address_;
    mutable ::PROTOBUF_NAMESPACE_ID::internal::CachedSize _cached_size_;
  };
  union { Impl_ _impl_; };
  friend struct ::TableStruct_UITreeNodePB2211_2eproto;
};
// -------------------------------------------------------------------

class Value final :
    public ::PROTOBUF_NAMESPACE_ID::MessageLite /* @@protoc_insertion_point(class_definition:Value) */ {
 public:
  inline Value() : Value(nullptr) {}
  ~Value() override;
  explicit PROTOBUF_CONSTEXPR Value(::PROTOBUF_NAMESPACE_ID::internal::ConstantInitialized);

  Value(const Value& from);
  Value(Value&& from) noexcept
    : Value() {
    *this = ::std::move(from);
  }

  inline Value& operator=(const Value& from) {
    CopyFrom(from);
    return *this;
  }
  inline Value& operator=(Value&& from) noexcept {
    if (this == &from) return *this;
    if (GetOwningArena() == from.GetOwningArena()
  #ifdef PROTOBUF_FORCE_COPY_IN_MOVE
        && GetOwningArena() != nullptr
  #endif  // !PROTOBUF_FORCE_COPY_IN_MOVE
    ) {
      InternalSwap(&from);
    } else {
      CopyFrom(from);
    }
    return *this;
  }

  static const Value& default_instance() {
    return *internal_default_instance();
  }
  enum KindCase {
    kInt32Value = 1,
    kDoubleValue = 2,
    kStringValue = 3,
    kBoolValue = 4,
    KIND_NOT_SET = 0,
  };

  static inline const Value* internal_default_instance() {
    return reinterpret_cast<const Value*>(
               &_Value_default_instance_);
  }
  static constexpr int kIndexInFileMessages =
    2;

  friend void swap(Value& a, Value& b) {
    a.Swap(&b);
  }
  inline void Swap(Value* other) {
    if (other == this) return;
  #ifdef PROTOBUF_FORCE_COPY_IN_SWAP
    if (GetOwningArena() != nullptr &&
        GetOwningArena() == other->GetOwningArena()) {
   #else  // PROTOBUF_FORCE_COPY_IN_SWAP
    if (GetOwningArena() == other->GetOwningArena()) {
  #endif  // !PROTOBUF_FORCE_COPY_IN_SWAP
      InternalSwap(other);
    } else {
      ::PROTOBUF_NAMESPACE_ID::internal::GenericSwap(this, other);
    }
  }
  void UnsafeArenaSwap(Value* other) {
    if (other == this) return;
    GOOGLE_DCHECK(GetOwningArena() == other->GetOwningArena());
    InternalSwap(other);
  }

  // implements Message ----------------------------------------------

  Value* New(::PROTOBUF_NAMESPACE_ID::Arena* arena = nullptr) const final {
    return CreateMaybeMessage<Value>(arena);
  }
  void CheckTypeAndMergeFrom(const ::PROTOBUF_NAMESPACE_ID::MessageLite& from)  final;
  void CopyFrom(const Value& from);
  void MergeFrom(const Value& from);
  PROTOBUF_ATTRIBUTE_REINITIALIZES void Clear() final;
  bool IsInitialized() const final;

  size_t ByteSizeLong() const final;
  const char* _InternalParse(const char* ptr, ::PROTOBUF_NAMESPACE_ID::internal::ParseContext* ctx) final;
  uint8_t* _InternalSerialize(
      uint8_t* target, ::PROTOBUF_NAMESPACE_ID::io::EpsCopyOutputStream* stream) const final;
  int GetCachedSize() const final { return _impl_._cached_size_.Get(); }

  private:
  void SharedCtor(::PROTOBUF_NAMESPACE_ID::Arena* arena, bool is_message_owned);
  void SharedDtor();
  void SetCachedSize(int size) const;
  void InternalSwap(Value* other);

  private:
  friend class ::PROTOBUF_NAMESPACE_ID::internal::AnyMetadata;
  static ::PROTOBUF_NAMESPACE_ID::StringPiece FullMessageName() {
    return "Value";
  }
  protected:
  explicit Value(::PROTOBUF_NAMESPACE_ID::Arena* arena,
                       bool is_message_owned = false);
  public:

  std::string GetTypeName() const final;

  // nested types ----------------------------------------------------

  // accessors -------------------------------------------------------

  enum : int {
    kInt32ValueFieldNumber = 1,
    kDoubleValueFieldNumber = 2,
    kStringValueFieldNumber = 3,
    kBoolValueFieldNumber = 4,
  };
  // sfixed32 int32_value = 1;
  bool has_int32_value() const;
  private:
  bool _internal_has_int32_value() const;
  public:
  void clear_int32_value();
  int32_t int32_value() const;
  void set_int32_value(int32_t value);
  private:
  int32_t _internal_int32_value() const;
  void _internal_set_int32_value(int32_t value);
  public:

  // double double_value = 2;
  bool has_double_value() const;
  private:
  bool _internal_has_double_value() const;
  public:
  void clear_double_value();
  double double_value() const;
  void set_double_value(double value);
  private:
  double _internal_double_value() const;
  void _internal_set_double_value(double value);
  public:

  // bytes string_value = 3;
  bool has_string_value() const;
  private:
  bool _internal_has_string_value() const;
  public:
  void clear_string_value();
  const std::string& string_value() const;
  template <typename ArgT0 = const std::string&, typename... ArgT>
  void set_string_value(ArgT0&& arg0, ArgT... args);
  std::string* mutable_string_value();
  PROTOBUF_NODISCARD std::string* release_string_value();
  void set_allocated_string_value(std::string* string_value);
  private:
  const std::string& _internal_string_value() const;
  inline PROTOBUF_ALWAYS_INLINE void _internal_set_string_value(const std::string& value);
  std::string* _internal_mutable_string_value();
  public:

  // bool bool_value = 4;
  bool has_bool_value() const;
  private:
  bool _internal_has_bool_value() const;
  public:
  void clear_bool_value();
  bool bool_value() const;
  void set_bool_value(bool value);
  private:
  bool _internal_bool_value() const;
  void _internal_set_bool_value(bool value);
  public:

  void clear_kind();
  KindCase kind_case() const;
  // @@protoc_insertion_point(class_scope:Value)
 private:
  class _Internal;
  void set_has_int32_value();
  void set_has_double_value();
  void set_has_string_value();
  void set_has_bool_value();

  inline bool has_kind() const;
  inline void clear_has_kind();

  template <typename T> friend class ::PROTOBUF_NAMESPACE_ID::Arena::InternalHelper;
  typedef void InternalArenaConstructable_;
  typedef void DestructorSkippable_;
  struct Impl_ {
    union KindUnion {
      constexpr KindUnion() : _constinit_{} {}
        ::PROTOBUF_NAMESPACE_ID::internal::ConstantInitialized _constinit_;
      int32_t int32_value_;
      double double_value_;
      ::PROTOBUF_NAMESPACE_ID::internal::ArenaStringPtr string_value_;
      bool bool_value_;
    } kind_;
    mutable ::PROTOBUF_NAMESPACE_ID::internal::CachedSize _cached_size_;
    uint32_t _oneof_case_[1];

  };
  union { Impl_ _impl_; };
  friend struct ::TableStruct_UITreeNodePB2211_2eproto;
};
// ===================================================================


// ===================================================================

#ifdef __GNUC__
  #pragma GCC diagnostic push
  #pragma GCC diagnostic ignored "-Wstrict-aliasing"
#endif  // __GNUC__
// -------------------------------------------------------------------

// UITreeNodePB2211

// string python_object_type_name = 1;
inline void UITreeNodePB2211::clear_python_object_type_name() {
  _impl_.python_object_type_name_.ClearToEmpty();
}
inline const std::string& UITreeNodePB2211::python_object_type_name() const {
  // @@protoc_insertion_point(field_get:UITreeNodePB2211.python_object_type_name)
  return _internal_python_object_type_name();
}
template <typename ArgT0, typename... ArgT>
inline PROTOBUF_ALWAYS_INLINE
void UITreeNodePB2211::set_python_object_type_name(ArgT0&& arg0, ArgT... args) {
 
 _impl_.python_object_type_name_.Set(static_cast<ArgT0 &&>(arg0), args..., GetArenaForAllocation());
  // @@protoc_insertion_point(field_set:UITreeNodePB2211.python_object_type_name)
}
inline std::string* UITreeNodePB2211::mutable_python_object_type_name() {
  std::string* _s = _internal_mutable_python_object_type_name();
  // @@protoc_insertion_point(field_mutable:UITreeNodePB2211.python_object_type_name)
  return _s;
}
inline const std::string& UITreeNodePB2211::_internal_python_object_type_name() const {
  return _impl_.python_object_type_name_.Get();
}
inline void UITreeNodePB2211::_internal_set_python_object_type_name(const std::string& value) {
  
  _impl_.python_object_type_name_.Set(value, GetArenaForAllocation());
}
inline std::string* UITreeNodePB2211::_internal_mutable_python_object_type_name() {
  
  return _impl_.python_object_type_name_.Mutable(GetArenaForAllocation());
}
inline std::string* UITreeNodePB2211::release_python_object_type_name() {
  // @@protoc_insertion_point(field_release:UITreeNodePB2211.python_object_type_name)
  return _impl_.python_object_type_name_.Release();
}
inline void UITreeNodePB2211::set_allocated_python_object_type_name(std::string* python_object_type_name) {
  if (python_object_type_name != nullptr) {
    
  } else {
    
  }
  _impl_.python_object_type_name_.SetAllocated(python_object_type_name, GetArenaForAllocation());
#ifdef PROTOBUF_FORCE_COPY_DEFAULT_STRING
  if (_impl_.python_object_type_name_.IsDefault()) {
    _impl_.python_object_type_name_.Set("", GetArenaForAllocation());
  }
#endif // PROTOBUF_FORCE_COPY_DEFAULT_STRING
  // @@protoc_insertion_point(field_set_allocated:UITreeNodePB2211.python_object_type_name)
}

// fixed64 python_object_address = 4;
inline void UITreeNodePB2211::clear_python_object_address() {
  _impl_.python_object_address_ = uint64_t{0u};
}
inline uint64_t UITreeNodePB2211::_internal_python_object_address() const {
  return _impl_.python_object_address_;
}
inline uint64_t UITreeNodePB2211::python_object_address() const {
  // @@protoc_insertion_point(field_get:UITreeNodePB2211.python_object_address)
  return _internal_python_object_address();
}
inline void UITreeNodePB2211::_internal_set_python_object_address(uint64_t value) {
  
  _impl_.python_object_address_ = value;
}
inline void UITreeNodePB2211::set_python_object_address(uint64_t value) {
  _internal_set_python_object_address(value);
  // @@protoc_insertion_point(field_set:UITreeNodePB2211.python_object_address)
}

// repeated .UITreeNodePB2211 children = 2;
inline int UITreeNodePB2211::_internal_children_size() const {
  return _impl_.children_.size();
}
inline int UITreeNodePB2211::children_size() const {
  return _internal_children_size();
}
inline void UITreeNodePB2211::clear_children() {
  _impl_.children_.Clear();
}
inline ::UITreeNodePB2211* UITreeNodePB2211::mutable_children(int index) {
  // @@protoc_insertion_point(field_mutable:UITreeNodePB2211.children)
  return _impl_.children_.Mutable(index);
}
inline ::PROTOBUF_NAMESPACE_ID::RepeatedPtrField< ::UITreeNodePB2211 >*
UITreeNodePB2211::mutable_children() {
  // @@protoc_insertion_point(field_mutable_list:UITreeNodePB2211.children)
  return &_impl_.children_;
}
inline const ::UITreeNodePB2211& UITreeNodePB2211::_internal_children(int index) const {
  return _impl_.children_.Get(index);
}
inline const ::UITreeNodePB2211& UITreeNodePB2211::children(int index) const {
  // @@protoc_insertion_point(field_get:UITreeNodePB2211.children)
  return _internal_children(index);
}
inline ::UITreeNodePB2211* UITreeNodePB2211::_internal_add_children() {
  return _impl_.children_.Add();
}
inline ::UITreeNodePB2211* UITreeNodePB2211::add_children() {
  ::UITreeNodePB2211* _add = _internal_add_children();
  // @@protoc_insertion_point(field_add:UITreeNodePB2211.children)
  return _add;
}
inline const ::PROTOBUF_NAMESPACE_ID::RepeatedPtrField< ::UITreeNodePB2211 >&
UITreeNodePB2211::children() const {
  // @@protoc_insertion_point(field_list:UITreeNodePB2211.children)
  return _impl_.children_;
}

// map<string, .Value> fields = 3;
inline int UITreeNodePB2211::_internal_fields_size() const {
  return _impl_.fields_.size();
}
inline int UITreeNodePB2211::fields_size() const {
  return _internal_fields_size();
}
inline void UITreeNodePB2211::clear_fields() {
  _impl_.fields_.Clear();
}
inline const ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >&
UITreeNodePB2211::_internal_fields() const {
  return _impl_.fields_.GetMap();
}
inline const ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >&
UITreeNodePB2211::fields() const {
  // @@protoc_insertion_point(field_map:UITreeNodePB2211.fields)
  return _internal_fields();
}
inline ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >*
UITreeNodePB2211::_internal_mutable_fields() {
  return _impl_.fields_.MutableMap();
}
inline ::PROTOBUF_NAMESPACE_ID::Map< std::string, ::Value >*
UITreeNodePB2211::mutable_fields() {
  // @@protoc_insertion_point(field_mutable_map:UITreeNodePB2211.fields)
  return _internal_mutable_fields();
}

// -------------------------------------------------------------------

// Value

// sfixed32 int32_value = 1;
inline bool Value::_internal_has_int32_value() const {
  return kind_case() == kInt32Value;
}
inline bool Value::has_int32_value() const {
  return _internal_has_int32_value();
}
inline void Value::set_has_int32_value() {
  _impl_._oneof_case_[0] = kInt32Value;
}
inline void Value::clear_int32_value() {
  if (_internal_has_int32_value()) {
    _impl_.kind_.int32_value_ = 0;
    clear_has_kind();
  }
}
inline int32_t Value::_internal_int32_value() const {
  if (_internal_has_int32_value()) {
    return _impl_.kind_.int32_value_;
  }
  return 0;
}
inline void Value::_internal_set_int32_value(int32_t value) {
  if (!_internal_has_int32_value()) {
    clear_kind();
    set_has_int32_value();
  }
  _impl_.kind_.int32_value_ = value;
}
inline int32_t Value::int32_value() const {
  // @@protoc_insertion_point(field_get:Value.int32_value)
  return _internal_int32_value();
}
inline void Value::set_int32_value(int32_t value) {
  _internal_set_int32_value(value);
  // @@protoc_insertion_point(field_set:Value.int32_value)
}

// double double_value = 2;
inline bool Value::_internal_has_double_value() const {
  return kind_case() == kDoubleValue;
}
inline bool Value::has_double_value() const {
  return _internal_has_double_value();
}
inline void Value::set_has_double_value() {
  _impl_._oneof_case_[0] = kDoubleValue;
}
inline void Value::clear_double_value() {
  if (_internal_has_double_value()) {
    _impl_.kind_.double_value_ = 0;
    clear_has_kind();
  }
}
inline double Value::_internal_double_value() const {
  if (_internal_has_double_value()) {
    return _impl_.kind_.double_value_;
  }
  return 0;
}
inline void Value::_internal_set_double_value(double value) {
  if (!_internal_has_double_value()) {
    clear_kind();
    set_has_double_value();
  }
  _impl_.kind_.double_value_ = value;
}
inline double Value::double_value() const {
  // @@protoc_insertion_point(field_get:Value.double_value)
  return _internal_double_value();
}
inline void Value::set_double_value(double value) {
  _internal_set_double_value(value);
  // @@protoc_insertion_point(field_set:Value.double_value)
}

// bytes string_value = 3;
inline bool Value::_internal_has_string_value() const {
  return kind_case() == kStringValue;
}
inline bool Value::has_string_value() const {
  return _internal_has_string_value();
}
inline void Value::set_has_string_value() {
  _impl_._oneof_case_[0] = kStringValue;
}
inline void Value::clear_string_value() {
  if (_internal_has_string_value()) {
    _impl_.kind_.string_value_.Destroy();
    clear_has_kind();
  }
}
inline const std::string& Value::string_value() const {
  // @@protoc_insertion_point(field_get:Value.string_value)
  return _internal_string_value();
}
template <typename ArgT0, typename... ArgT>
inline void Value::set_string_value(ArgT0&& arg0, ArgT... args) {
  if (!_internal_has_string_value()) {
    clear_kind();
    set_has_string_value();
    _impl_.kind_.string_value_.InitDefault();
  }
  _impl_.kind_.string_value_.SetBytes( static_cast<ArgT0 &&>(arg0), args..., GetArenaForAllocation());
  // @@protoc_insertion_point(field_set:Value.string_value)
}
inline std::string* Value::mutable_string_value() {
  std::string* _s = _internal_mutable_string_value();
  // @@protoc_insertion_point(field_mutable:Value.string_value)
  return _s;
}
inline const std::string& Value::_internal_string_value() const {
  if (_internal_has_string_value()) {
    return _impl_.kind_.string_value_.Get();
  }
  return ::PROTOBUF_NAMESPACE_ID::internal::GetEmptyStringAlreadyInited();
}
inline void Value::_internal_set_string_value(const std::string& value) {
  if (!_internal_has_string_value()) {
    clear_kind();
    set_has_string_value();
    _impl_.kind_.string_value_.InitDefault();
  }
  _impl_.kind_.string_value_.Set(value, GetArenaForAllocation());
}
inline std::string* Value::_internal_mutable_string_value() {
  if (!_internal_has_string_value()) {
    clear_kind();
    set_has_string_value();
    _impl_.kind_.string_value_.InitDefault();
  }
  return _impl_.kind_.string_value_.Mutable(      GetArenaForAllocation());
}
inline std::string* Value::release_string_value() {
  // @@protoc_insertion_point(field_release:Value.string_value)
  if (_internal_has_string_value()) {
    clear_has_kind();
    return _impl_.kind_.string_value_.Release();
  } else {
    return nullptr;
  }
}
inline void Value::set_allocated_string_value(std::string* string_value) {
  if (has_kind()) {
    clear_kind();
  }
  if (string_value != nullptr) {
    set_has_string_value();
    _impl_.kind_.string_value_.InitAllocated(string_value, GetArenaForAllocation());
  }
  // @@protoc_insertion_point(field_set_allocated:Value.string_value)
}

// bool bool_value = 4;
inline bool Value::_internal_has_bool_value() const {
  return kind_case() == kBoolValue;
}
inline bool Value::has_bool_value() const {
  return _internal_has_bool_value();
}
inline void Value::set_has_bool_value() {
  _impl_._oneof_case_[0] = kBoolValue;
}
inline void Value::clear_bool_value() {
  if (_internal_has_bool_value()) {
    _impl_.kind_.bool_value_ = false;
    clear_has_kind();
  }
}
inline bool Value::_internal_bool_value() const {
  if (_internal_has_bool_value()) {
    return _impl_.kind_.bool_value_;
  }
  return false;
}
inline void Value::_internal_set_bool_value(bool value) {
  if (!_internal_has_bool_value()) {
    clear_kind();
    set_has_bool_value();
  }
  _impl_.kind_.bool_value_ = value;
}
inline bool Value::bool_value() const {
  // @@protoc_insertion_point(field_get:Value.bool_value)
  return _internal_bool_value();
}
inline void Value::set_bool_value(bool value) {
  _internal_set_bool_value(value);
  // @@protoc_insertion_point(field_set:Value.bool_value)
}

inline bool Value::has_kind() const {
  return kind_case() != KIND_NOT_SET;
}
inline void Value::clear_has_kind() {
  _impl_._oneof_case_[0] = KIND_NOT_SET;
}
inline Value::KindCase Value::kind_case() const {
  return Value::KindCase(_impl_._oneof_case_[0]);
}
#ifdef __GNUC__
  #pragma GCC diagnostic pop
#endif  // __GNUC__
// -------------------------------------------------------------------

// -------------------------------------------------------------------


// @@protoc_insertion_point(namespace_scope)


// @@protoc_insertion_point(global_scope)

#include <google/protobuf/port_undef.inc>
#endif  // GOOGLE_PROTOBUF_INCLUDED_GOOGLE_PROTOBUF_INCLUDED_UITreeNodePB2211_2eproto
