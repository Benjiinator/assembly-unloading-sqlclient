using Host.AssemblyLoading.Tools;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Host.AssemblyLoading
{
    public static class Module
    {
        public static void Run<T>(string executeModule, string resolveModule, Action<T> action) where T : class
        {
            WeakReference hostAlcWeakRef;
            string currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var basePath = Path.Combine(currentAssemblyDirectory, $"..\\..\\..\\..\\");

            string moduleFullPath = Path.Combine(basePath, $"{executeModule}\\bin\\Debug\\net5.0\\win-x64\\{executeModule}.dll");
            string resolveFullPath = Path.Combine(basePath, $"{resolveModule}\\bin\\Debug\\net5.0\\win-x64\\{resolveModule}.dll");

            ExecuteAndUnload<T>(moduleFullPath, resolveFullPath, action, out hostAlcWeakRef);

            // Poll and run GC until the AssemblyLoadContext is unloaded.
            // You don't need to do that unless you want to know when the context
            // got unloaded. You can just leave it to the regular GC.
            for (int i = 0; hostAlcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Console.WriteLine($"Unload {executeModule} success: {!hostAlcWeakRef.IsAlive}");
        }

        // It is important to mark this method as NoInlining, otherwise the JIT could decide
        // to inline it into the Main method. That could then prevent successful unloading
        // of the plugin because some of the MethodInfo / Type / Plugin.Interface / HostAssemblyLoadContext
        // instances may get lifetime extended beyond the point when the plugin is expected to be
        // unloaded.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ExecuteAndUnload<T>(string modulePath, string resolvePath, Action<T> action, out WeakReference alcWeakRef) where T : class
        {
            // Create the unloadable HostAssemblyLoadContext
            var alc = new ModuleAssemblyLoadContext(resolvePath);

            // Load System.ComponentModel.TypeDescriptor to the ALC beforehand,
            // as otherwise the internal caches in it will block unloading the ALC: https://github.com/dotnet/coreclr/issues/26271
            // The netstandard assembly shim also needs to be loaded explicitly for the TypeDescriptor to be loaded.
            var typeDescriptorAssemblyPath = typeof(TypeDescriptor).Assembly.Location;
            alc.LoadFromAssemblyPath(typeDescriptorAssemblyPath);
            var netstandardShimAssemblyPath = Path.Combine(Path.GetDirectoryName(typeDescriptorAssemblyPath), "netstandard.dll");
            alc.LoadFromAssemblyPath(netstandardShimAssemblyPath);

            // Create a weak reference to the AssemblyLoadContext that will allow us to detect
            // when the unload completes.
            alcWeakRef = new WeakReference(alc);

            // Load the plugin assembly into the HostAssemblyLoadContext. 
            // NOTE: the assemblyPath must be an absolute path.
            Assembly a = alc.LoadFromAssemblyPath(modulePath);

            var assemblyResolverTool = new AssemblyResolverTool();

            assemblyResolverTool.dependencyContext = DependencyContext.Load(a);

            assemblyResolverTool.assemblyResolver = new CompositeCompilationAssemblyResolver
                                    (new ICompilationAssemblyResolver[]
            {
            new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(modulePath)),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver()
            });

            alc.Resolving += assemblyResolverTool.OnResolving;

            var module = ResolveTool.ResolveInstance<T>(a);

            action(module);

            // This initiates the unload of the HostAssemblyLoadContext. The actual unloading doesn't happen
            // right away, GC has to kick in later to collect all the stuff.
            alc.Resolving -= assemblyResolverTool.OnResolving;
            alc.Unload();
        }
    }
}
