/*
 * Program.cs
 * 
 * by: SA Van Ness, Windojitsu LLC (see LICENSE)
 * rev: 2021-05-10
 * 
 * Recursively take ownership and reset ACL for files/directories.
 * 
 * [√] Enable necessary admin privs, to take ownership and reset ACLs for all files/directories.
 * [√] DON'T follow symlinks/junctions.
 * [√] DO operate on symlinks/junctions.
 * [√] DO support traversing/accessing long pathnames.
 * [√] DO include hidden/system files and directories, and empty directories.
 * [√] DON'T modify timestamp or attributes of any files/directories.
 */

using System;
using System.Security;
using System.Security.AccessControl;
using System.IO;

using Jitsu.IO;

namespace OwnAndResetAcl
{
    class Program
    {
        //----------------------------------------
        const string SddlString = @"O:BA G:BA D:P (A;;FA;;;WD) S:P";
        //Owner="Builtin\Administrators"
        //Group="Builtin\Administrators"
        //DACL=Protected, no AutoInherit, grant FullAccess to Everyone
        //SACL=Protected, no AutoInherit, no auditing, no UAC labels
        //
        // In theory a NULL DACL would be leaner/faster, but in practice that seems to cause havok
        // for some tools or processes within Windows. So for now we'll stick with the DACL above,
        // which explicitly grants Everyone:F access.
        //
        //const string SddlString = @"O:BA G:BA D:P NO_ACCESS_CONTROL S:P NO_ACCESS_CONTROL";
        //DACL=Protected,NULL (no inheritance; full control for everyone)
        //SACL=Protected,NULL (no inheritance; no auditing, no UAC labels)

        //----------------------------------------
        public static int Main( string[] args )
        {
            try
            {
                // Ensure we're elevated/administrator, and activate necessary privs to modify ownership/acls.
                Win32.TokenPrivileges.EnablePrivilege("SeBackupPrivilege"); //read any file/secdesc
                Win32.TokenPrivileges.EnablePrivilege("SeRestorePrivilege"); //write any file/secdesc
                Win32.TokenPrivileges.EnablePrivilege("SeTakeOwnershipPrivilege"); //take ownership of any file
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: "+ex.Message);
                Console.Error.WriteLine("(This program must be run as administrator / elevated.)");
                return Int32.MinValue;
            }

            try
            {
                return  _MainImpl(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return Int32.MinValue;
            }
        }

        //----------------------------------------
        static int _MainImpl( string[] args )
        {
            // Parse command-line.
            if (args.Length == 1)
            {
                // If target is singular file..
                if (File.Exists(args[0]))
                {
                    Console.WriteLine("Updating file: " + args[0]);

                    _VisitFile(args[0]);
                    return 0;//done
                }

                // If target is directory..
                if (Directory.Exists(args[0]))
                {
                    Console.WriteLine("Updating directory: " + args[0]);

                    // Walk the tree.. visit, but do not traverse, junction/symlink reparse-points.
                    DirectoryTreeTraverser walker = new DirectoryTreeTraverser(_VisitDirectory, _VisitFile);
                    walker.TraverseDepthFirstSorted(args[0], traverseLinks: false);

                    return 0;//done
                }

                throw new DirectoryNotFoundException("File or Directory not found: " + args[0]);
            }
            else
            {
                // Show usage description/samples.
                Stream usage = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Program), @"Usage.txt");
                using (StreamReader reader = new StreamReader(usage, System.Text.Encoding.UTF8))
                    Console.Error.WriteLine(reader.ReadToEnd());
            }

            return Int32.MinValue;
        }

        //----------------------------------------
        static bool _VisitDirectory( string path )
        {
            Console.WriteLine();
            Console.WriteLine(path);

            // Nullify the directory's security-descriptor.
            DirectorySecurity secdesc = new DirectorySecurity();
            secdesc.SetSecurityDescriptorSddlForm(SddlString, AccessControlSections.All);
            Directory.SetAccessControl(path, secdesc);

            return true;
        }

        //----------------------------------------
        static bool _VisitFile( string path )
        {
            Console.WriteLine(path);

            // Nullify the file's security-descriptor.
            FileSecurity secdesc = new FileSecurity();
            secdesc.SetSecurityDescriptorSddlForm(SddlString, AccessControlSections.All);
            File.SetAccessControl(path, secdesc);

            return true;
        }

    }
}
