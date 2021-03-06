enum FlagEnum
{
  A = 1 << 0,
  B = 1 << 1,
  C = 1 << 2,
  D = 1 << 3,
};

enum FlagEnum2
{
  A = 1 << 0,
  B = 1 << 1,
  C = 1 << 2,
  D = 1 << 4,
};

class C { };
void DoSomethingC(C*, int);

struct TestRename
{
  int lowerCaseMethod();
  int lowerCaseField;
};

#define TEST_ENUM_ITEM_NAME_0 0
#define TEST_ENUM_ITEM_NAME_1 1
#define TEST_ENUM_ITEM_NAME_2 2