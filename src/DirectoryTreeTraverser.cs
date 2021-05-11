/*
 * DirectoryTreeTraverser.cs
 * 
 * by: SA Van Ness, Windojitsu LLC (see LICENSE)
 * rev: 2021-05-10
 * 
 * Helper for traversing directory-trees..
 * - Visitor pattern: directories are processed by delegate-callback.
 * - Subdirectories are visited in depth-first, sorted (ordinal, ignore-case) order.
 * - Includes hidden- and system-subdirectories.
 * - Reparse-points (junctions and symlinks) are visited, but traversal is optional.
 * - Supports relative or absolute path argument.
 * - File/directory path names longer than 260 can be traversed, when starting with an absolute 
 * pathname in the form of "\\?\X:\Some\Very\Long\Path"
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace Jitsu.IO
{
    internal class DirectoryTreeTraverser
    {
        private readonly VisitorCallback _onDirectory;
        private readonly VisitorCallback _onFile;

        //--------------------------------------------------------------
        // Initialization

        public DirectoryTreeTraverser( VisitorCallback onDirectory, VisitorCallback onFile=null )
        {
            _onDirectory = onDirectory;
            _onFile = onFile;
        }

        //--------------------------------------------------------------
        // Interface

        //----------------------------------------
        public delegate bool VisitorCallback( string path );

        //----------------------------------------
        public void TraverseDepthFirstSorted( string baseDirectory, bool traverseLinks = false )
        {
            // Prime the LIFO stack.
            Stack<string> dfsStack = new Stack<string>(20);
            dfsStack.Push(baseDirectory);

            // Depth-first: process LIFO stack until empty.
            while (dfsStack.Count > 0)
            {
                string dir = dfsStack.Pop();
                bool proceed = _onDirectory.Invoke(dir);

                // Don't walk into junctions/symlinks, unless opted in.
                DirectoryInfo dinfo = new DirectoryInfo(dir);
                bool linkOrJunction = dinfo.Exists && dinfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
                if (linkOrJunction && !traverseLinks)
                    proceed = false;

                if (proceed)
                {
                    // Visit files.
                    if (_onFile != null)
                    {
                        string[] files = Directory.GetFiles(dir);
                        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                        foreach (string s in files)
                            _onFile(s);
                    }

                    // Post subdirectories to LIFO stack, in reverse-sorted order.
                    string[] subdirs = Directory.GetDirectories(dir);
                    Array.Sort(subdirs, StringComparer.OrdinalIgnoreCase);
                    Array.Reverse(subdirs);
                    foreach (string s in subdirs)
                        dfsStack.Push(s);
                }

                continue;
            }

            return;
        }

    }
}