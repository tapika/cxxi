﻿using System;
using System.Text;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public interface IMarshalPrinter : ITypeVisitor<bool>, IDeclVisitor<bool>
    {
        
    }

    public class CLIMarshalNativeToManagedPrinter : IMarshalPrinter
    {
        public TextGenerator Return;

        Library Library { get; set; }
        ITypeMapDatabase TypeMapDatabase { get; set; }
        MarshalContext Context { get; set; }

        public CLIMarshalNativeToManagedPrinter(ITypeMapDatabase database,
            Library library, MarshalContext marshalContext)
        {
            Library = library;
            TypeMapDatabase = database;
            Context = marshalContext;

            Return = new TextGenerator();
            Context.Return = Return;
            Context.MarshalToManaged = this;
        }

        public bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            switch (array.SizeType)
            {
                case ArrayType.ArraySize.Constant:
                    Return.Write("nullptr");
                    break;
                case ArrayType.ArraySize.Variable:
                    Return.Write("nullptr");
                    break;
            }

            return true;
        }

        public bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var returnType = function.ReturnType;
            return returnType.Visit(this, quals);
        }

        public bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            if (pointee.IsPrimitiveType(PrimitiveType.Void, walkTypedefs: true))
            {
                Return.Write("IntPtr({0})", Context.ReturnVarName);
                return true;
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Char))
            {
                Return.Write("clix::marshalString<clix::E_UTF8>({0})",
                             Context.ReturnVarName);
                return true;
            }

            PrimitiveType primitive;
            if (pointee.IsPrimitiveType(out primitive, walkTypedefs: true))
                Return.Write("*");

            if (!pointee.Visit(this, quals))
                return false;

            return true;
        }

        public bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.Int8:
                case PrimitiveType.UInt8:
                case PrimitiveType.Int16:
                case PrimitiveType.UInt16:
                case PrimitiveType.Int32:
                case PrimitiveType.UInt32:
                case PrimitiveType.Int64:
                case PrimitiveType.UInt64:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Return.Write(Context.ReturnVarName);
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.CLIMarshalToManaged(Context);
                return typeMap.IsValueType;
            }

            // TODO: How should function pointers behave here?

            return decl.Type.Visit(this);
        }

        public bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(template, out typeMap))
            {
                typeMap.Type = template;
                typeMap.CLIMarshalToManaged(Context);
                return true;
            }

            return template.Template.Visit(this);
        }

        public bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public bool VisitClassDecl(Class @class)
        {
            var instance = string.Empty;

            if (!Context.ReturnType.IsPointer())
                instance += "&";

            instance += Context.ReturnVarName;

            WriteClassInstance(@class, instance);
            return true;
        }

        public void WriteClassInstance(Class @class, string instance)
        {
            if (@class.IsRefType)
                Return.Write("gcnew ");

            Return.Write("{0}::{1}(", Library.Name, @class.Name);
            Return.Write("(::{0}*)", @class.QualifiedOriginalName);
            Return.Write("{0})", instance);
        }

        public bool VisitFieldDecl(Field field)
        {
            return field.Type.Visit(this);
        }

        public bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public bool VisitParameterDecl(Parameter parameter)
        {
            return parameter.Type.Visit(this, parameter.QualifiedType.Qualifiers);
        }

        public bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public bool VisitEnumDecl(Enumeration @enum)
        {
            Return.Write("({0}){1}", ToCLITypeName(@enum),
                Context.ReturnVarName);
            return true;
        }

        private string ToCLITypeName(Declaration decl)
        {
            var typePrinter = new CLITypePrinter(TypeMapDatabase, Library);
            return typePrinter.VisitDeclaration(decl);
        }

        public bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public bool VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public bool VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public bool VisitDelegate(Delegate @delegate)
        {
            throw new NotImplementedException();
        }
    }

    public class CLIMarshalManagedToNativePrinter : ITypeVisitor<bool>,
                                                    IDeclVisitor<bool>
    {
        public TextGenerator SupportBefore;
        public TextGenerator SupportAfter;
        public TextGenerator Return;
        public TextGenerator VarPrefix;
        public TextGenerator ArgumentPrefix;

        ITypeMapDatabase TypeMapDatabase { get; set; }
        MarshalContext Context { get; set; }

        public CLIMarshalManagedToNativePrinter(ITypeMapDatabase typeMap,
            MarshalContext ctx)
        {
            TypeMapDatabase = typeMap;
            Context = ctx;

            SupportBefore = new TextGenerator();
            SupportAfter = new TextGenerator();
            Return = new TextGenerator();
            VarPrefix = new TextGenerator();
            ArgumentPrefix = new TextGenerator();

            Context.Return = Return;
            Context.MarshalToNative = this;
        }

        public bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var decl = tag.Declaration;
            return decl.Visit(this);
        }

        public bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return false;
        }

        public bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var returnType = function.ReturnType;
            return returnType.Visit(this, quals);
        }

        public bool VisitDelegateType(FunctionType function, string type)
        {
            // We marshal function pointer types by calling
            // GetFunctionPointerForDelegate to get a native function
            // pointer ouf of the delegate. Then we can pass it in the
            // native call. Since references are not tracked in the
            // native side, we need to be careful and protect it with an
            // explicit GCHandle if necessary.

            var sb = new StringBuilder();
            sb.AppendFormat("static_cast<::{0}>(", type);
            sb.Append("System::Runtime::InteropServices::Marshal::");
            sb.Append("GetFunctionPointerForDelegate(");
            sb.AppendFormat("{0}).ToPointer())", Context.Parameter.Name);
            Return.Write(sb.ToString());

            return true;
        }

        public bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            var isVoidPtr = pointee.IsPrimitiveType(PrimitiveType.Void,
                walkTypedefs: true);

            var isUInt8Ptr = pointee.IsPrimitiveType(PrimitiveType.UInt8,
                walkTypedefs: true);

            if (isVoidPtr || isUInt8Ptr)
            {
                if (isUInt8Ptr)
                    Return.Write("({0})", "uint8*");
                Return.Write("{0}.ToPointer()", Context.Parameter.Name);
                return true;
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Char))
            {
                SupportBefore.Write(
                    "auto _{0} = clix::marshalString<clix::E_UTF8>({1});",
                    Context.ArgName, Context.Parameter.Name);

                Return.Write("_{0}.c_str()", Context.ArgName);
                return true;
            }

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                // TODO: We have to translate the function type typedef to C/C++
                return VisitDelegateType(function, function.ToString());
            }

            return pointee.Visit(this, quals);
        }

        public bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return false;
        }

        public bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public bool VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Void:
                    return true;
                case PrimitiveType.Bool:
                case PrimitiveType.Int8:
                case PrimitiveType.UInt8:
                case PrimitiveType.Int16:
                case PrimitiveType.UInt16:
                case PrimitiveType.Int32:
                case PrimitiveType.UInt32:
                case PrimitiveType.Int64:
                case PrimitiveType.UInt64:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    Return.Write(Context.Parameter.Name);
                    return true;
                case PrimitiveType.WideChar:
                    return false;
            }

            return false;
        }

        public bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.CLIMarshalToNative(Context);
                return typeMap.IsValueType;
            }

            FunctionType func;
            if (decl.Type.IsPointerTo<FunctionType>(out func))
            {
                VisitDelegateType(func, typedef.Declaration.OriginalName);
                return true;
            }

            PrimitiveType primitive;
            if (decl.Type.IsPrimitiveType(out primitive, walkTypedefs: true))
            {
                Return.Write("({0})", typedef.Declaration.Name);
            }

            return decl.Type.Visit(this);
        }

        public bool VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                    TypeQualifiers quals)
        {
            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(template, out typeMap))
            {
                typeMap.Type = template;
                typeMap.CLIMarshalToNative(Context);
                return true;
            }

            return template.Template.Visit(this);
        }

        public bool VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public bool VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public bool VisitClassDecl(Class @class)
        {
            if (@class.IsValueType)
            {
                MarshalValueClass(@class);
            }
            else
            {
                MarshalRefClass(@class);
            }

            return true;
        }

        private void MarshalRefClass(Class @class)
        {
            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(@class, out typeMap))
            {
                typeMap.CLIMarshalToNative(Context);
                return;
            }

            if (!Context.Parameter.Type.IsPointer())
            {
                Return.Write("*");

                if (Context.Parameter.Type.IsReference())
                    VarPrefix.Write("&");
            }

            var method = Context.Function as Method;
            if (method != null
                && method.Conversion == MethodConversionKind.FunctionToInstanceMethod
                && Context.ParameterIndex == 0)
            {
                Return.Write("(::{0}*)", @class.QualifiedOriginalName);
                Return.Write("NativePtr");
                return;
            }

            Return.Write("(::{0}*)", @class.QualifiedOriginalName);
            Return.Write("{0}->NativePtr", Context.Parameter.Name);
        }

        private void MarshalValueClass(Class @class)
        {
            if (Context.Parameter.Type.IsReference())
            {
                var argName = string.Format("_{0}", Context.ArgName);
                SupportBefore.Write("auto {0} = (::{1}*)&{2};",
                                    argName, @class.OriginalName,
                                    Context.Parameter.Name);
                Return.Write("*{0}", argName);
                return;
            }

            SupportAfter.PushIndent();

            foreach (var field in @class.Fields)
            {
                SupportAfter.Write("{0}.{1} = ", Context.ArgName,
                                    field.OriginalName);

                var fieldRef = string.Format("{0}.{1}", Context.Parameter.Name,
                                                field.Name);

                var marshalCtx = new MarshalContext(Context.Driver)
                    {
                        ArgName = fieldRef
                    };

                var marshal = new CLIMarshalManagedToNativePrinter(TypeMapDatabase,
                                                                   marshalCtx);
                field.Visit(marshal);

                SupportAfter.WriteLine("{0};", marshal.Return);
            }

            Return.Write("::{0}()", @class.QualifiedOriginalName);

            if (Context.Parameter.Type.IsPointer())
                ArgumentPrefix.Write("&");
        }

        public bool VisitFieldDecl(Field field)
        {
            Context.Parameter = new Parameter
                {
                    Name = Context.ArgName,
                    QualifiedType = field.QualifiedType
                };

            return field.Type.Visit(this);
        }

        public bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public bool VisitParameterDecl(Parameter parameter)
        {
            return parameter.Type.Visit(this);
        }

        public bool VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public bool VisitEnumDecl(Enumeration @enum)
        {
            Return.Write("(::{0}){1}", @enum.QualifiedOriginalName,
                         Context.Parameter.Name);
            return true;
        }

        public bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public bool VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public bool VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public bool VisitDelegate(Delegate @delegate)
        {
            throw new NotImplementedException();
        }
    }
}
