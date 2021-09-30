#include "memory/memory.h"
#include "memory/stack.h"
#include "logging.h"

using namespace icsharp;

ThreadID currentThreadNotConfigured() {
    throw std::logic_error("Current thread getter is not configured!");
}

std::function<ThreadID()> icsharp::currentThread(&currentThreadNotConfigured);

Heap icsharp::heap = Heap();

#ifdef _DEBUG
std::map<unsigned, const char*> icsharp::stringsPool;
int topStringIndex = 0;
#endif

ThreadID lastThreadID = 0;
Stack *currentStack = nullptr;

inline void switchContext() {
    ThreadID tid = currentThread();
    if (tid != lastThreadID) {
        lastThreadID = tid;
        Stack *&s = stacks[tid];
        if (!s) s = new Stack();
        currentStack = s;
    }
}

Stack &icsharp::stack() {
    switchContext();
    return *currentStack;
}

StackFrame &icsharp::topFrame() {
    switchContext();
    return currentStack->topFrame();
}

void icsharp::validateStackEmptyness() {
#ifdef _DEBUG
    for (auto &kv : stacks) {
        if (!kv.second->isEmpty()) {
            FAIL_LOUD("Stack is not empty after program termination!!");
        }
    }
#endif
}

#ifdef _DEBUG
unsigned icsharp::allocateString(const char *s) {
    unsigned currentIndex = topStringIndex;
    // Place s into intern pool
    stringsPool[currentIndex] = s;
//    LOG(tout << "Allocated string '" << s << "' with index '" << currentIndex << "'");
    // Increment top index
    topStringIndex++;
    // Return string's index
    return currentIndex;
}
#endif

unsigned entries_count, data_ptr;
std::vector<char> data;
std::vector<unsigned> dataPtrs;

#define MAX_ENTRIES 3

INT8 icsharp::entriesCount() {
    return (INT8) entries_count;
}

void icsharp::clear_mem() {
    LOG(tout << "clear_mem()" << std::endl);
    entries_count = 0; data_ptr = 0;
    dataPtrs.reserve(MAX_ENTRIES);
    data.reserve(MAX_ENTRIES * sizeof(DOUBLE));
}

void mem(char *value, size_t size) {
    dataPtrs[entries_count++] = data_ptr;
    data_ptr += size;
    memcpy(data.data() + data_ptr - size, value, size);
}

void mem(char *value, size_t size, INT8 idx) {
    ++entries_count;
    dataPtrs[idx] = data_ptr;
    data_ptr += size;
    memcpy(data.data() + data_ptr - size, value, size);
}

void update(char *value, size_t size, INT8 idx) {
    memcpy(data.data() + dataPtrs[idx], value, size);
}

void icsharp::mem_i1(INT8 value) {
    LOG(tout << "mem_i1 " << (INT64) value << std::endl);
    mem((char *) &value, sizeof(INT8));
}

void icsharp::mem_i1(INT8 value, INT8 idx) {
    LOG(tout << "mem_i1 " << value << " " << idx << std::endl);
    mem((char *) &value, sizeof(INT8), idx);
}

void icsharp::mem_i2(INT16 value) {
    LOG(tout << "mem_i2 " << (INT64) value << std::endl);
    mem((char *) &value, sizeof(INT16));
}

void icsharp::mem_i2(INT16 value, INT8 idx) {
    LOG(tout << "mem_i2 " << value << " " << idx << std::endl);
    mem((char *) &value, sizeof(INT16), idx);
}

void icsharp::mem_i4(INT32 value) {
    LOG(tout << "mem_i4 " << (INT64) value << std::endl);
    mem((char *) &value, sizeof(INT32));
}

void icsharp::mem_i4(INT32 value, INT8 idx) {
    LOG(tout << "mem_i4 " << (INT64) value << " " << (INT64) idx << std::endl);
    mem((char *) &value, sizeof(INT32), idx);
}

void icsharp::mem_i8(INT64 value) {
    LOG(tout << "mem_i8 " << (INT64) value << std::endl);
    mem((char *) &value, sizeof(INT64));
}

void icsharp::mem_i8(INT64 value, INT8 idx) {
    LOG(tout << "mem_i8 " << value << " " << idx << std::endl);
    mem((char *) &value, sizeof(INT64), idx);
}

void icsharp::mem_f4(FLOAT value) {
    LOG(tout << "mem_f4 " << value << std::endl);
    mem((char *) &value, sizeof(FLOAT));
}

void icsharp::mem_f4(FLOAT value, INT8 idx) {
    LOG(tout << "mem_f4 " << value << " " << idx << std::endl);
    mem((char *) &value, sizeof(FLOAT), idx);
}

void icsharp::mem_f8(DOUBLE value) {
    LOG(tout << "mem_f8 " << value << std::endl);
    mem((char *) &value, sizeof(DOUBLE));
}

