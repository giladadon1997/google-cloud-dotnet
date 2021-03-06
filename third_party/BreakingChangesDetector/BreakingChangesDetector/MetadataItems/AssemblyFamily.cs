﻿/*
    MIT License

    Copyright(c) 2014-2018 Infragistics, Inc.
    Copyright 2018 Google LLC

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BreakingChangesDetector.MetadataItems
{
    /// <summary>
    /// Represents the a group of externally visible <see cref="AssemblyData"/> instances.
    /// </summary>
    public sealed class AssemblyFamily : IEnumerable<AssemblyData>
    {
        private readonly List<AssemblyData> _assemblies = new List<AssemblyData>();

        internal AssemblyFamily() { }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _assemblies.GetEnumerator();

        /// <summary>
        /// Add an <see cref="AssemblyData"/> instance to the family.
        /// </summary>
        /// <param name="assemblyData">The AssemblyData to add.</param>
        public void Add(AssemblyData assemblyData) => _assemblies.Add(assemblyData);

        /// <summary>
        /// Creates an <see cref="AssemblyFamily"/> instance from a collection of assemblies. 
        /// </summary>
        /// <param name="assemblies">A collection of assemblies all belonging to the same logical group.</param>
        /// <returns>The created <see cref="AssemblyFamily"/> instance.</returns>
        public static AssemblyFamily FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            var context = MetadataResolutionContext.CreateFromAssemblies(assemblies);
            var family = new AssemblyFamily();

            foreach (var assembly in assemblies)
            {
                family.Add(context.GetAssemblyData(assembly));
            }

            return family;
        }

        /// <summary>
        /// Creates an <see cref="AssemblyFamily"/> instance from all assemblies in the specified directory.
        /// </summary>
        /// <param name="path">The path from which to load the assemblies.</param>
        /// <param name="recursive">Indicates whether to recursively search for assemblies in descendant directories</param>
        /// <returns>The created <see cref="AssemblyFamily"/> instance.</returns>
        public static AssemblyFamily FromDirectory(string path, bool recursive = false)
        {
            var assemblies = new List<Assembly>();
            FromDirectoryHelper(assemblies, path, recursive);
            return FromAssemblies(assemblies);
        }

        /// <summary>
        /// Gets the <see cref="AssemblyData"/> with the specified full name in the family.
        /// </summary>
        public AssemblyData GetAssembly(string fullName) =>
            _assemblies.FirstOrDefault(a => a.FullName == fullName);

        /// <summary>
        /// Gets an enumerator capable of iterating the assemblies.
        /// </summary>
        public IEnumerator<AssemblyData> GetEnumerator() => _assemblies.GetEnumerator();

        /// <summary>
        /// Gets a newer <see cref="AssemblyData"/> equivalent to the specified older version.
        /// </summary>
        /// <param name="oldAssembly"></param>
        /// <returns></returns> 
        internal AssemblyData GetEquivalentAssembly(AssemblyData oldAssembly)
        {
            foreach (var newAssembly in _assemblies)
            {
                if (oldAssembly.IsEquivalentToNewAssembly(newAssembly))
                {
                    return newAssembly;
                }
            }

            foreach (var newAssembly in _assemblies)
            {
                var referencedNewAssembly = newAssembly.GetReferencedAssemblies()
                    .Where(a => oldAssembly.IsEquivalentToNewAssembly(a))
                    .FirstOrDefault();

                if (referencedNewAssembly != null)
                {
                    return referencedNewAssembly;
                }
            }

            return null;
        }

        private static void FromDirectoryHelper(List<Assembly> assemblies, string path, bool recursive)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                var extension = Path.GetExtension(file).ToLower();
                if (extension == ".dll" || extension == ".exe")
                {
                    try
                    {
                        assemblies.Add(Assembly.LoadFrom(file));
                    }
                    catch (BadImageFormatException)
                    {
                        // Ignore files that are not .NET assemblies
                    }
                }
            }

            if (recursive)
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    FromDirectoryHelper(assemblies, directory, recursive);
                }
            }
        }
    }
}
