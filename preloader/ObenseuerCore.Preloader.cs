using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
#if !BEPINEX5
using BepInEx.Preloader.Core.Patching;
#endif
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ObenseuerCore.Preloader
{
#if BEPINEX5
    public static class L0Publicizer
    {
#else
    [PatcherPluginInfo("obenseuer.moddingframework.preloader", "Obenseuer Modding Framework Preloader", "0.13.0")]
    public class L0Publicizer : BasePatcher
    {
#endif
        private const string Target = "Assembly-CSharp.dll";
        private const string ConfigFileName = "ObenseuerCore.L0.cfg";
        private const string FsIgnore = "FullSerializer.fsIgnoreAttribute";
        private const string FsIgnoreAssembly = "Assembly-CSharp-firstpass";
        private const string FsProperty = "FullSerializer.fsPropertyAttribute";
        private const string SerializeField = "UnityEngine.SerializeField";
        private const string LitJsonIgnore = "LitJson.JsonIgnore";
        private const string LitJsonInclude = "LitJson.JsonInclude";
        private const string CompilerGenerated = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
        private const string CheckChildFields = "FullSerializer.fsCheckChildFields";
        private static readonly string[] DefaultFlagNames = { "GUID", "global" };
        private static readonly string[] NameLookups = { "GetMethod", "GetField", "GetProperty" };

#if BEPINEX5
        public static IEnumerable<string> TargetDLLs
        {
            get { yield return Target; }
        }

        public static void Patch(ref AssemblyDefinition assembly)
        {
            Publicize(assembly);
        }
#else
        [TargetAssembly(Target)]
        public bool Patch(ref AssemblyDefinition assembly)
        {
            return Publicize(assembly);
        }
#endif

        private static bool Publicize(AssemblyDefinition assembly)
        {
            ManualLogSource log = Logger.CreateLogSource("ObenseuerCore.Preloader");
            PublicizeContext c = null;
            bool applied = false;
            try
            {
                if (!ReadPublicize())
                {
                    log.LogInfo("L0 publicize off (" + ConfigFileName + " Publicize is not true); " + Target + " unchanged");
                    return false;
                }
                if (!FlagHidesFromFs())
                {
                    log.LogWarning("L0 publicize skipped: NonSerialized does not hide fields from FullSerializer on this runtime; " + Target + " unchanged");
                    return false;
                }
                c = new PublicizeContext(assembly.MainModule);
                foreach (TypeDefinition type in c.Module.Types)
                {
                    PlanType(type, c);
                }
                foreach (Action edit in c.Edits)
                {
                    applied = true;
                    edit();
                }
            }
            catch (Exception e)
            {
                log.LogError("L0 publicize failed, " + Target + (applied ? " may be partially patched: " : " left unchanged: ") + e);
                return applied;
            }
            Report(log, c);
            return applied;
        }

        private static bool ReadPublicize()
        {
            ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, ConfigFileName), true);
            return config.Bind("L0", "Publicize", false, "Publicize Assembly-CSharp members in memory at load (disk file untouched). Only true enables it.").Value;
        }

        private sealed class FlagProbe
        {
            [NonSerialized]
            public int Hidden;
        }

        private static bool FlagHidesFromFs()
        {
            return typeof(FlagProbe).GetField("Hidden").IsDefined(typeof(NonSerializedAttribute), true);
        }

        private sealed class PublicizeContext
        {
            public readonly ModuleDefinition Module;
            public readonly MethodReference FsIgnoreCtor;
            public readonly MethodReference JsonIgnoreCtor;
            public readonly HashSet<string> NameGuards = new HashSet<string>(DefaultFlagNames);
            public readonly List<Action> Edits = new List<Action>();
            public readonly SortedDictionary<string, int> Changed = new SortedDictionary<string, int>();
            public readonly SortedDictionary<string, int> Skipped = new SortedDictionary<string, int>();

            public PublicizeContext(ModuleDefinition module)
            {
                Module = module;
                FsIgnoreCtor = FsIgnoreConstructor(module);
                JsonIgnoreCtor = module.ImportReference(module.GetType("LitJson", "JsonIgnore").Methods.First(m => m.IsConstructor && !m.HasParameters));
                foreach (TypeDefinition type in AllTypes(module.Types))
                {
                    foreach (MethodDefinition method in type.Methods)
                    {
                        CollectLookups(method, this);
                    }
                }
            }

            public void Change(string key)
            {
                Count(Changed, key);
            }

            public void Skip(string reason)
            {
                Count(Skipped, reason);
            }

            private static MethodReference FsIgnoreConstructor(ModuleDefinition module)
            {
                AssemblyNameReference scope = module.AssemblyReferences.First(a => a.Name == FsIgnoreAssembly);
                TypeReference type = new TypeReference("FullSerializer", "fsIgnoreAttribute", module, scope);
                MethodReference ctor = new MethodReference(".ctor", module.TypeSystem.Void) { DeclaringType = type, HasThis = true };
                return module.ImportReference(ctor);
            }
        }

        private static void Count(IDictionary<string, int> counts, string key)
        {
            int n;
            counts.TryGetValue(key, out n);
            counts[key] = n + 1;
        }

        private static void CollectLookups(MethodDefinition method, PublicizeContext c)
        {
            if (!method.HasBody || !method.Body.Instructions.Any(ins => IsReflectionCall(ins, NameLookups)))
            {
                return;
            }
            foreach (Instruction ins in method.Body.Instructions)
            {
                if (ins.OpCode == OpCodes.Ldstr)
                {
                    c.NameGuards.Add((string)ins.Operand);
                }
            }
        }

        private static bool IsReflectionCall(Instruction ins, string[] names)
        {
            MethodReference call = ins.Operand as MethodReference;
            return call != null && (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) && call.DeclaringType.FullName == "System.Type" && names.Contains(call.Name);
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

        private static void PlanType(TypeDefinition type, PublicizeContext c)
        {
            if (IsCompilerGeneratedName(type.Name) || Has(type, CompilerGenerated))
            {
                c.Skip("compiler-generated type");
                return;
            }
            TypeAttributes visibility = type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public;
            if ((type.Attributes & TypeAttributes.VisibilityMask) != visibility)
            {
                TypeDefinition target = type;
                if (type.IsNested && c.NameGuards.Contains(type.Name))
                {
                    c.Skip("default-flag name lookup");
                }
                else
                {
                    c.Edits.Add(() => target.Attributes = (target.Attributes & ~TypeAttributes.VisibilityMask) | visibility);
                    c.Change(type.IsNested ? "nested types" : "top-level types");
                }
            }
            foreach (MethodDefinition method in type.Methods)
            {
                PlanMethod(method, c);
            }
            foreach (PropertyDefinition property in type.Properties)
            {
                PlanProperty(property, c);
            }
            foreach (FieldDefinition field in type.Fields)
            {
                PlanField(field, c);
            }
            foreach (TypeDefinition nested in type.NestedTypes)
            {
                PlanType(nested, c);
            }
        }

        private static void PlanMethod(MethodDefinition method, PublicizeContext c)
        {
            if (method.IsPublic)
            {
                return;
            }
            PropertyDefinition owner = method.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == method || p.SetMethod == method);
            if (owner != null && (owner.HasParameters || HasPublicAccessor(owner)))
            {
                c.Skip("property with a public accessor or indexer");
                return;
            }
            if (method.Name == ".cctor")
            {
                c.Skip("static constructor");
                return;
            }
            if (IsCompilerGeneratedName(method.Name) || Has(method, CompilerGenerated))
            {
                c.Skip("compiler-generated method");
                return;
            }
            if (method.IsVirtual)
            {
                c.Skip("virtual method: overrides must keep the base accessibility");
                return;
            }
            if (method.HasOverrides)
            {
                c.Skip("explicit interface implementation");
                return;
            }
            if (c.NameGuards.Contains(MemberName(method)))
            {
                c.Skip("default-flag name lookup");
                return;
            }
            MethodDefinition target = method;
            c.Edits.Add(() => target.Attributes = (target.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public);
            c.Change(method.IsConstructor ? "constructors" : "methods");
        }

        private static void PlanProperty(PropertyDefinition property, PublicizeContext c)
        {
            if (property.HasParameters || c.NameGuards.Contains(property.Name) || HasPublicAccessor(property))
            {
                return;
            }
            MethodDefinition getter = property.GetMethod;
            if (getter != null && !getter.IsStatic && !Has(property, FsProperty) && !Has(property, SerializeField) && !Has(property, FsIgnore))
            {
                AddAttribute(property, c.FsIgnoreCtor, c, "property fsIgnore");
            }
            if (!Has(property, LitJsonInclude) && !Has(property, LitJsonIgnore))
            {
                AddAttribute(property, c.JsonIgnoreCtor, c, "property JsonIgnore");
            }
        }

        private static bool HasPublicAccessor(PropertyDefinition property)
        {
            return (property.GetMethod != null && property.GetMethod.IsPublic) || (property.SetMethod != null && property.SetMethod.IsPublic);
        }

        private static void PlanField(FieldDefinition field, PublicizeContext c)
        {
            if (field.IsPublic)
            {
                return;
            }
            if (field.IsLiteral)
            {
                c.Skip("literal constant");
                return;
            }
            if (field.DeclaringType.IsEnum)
            {
                c.Skip("enum field");
                return;
            }
            if (IsCompilerGeneratedName(field.Name) || Has(field, CompilerGenerated))
            {
                c.Skip("compiler-generated backing field");
                return;
            }
            if (field.DeclaringType.Events.Any(e => e.Name == field.Name))
            {
                c.Skip("event backing field");
                return;
            }
            if (c.NameGuards.Contains(field.Name))
            {
                c.Skip("default-flag name lookup");
                return;
            }
            if (Has(field, SerializeField))
            {
                c.Skip("[SerializeField] left alone");
                return;
            }
            bool isPrivate = (field.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;
            bool hasFsProperty = Has(field, FsProperty);
            if (isPrivate && Has(field, CheckChildFields))
            {
                c.Skip("private field with fsCheckChildFields: ReplaceProperty copy depends on IsPrivate");
                return;
            }
            if (field.IsStatic)
            {
                Publicize(field, c, "static fields", false, false);
                return;
            }
            if (Has(field, FsIgnore))
            {
                Publicize(field, c, "fields already fsIgnore", false, true);
                return;
            }
            if (isPrivate && !hasFsProperty)
            {
                Publicize(field, c, "private fields", true, true);
                return;
            }
            if (hasFsProperty)
            {
                c.Skip(isPrivate
                    ? "private fsProperty: publicizing a base field makes derived ReplaceProperties copy it"
                    : "fsProperty non-public: NotSerialized would hide it from FullSerializer");
                return;
            }
            Publicize(field, c, "protected/internal fields", false, true);
        }

        private static void Publicize(FieldDefinition field, PublicizeContext c, string key, bool fsIgnore, bool notSerialized)
        {
            if (fsIgnore)
            {
                AddAttribute(field, c.FsIgnoreCtor, c, "field fsIgnore");
            }
            if (notSerialized && (field.Attributes & FieldAttributes.NotSerialized) == 0)
            {
                c.Edits.Add(() => field.Attributes |= FieldAttributes.NotSerialized);
                c.Change("NotSerialized flag");
            }
            if (!Has(field, LitJsonInclude) && !Has(field, LitJsonIgnore))
            {
                AddAttribute(field, c.JsonIgnoreCtor, c, "field JsonIgnore");
            }
            c.Edits.Add(() => field.Attributes = (field.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public);
            c.Change(key);
        }

        private static void AddAttribute(ICustomAttributeProvider target, MethodReference ctor, PublicizeContext c, string key)
        {
            CustomAttribute attribute = new CustomAttribute(ctor);
            c.Edits.Add(() => target.CustomAttributes.Add(attribute));
            c.Change(key);
        }

        private static bool Has(ICustomAttributeProvider target, string fullName)
        {
            return target.CustomAttributes.Any(a => a.AttributeType.FullName == fullName);
        }

        private static bool IsCompilerGeneratedName(string name)
        {
            return name.Length > 0 && name[0] == '<';
        }

        private static string MemberName(MethodDefinition method)
        {
            int underscore = method.IsSpecialName ? method.Name.IndexOf('_') : -1;
            return underscore > 0 ? method.Name.Substring(underscore + 1) : method.Name;
        }

        private static void Report(ManualLogSource log, PublicizeContext c)
        {
            log.LogInfo("L0 publicize applied in memory to " + Target + " (disk file untouched)");
            log.LogInfo("L0 changed: " + Join(c.Changed));
            log.LogInfo("L0 skipped: " + Join(c.Skipped));
        }

        private static string Join(SortedDictionary<string, int> counts)
        {
            return string.Join("; ", counts.Select(pair => pair.Key + "=" + pair.Value).ToArray());
        }
    }
}
