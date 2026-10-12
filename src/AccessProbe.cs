using System;

[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("Assembly-CSharp")]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; private set; }
    }
}

namespace ObenseuerCore
{
    internal static class AccessProbe
    {
        internal static string Run()
        {
            try
            {
                ReadPrivateStatic();
                CallPrivateStatic();
            }
            catch (Exception e)
            {
                Core.RawAccessAvailable = false;
                return "L0 access probe: " + e.GetType().Name + ": " + e.Message;
            }
            Core.RawAccessAvailable = true;
            return "L0 access probe: OK";
        }

        private static string ReadPrivateStatic()
        {
            return SaveController.SaveName;
        }

        private static string CallPrivateStatic()
        {
            return SaveController.NormalizeFolderPath(".");
        }
    }
}
