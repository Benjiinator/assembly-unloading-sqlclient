using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace Host.AssemblyLoading.Tools
{
    public class AssemblyInfoTool
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Print(string status)
        {
            var assemblyInfo = new StringBuilder();
            assemblyInfo.Append($"Status: {status}");
            foreach (var loadContext in AssemblyLoadContext.All)
            {
                assemblyInfo.AppendLine(string.Empty);
                assemblyInfo.Append($"LoadContext name: {loadContext.Name}");
                assemblyInfo.AppendLine(string.Empty);
                foreach (var item in loadContext.Assemblies.OrderBy(x => x.FullName))
                {
                    assemblyInfo.Append($"Assembly: {item.FullName}");
                    assemblyInfo.AppendLine(string.Empty);

                }
            }
            Console.WriteLine(assemblyInfo.ToString());
        }
    }

}
