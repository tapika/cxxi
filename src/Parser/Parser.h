/************************************************************************
*
* Cxxi
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include <llvm/Support/Host.h>
#include <clang/Frontend/CompilerInstance.h>
#include <clang/Frontend/CompilerInvocation.h>
#include <clang/Frontend/ASTConsumers.h>
#include <clang/Basic/FileManager.h>
#include <clang/Basic/TargetOptions.h>
#include <clang/Basic/TargetInfo.h>
#include <clang/Basic/IdentifierTable.h>
#include <clang/AST/ASTConsumer.h>
#include <clang/AST/Mangle.h>
#include <clang/AST/RecordLayout.h>
#include <clang/Lex/Preprocessor.h>
#include <clang/Parse/ParseAST.h>
#include <clang/Sema/Sema.h>
#include "CXXABI.h"

#include <string>
#include <cstdarg>

#using <Bridge.dll>
#include <vcclr.h>

#define ARRAY_SIZE(arr) (sizeof(arr) / sizeof(arr[0]))
#define Debug printf

using namespace System::Collections::Generic;

public ref struct ParserOptions
{
    ParserOptions()
    {
        IncludeDirs = gcnew List<System::String^>();
        Defines = gcnew List<System::String^>();
    }

    // Include directories
    List<System::String^>^ IncludeDirs;
    List<System::String^>^ Defines;

    // C/C++ header file name.
    System::String^ FileName;

    Cxxi::Library^ Library;

    // Toolset version - 2005 - 8, 2008 - 9, 2010 - 10, 0 - autoprobe for any.
    int ToolSetToUse;

    bool Verbose;
};


public enum struct ParserDiagnosticLevel
{
    Ignored,
    Note,
    Warning,
    Error,
    Fatal
};


public value struct ParserDiagnostic
{
    System::String^ FileName;
    System::String^ Message;
    ParserDiagnosticLevel Level;
};

public enum struct ParserResultKind
{
    Success,
    Error,
    FileNotFound
};

public ref struct ParserResult
{
    ParserResult()
    {
        Diagnostics = gcnew List<ParserDiagnostic>(); 
    }

    ParserResultKind Kind;
    Cxxi::Library^ Library;
    List<ParserDiagnostic>^ Diagnostics;
};

struct Parser
{
    Parser(ParserOptions^ Opts);

    void Setup(ParserOptions^ Opts);
    ParserResult^ Parse(const std::string& File);

protected:

    // AST traversers
    void WalkAST();
    void WalkMacros(clang::PreprocessingRecord* PR);
    Cxxi::Declaration^ WalkDeclaration(clang::Decl* D, clang::TypeLoc* = 0,
        bool IgnoreSystemDecls = true, bool CanBeDefinition = false);
    Cxxi::Declaration^ WalkDeclarationDef(clang::Decl* D);
    Cxxi::Enumeration^ WalkEnum(clang::EnumDecl*);
    Cxxi::Function^ WalkFunction(clang::FunctionDecl*, bool IsDependent = false);
    Cxxi::Class^ WalkRecordCXX(clang::CXXRecordDecl*, bool IsDependent = false);
    Cxxi::Method^ WalkMethodCXX(clang::CXXMethodDecl*);
    Cxxi::Field^ WalkFieldCXX(clang::FieldDecl*, Cxxi::Class^);
    Cxxi::ClassTemplate^ Parser::WalkClassTemplate(clang::ClassTemplateDecl*);
    Cxxi::FunctionTemplate^ Parser::WalkFunctionTemplate(
        clang::FunctionTemplateDecl*);
    Cxxi::Variable^ WalkVariable(clang::VarDecl*);
    Cxxi::Type^ WalkType(clang::QualType, clang::TypeLoc* = 0,
      bool DesugarType = false);

    // Clang helpers
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(clang::Decl*, clang::TargetCXXABI,
        bool IsDependent = false);
    std::string GetTypeName(const clang::Type*);
    void HandleComments(clang::Decl* D, Cxxi::Declaration^);
    void WalkFunction(clang::FunctionDecl* FD, Cxxi::Function^ F,
        bool IsDependent = false);

    Cxxi::TranslationUnit^ GetModule(clang::SourceLocation Loc);
    Cxxi::Namespace^ GetNamespace(const clang::NamedDecl*);

    int Index;
    gcroot<Cxxi::Library^> Lib;
    llvm::OwningPtr<clang::CompilerInstance> C;
    clang::ASTContext* AST;
};

//-----------------------------------//

typedef std::string String;

String StringFormatArgs(const char* str, va_list args);
String StringFormat(const char* str, ...);
