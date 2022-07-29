// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    internal class RenameAssemblyProcessor : IAssemblyDefinitionProcessor
    {
        private string assemblyName;

        public RenameAssemblyProcessor(string assemblyName)
        {
            this.assemblyName = assemblyName;
        }

        public bool Process(AssemblyProcessorContext context)
        {
            var ist = (assy: context.Assembly.Name.Name, module: context.Assembly.MainModule.Name);
            var soll = (assy: assemblyName, module: assemblyName + ".dll");
            if (ist == soll)
                return false;

            context.Assembly.Name.Name = soll.assy;
            context.Assembly.MainModule.Name = soll.module;
            APUtilities.Diagnostic(context.Log, error: false, diagnostic: null, nameof(RenameAssemblyProcessor),
                $"Renamed {ist.assy} (Module {ist.module}) to {soll.assy} (Module {soll.module}).");
            return true;
        }
    }
}
