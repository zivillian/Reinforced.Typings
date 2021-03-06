﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reinforced.Typings.Exceptions;
using Reinforced.Typings.Visitors.TypeScript;
using Reinforced.Typings.Visitors.Typings;

namespace Reinforced.Typings
{
    internal class FilesOperations : IFilesOperations
    {
        private readonly List<string> _tmpFiles = new List<string>();

        public ExportContext Context { get; set; }

        public void DeployTempFiles()
        {
            foreach (var tmpFile in _tmpFiles)
            {
                var origFile = Path.GetFileNameWithoutExtension(tmpFile);
                var origDir = Path.GetDirectoryName(tmpFile);
                origFile = Path.Combine(origDir, origFile);
                try
                {
                    if (File.Exists(origFile)) File.Delete(origFile);
                    File.Move(tmpFile, origFile);
#if DEBUG
                    Console.WriteLine("File replaced: {0} -> {1}", tmpFile, origFile);
#endif
                }
                catch (Exception ex)
                {
                    ErrorMessages.RTE0002_DeployingFilesError.Throw(origFile, ex.Message);
                }
            }
        }

        protected virtual void ExportCore(StreamWriter tw, ExportedFile file)
        {
            var visitor = Context.Global.ExportPureTypings ? new TypingsExportVisitor(tw, Context.Global.TabSymbol) : new TypeScriptExportVisitor(tw, Context.Global.TabSymbol);
            WriteWarning(tw);
            foreach (var rtReference in file.References.References)
            {
                visitor.Visit(rtReference);
            }

            foreach (var rtImport in file.References.Imports)
            {
                visitor.Visit(rtImport);
            }
            if (file.References.References.Any() || file.References.Imports.Any()) tw.WriteLine();
            foreach (var fileNamespace in file.Namespaces)
            {
                visitor.Visit(fileNamespace);
            }
        }

        public void Export(string fileName, ExportedFile file)
        {
            using (var fs = GetTmpFile(fileName))
            {
                using (var tw = new StreamWriter(fs))
                {
                    ExportCore(tw, file);
                }
            }
        }

        private void WriteWarning(TextWriter tw)
        {
            if (Context.Global.WriteWarningComment)
            {
                tw.WriteLine("//     This code was generated by a Reinforced.Typings tool. ");
                tw.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
                tw.WriteLine("//     the code is regenerated.");
                tw.WriteLine();
            }
        }

        private Stream GetTmpFile(string fileName)
        {
            fileName = fileName + ".tmp";
            try
            {
                var dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
#if DEBUG
                Console.WriteLine("Temp file aquired: {0}", fileName);
#endif
                _tmpFiles.Add(fileName);
            }
            catch (Exception ex)
            {
                ErrorMessages.RTE0001_TempFileError.Throw(fileName, ex.Message);
            }

            return File.OpenWrite(fileName);
        }


        public void ClearTempRegistry()
        {
            _tmpFiles.Clear();
        }
    }

    internal static class ArrayExtensions
    {
        public static bool PartialCompare(string[] array1, string[] array2, int idx)
        {
            var minLen = array1.Length > array2.Length ? array2.Length : array1.Length;
            if (idx > minLen) return false;
            for (int i = 0; i < idx; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }
    }
}