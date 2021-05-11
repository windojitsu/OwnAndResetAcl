/*
 * Win32_TokenPrivileges.cs
 * 
 * by: SA Van Ness, Windojitsu LLC (see LICENSE)
 * rev: 2021-05-10
 * 
 * Interop helpers for enabling/disabling NT user account privileges.
 */

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Win32
{
    internal class TokenPrivileges
    {
        //--------------------------------------------------------------
        // Managed wrappers

        //----------------------------------------
        public static void EnablePrivilege( string privilegeName )
        {
            _AdjustPrivilege(privilegeName, enable: true);
            return;
        }

        //----------------------------------------
        public static void DisablePrivilege( string privilegeName )
        {
            _AdjustPrivilege(privilegeName, enable: false);
            return;
        }

        //--------------------------------------------------------------
        // Managed helpers

        //----------------------------------------
        static void _AdjustPrivilege( string privilegeName, bool enable )
        {
            // Get current process token.
            IntPtr hProcess = new IntPtr(-1);

            IntPtr hToken = IntPtr.Zero;
            if (!_Interop_Advapi32.OpenProcessToken(hProcess, _Interop_Advapi32.TOKEN_ADJUST_PRIVILEGES | _Interop_Advapi32.TOKEN_QUERY, ref hToken))
                throw new Win32Exception();

            // Lookup LUID for priv name.
            Int64 luid = 0L;
            if (!_Interop_Advapi32.LookupPrivilegeValue(null, privilegeName, out luid))
                throw new Win32Exception();

            // Turn the priv on (or off).
            _Interop_Advapi32.TokenPrivilegesWith1LuidAndAttributes tpw1laa;
            tpw1laa.PrivilegeCount = 1;
            tpw1laa.Luid = luid;
            tpw1laa.Attributes = (enable ? _Interop_Advapi32.SE_PRIVILEGE_ENABLED : _Interop_Advapi32.SE_PRIVILEGE_DISABLED);

            if (!_Interop_Advapi32.AdjustTokenPrivileges(hToken, false, ref tpw1laa, 0, IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception();

            // NB: the AdjustTokenPrivileges API doesn't always throw on failure.. need to explicitly check GetLastError().
            int checkLastError = Marshal.GetLastWin32Error();
            if (checkLastError != 0)
                throw new Win32Exception(checkLastError);

            return;
        }

        //--------------------------------------------------------------
        // Interop declarations

        //----------------------------------------
        // Advapi32 Token/Privilege APIs

        static class _Interop_Advapi32
        {
            internal const uint SE_PRIVILEGE_DISABLED = 0x00000000;
            internal const uint SE_PRIVILEGE_ENABLED = 0x00000002;

            internal const int TOKEN_QUERY = 0x00000008;
            internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

            [DllImport("Advapi32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern bool OpenProcessToken( IntPtr hProcess, uint desiredAccess, ref IntPtr phToken );

            [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern bool LookupPrivilegeValue( string hostName, string privName, out Int64 pLuid );

            [DllImport("Advapi32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern bool AdjustTokenPrivileges( 
                IntPtr hToken, 
                bool disableAll,
                ref TokenPrivilegesWith1LuidAndAttributes newState, 
                int unusedBufferLength, IntPtr unusedPrevState, IntPtr unusedReturnLength
            );

            [StructLayout(LayoutKind.Sequential, Pack=4)]
            internal struct TokenPrivilegesWith1LuidAndAttributes
            {
                public uint PrivilegeCount;
                public Int64 Luid;//must use Pack=4 to achieve desired offset.. else model LUID as {uint32,int32} struct per winnt.h
                public uint Attributes;
            }
        }

    }
}