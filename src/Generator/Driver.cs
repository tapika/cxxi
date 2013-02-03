﻿using Cxxi.Generators;
using Cxxi.Passes;
using Cxxi.Types;
using System;
using System.Collections.Generic;

namespace Cxxi
{
    public class Driver
    {
        private readonly Options options;
        private readonly ILibrary transform;
        private readonly TypeMapDatabase typeDatabase;
        private Library library;

        public Driver(Options options, ILibrary transform)
        {
            this.options = options;
            this.transform = transform;
            typeDatabase = new TypeMapDatabase();
            typeDatabase.SetupTypeMaps();
        }

        public void ParseCode()
        {
            Console.WriteLine("Parsing code...");

            var headers = new List<string>();
            transform.SetupHeaders(headers);

            var parser = new Parser(options);
            parser.HeaderParsed += (file, success) =>
                Console.WriteLine(success ? "  Parsed '" + file + "'." :
                                            "  Could not parse '" + file + "'.");

            parser.ParseHeaders(headers);
            parser.ParseHeaders(options.Headers);

            library = parser.Library;
        }

        public void ProcessCode()
        {
            // Sort the declarations to be in original order.
            foreach (var unit in library.TranslationUnits)
                SortDeclarations(unit);

            if (transform != null)
                transform.Preprocess(new LibraryHelpers(library));

            var passes = new PassBuilder(library);
            passes.ResolveIncompleteDecls(typeDatabase);
            passes.CleanInvalidDeclNames();
            passes.CheckFlagEnums();

            if (transform != null)
                transform.SetupPasses(passes);

            var transformer = new Transform() { Options = options, Passes = passes };
            transformer.TransformLibrary(library);

            if (transform != null)
                transform.Postprocess(new LibraryHelpers(library));
        }

        private static void SortDeclarations(Namespace @namespace)
        {
            @namespace.Classes.Sort((c, c1) =>
                              (int) (c.DefinitionOrder - c1.DefinitionOrder));

            foreach (var childNamespace in @namespace.Namespaces)
                SortDeclarations(childNamespace);
        }

        public void GenerateCode()
        {
            if (library.TranslationUnits.Count <= 0)
                return;

            Console.WriteLine("Generating wrapper code...");

            var gen = new Generator(options, library, transform, typeDatabase);
            gen.Generate();
        }
    }

    public class Options
    {
        public Options()
        {
            Defines = new List<string>();
            IncludeDirs = new List<string>();
            Headers = new List<string>();
            Assembly = string.Empty;
        }

        public bool Verbose = false;
        public bool ShowHelpText = false;
        public bool OutputDebug = false;
        public string OutputNamespace;
        public string OutputDir;
        public string LibraryName;
        public List<string> Defines;
        public List<string> IncludeDirs;
        public List<string> Headers;
        public string Template;
        public string Assembly;
        public int ToolsetToUse;
    }
}