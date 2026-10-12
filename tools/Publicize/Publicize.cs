using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

public static class Publicize
{
    private sealed class Stats
    {
        public int Types;
        public int Methods;
        public int Fields;
        public int SkippedEventFields;
    }

    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: Publicize <input.dll> [outDir]");
            return 2;
        }

        string input = Path.GetFullPath(args[0]);
        string outDir = Path.GetFullPath(args.Length > 1 ? args[1] : "lib");
        Directory.CreateDirectory(outDir);
        string output = Path.Combine(outDir, Path.GetFileNameWithoutExtension(input) + ".publicized.dll");

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(input));
        AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters { AssemblyResolver = resolver });

        var stats = new Stats();
        foreach (TypeDefinition type in AllTypes(assembly.MainModule.Types))
        {
            PublicizeType(type, stats);
        }

        assembly.Write(output);
        Console.WriteLine("publicized types={0} methods={1} fields={2} skippedEventBackingFields={3}", stats.Types, stats.Methods, stats.Fields, stats.SkippedEventFields);
        Console.WriteLine("wrote " + output + " (" + new FileInfo(output).Length + " bytes)");
        return 0;
    }

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (TypeDefinition type in types)
        {
            yield return type;
            foreach (TypeDefinition nested in AllTypes(type.NestedTypes))
            {
                yield return nested;
            }
        }
    }

    private static void PublicizeType(TypeDefinition type, Stats stats)
    {
        TypeAttributes visibility = type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public;
        if ((type.Attributes & TypeAttributes.VisibilityMask) != visibility)
        {
            type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | visibility;
            stats.Types++;
        }

        foreach (MethodDefinition method in type.Methods)
        {
            if ((method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
            {
                method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
                stats.Methods++;
            }
        }

        HashSet<string> eventNames = new HashSet<string>(type.Events.Select(e => e.Name));
        foreach (FieldDefinition field in type.Fields)
        {
            if (eventNames.Contains(field.Name))
            {
                if ((field.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public) stats.SkippedEventFields++;
                continue;
            }
            if ((field.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public)
            {
                field.Attributes = (field.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
                stats.Fields++;
            }
        }
    }
}