void icsharp::mem_f8(DOUBLE value, INT8 idx) {
    LOG(tout << "mem_f8 " << value << " " << idx << std::endl);
    mem((char *) &value, sizeof(DOUBLE), idx);
}

void icsharp::mem_p(INT_PTR value) {
    LOG(tout << "mem_p " << value << std::endl);
    mem((char *) &value, sizeof(INT_PTR));
}

void icsharp::mem_p(INT_PTR value, INT8 idx) {
    LOG(tout << "mem_p " << value << " " << idx << std::endl);
    mem((char *) &value, sizeof(INT_PTR), idx);
}

void icsharp::update_i1(INT8 value, INT8 idx) {
    LOG(tout << "update_i1 " << (INT64) value << " (index = " << (int)idx << ")" << std::endl);
    update((char *) &value, sizeof(INT8), idx);
}

void icsharp::update_i2(INT16 value, INT8 idx) {
    LOG(tout << "update_i1 " << (INT64) value << " (index = " << (int)idx << ")" << std::endl);
    update((char *) &value, sizeof(INT16), idx);
}

void icsharp::update_i4(INT32 value, INT8 idx) {
    LOG(tout << "update_i4 " << (INT64) value << " (index = " << (int)idx << ")" << std::endl);
    update((char *) &value, sizeof(INT32), idx);
}

void icsharp::update_i8(INT64 value, INT8 idx) {
    LOG(tout << "update_i8 " << (INT64) value << " (index = " << (int)idx << ")" << std::endl);
    update((char *) &value, sizeof(INT64), idx);
}

void icsharp::update_f4(FLOAT value, INT8 idx) {
    LOG(tout << "update_f4 " << (INT64) value << " (index = " << (int)idx << ")" << std::endl);
    update((char *) &value, sizeof(FLOAT), idx);
}

void icsharp::update_f8(DOUBLE value, INT8 idx) {
    LOG(tout << "update_f8 " << (INT64) value << " (index = " << (int)idx << ")" << std::endl);
    update((char *) &value, sizeof(DOUBLE), idx);
}

void icsharp::update_p(INT_PTR value, INT8 idx) {
    LOG(tout << "update_p " << (INT64) value << " (index = " << (int)idx << ")" << std::endl);
    update((char *) &value, sizeof(INT_PTR), idx);
}

INT8 icsharp::unmem_i1(INT8 idx) {
    auto result = *((INT8*) (data.data() + dataPtrs[idx]));
    LOG(tout << "unmem_i1(" << (int)idx << ") returned " << (int) result);
    return result;
//    return *((INT8*) (data.data() + dataPtrs[idx]));
}

INT16 icsharp::unmem_i2(INT8 idx) {
    auto result = *((INT16*) (data.data() + dataPtrs[idx]));
    LOG(tout << "unmem_i2(" << (int)idx << ") returned " << (int) result);
    return result;
//    return *((INT16*) (data.data() + dataPtrs[idx]));
}

INT32 icsharp::unmem_i4(INT8 idx) {
    auto result = *((INT32*) (data.data() + dataPtrs[idx]));
    LOG(tout << "unmem_i4(" << (int)idx << ") returned " << (int) result);
    return result;
//    return *((INT32*) (data.data() + dataPtrs[idx]));
}

INT64 icsharp::unmem_i8(INT8 idx) {
    auto result = *((INT64*) (data.data() + dataPtrs[idx]));
    LOG(tout << "unmem_i8(" << (int)idx << ") returned " << result);
    return result;
//    return *((INT64*) (data.data() + dataPtrs[idx]));
}

FLOAT icsharp::unmem_f4(INT8 idx) {
    auto result = *((FLOAT*) (data.data() + dataPtrs[idx]));
    LOG(tout << "unmem_f4(" << (int)idx << ") returned " << result);
    return result;
//    return *((FLOAT*) (data.data() + dataPtrs[idx]));
}

DOUBLE icsharp::unmem_f8(INT8 idx) {
    auto result = *((DOUBLE*) (data.data() + dataPtrs[idx]));
    LOG(tout << "unmem_f8(" << (int)idx << ") returned " << result);
    return result;
    return *((DOUBLE*) (data.data() + dataPtrs[idx]));
}

INT_PTR icsharp::unmem_p(INT8 idx) {
    auto result = *((INT_PTR*) (data.data() + dataPtrs[idx]));
    LOG(tout << "unmem_p(" << (int)idx << ") returned " << result);
    return result;
//    return *((INT_PTR*) (data.data() + dataPtrs[idx]));
}

bool _mainEntered = false;

void icsharp::mainEntered() {
    _mainEntered = true;
}

bool icsharp::mainLeft() {
    return _mainEntered && stack().isEmpty();
}

VirtualAddress icsharp::resolve(INT_PTR p) {
    // TODO: add stack and statics case #do
    return heap.physToVirtAddress(p);
}
