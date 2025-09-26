using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UxplayLauncher.Services;

public class DependencyManager
{
    private readonly string _msys2Root = @"C:\msys64";
    private readonly string[] _requiredDlls = {
        "libglib-2.0-0.dll",
        "libcrypt-3-x64.dll",
        "libgstapp-1.0-0.dll",
        "libgobject-2.0-0.dll",
        "libgstreamer-1.0-0.dll",
        "libgstbase-1.0-0.dll",
        "libgstaudio-1.0-0.dll",
        "libgstvideo-1.0-0.dll"
    };

    public bool CopyRequiredDependencies(string targetDirectory)
    {
        try
        {
            var msys2BinPath = Path.Combine(_msys2Root, "mingw64", "bin");
            if (!Directory.Exists(msys2BinPath))
            {
                return false;
            }

            var copiedCount = 0;
            foreach (var dll in _requiredDlls)
            {
                var sourcePath = Path.Combine(msys2BinPath, dll);
                var targetPath = Path.Combine(targetDirectory, dll);

                if (File.Exists(sourcePath) && !File.Exists(targetPath))
                {
                    File.Copy(sourcePath, targetPath, true);
                    copiedCount++;
                }
            }

            return copiedCount > 0;
        }
        catch
        {
            return false;
        }
    }

    public bool AreDependenciesAvailable(string directory)
    {
        return _requiredDlls.All(dll => File.Exists(Path.Combine(directory, dll)));
    }
}
