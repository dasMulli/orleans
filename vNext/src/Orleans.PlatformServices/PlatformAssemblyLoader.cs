namespace Orleans.PlatformServices
{
    using System;
    using System.Reflection;
#if NETCORE
    using System.IO;
    using System.Runtime.Loader;
    using System.Collections.Generic;
#endif

    public static class PlatformAssemblyLoader
    {
        public static Assembly LoadFromBytes(byte[] assembly, byte[] debugSymbols = null)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

#if NETCORE
            using (var assemblyStream = new MemoryStream(assembly))
            {
                if (debugSymbols != null)
                {
                    using (var debugSymbolStream = new MemoryStream(debugSymbols))
                    {
                        return AssemblyLoadContext.Default.LoadFromStream(assemblyStream, debugSymbolStream);
                    }
                }
                else
                {
                    return AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
                }
            }
#elif NET46
            return Assembly.Load(assembly, debugSymbols);
#else
            throw new NotImplementedException();
#endif
        }

        public static Assembly LoadFromAssemblyPath(string assemblyPath)
        {
#if NETCORE
            Assembly rootAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            LoadDependencies(rootAssembly, rootAssembly.GetReferencedAssemblies());
            return rootAssembly;
#elif NET46
            return Assembly.LoadFrom(assemblyPath);
#else
            throw new NotImplementedException();
#endif
        }

#if NETCORE
        private static HashSet<Assembly> loadedAssemblies = new HashSet<Assembly>();
        private static void LoadDependencies(Assembly fromAssembly, AssemblyName[] dependencies)
        {
            foreach (var reference in dependencies)
            {
                try
                {
                    var asm = AssemblyLoadContext.Default.LoadFromAssemblyName(reference);
                    if (loadedAssemblies.Add(asm))
                    {
                        LoadDependencies(asm, asm.GetReferencedAssemblies());
                    }
                }
                catch (Exception)
                {
                    //TODO: Should we log it?
                    //logger.Warn(ErrorCode.Provider_AssemblyLoadError, $"Unable to load assembly {reference.FullName} referenced by {fromAssembly.FullName}", ex);
                }
            }
        }
#endif
    }
}