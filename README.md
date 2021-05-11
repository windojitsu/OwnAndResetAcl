# OwnAndResetAcl #

### *A sanity-saving tool for dealing with those pesky "Windows.old" directories* ###

Reclaim ownership and control of a directory tree.

 * Recursively takes ownership and resets the ACLs of files, directories and symbolic links.
 * Does NOT follow reparse-points (symlinks or junctions).
 * DOES reclaim ownership and restore access to symlinks and junctions.
 * DOES support traversing/accessing long pathnames.
 * DOES include hidden/system files and directories, and empty directories.
 * DOES NOT modify timestamp or attributes of any files/directories.

***USE WITH EXTREME CAUTION: Everything in and under the target directory will be owned by the 'Builtin\Administrators' group, and have a DACL which explicitly grants Everyone full-control.***

The SACL will be empty (no auditing or UAC flagging), and both the DACL and SACL will have the "protected" flag -- no inheritance of permissions from parent.

The SDDL string (eg. if you were to use CACLS.exe or iCACLS.exe for same affect) is: 
`"O:BA G:BA D:P(A;;FA;;;WD) S:P"`

## Usage ##
`	OwnAndResetAcl.exe {targetDirectory}`

- Must be run from elevated (Administrator) cmd prompt.
- Will overwrite the security-descriptor (ownership and access-control) of targetDirectory and everything within that tree, recursively (but without traversing symlinks or junctions).
- Supports enumerating path names longer than MAX_PATH (260) when given an absolute path in the form `"\\?\X:\Very\Long\Path"`

### Examples ###
`	OwnAndResetAcl.exe "\\?\D:\Windows.old"`

### Requirements ###
- .NET Framework 4.8.
- Tested on Windows 10 version 20H2 and later (but should run ok on earlier builds of Windows 10).

## Change Log ##
#### v1.0:
- Initial release.

## Source Notes
- `DirectoryTreeTraverser.cs` implements a simple visitor-pattern to do depth-first traversal of a directory tree with/without traversing reparse-points.
- `Win32_TokenPrivileges.cs` contains a managed wrapper for the surprisingly awkward-to-p/invoke [AdjustTokenPrivileges](https://docs.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-adjusttokenprivileges) API.

## Why is this a thing?
This tool was purpose-built for wrangling "Windows.old" directory trees.. when, for example, moving an old system drive to a new PC.  But I suspect it may prove useful in many similar situations..

It is frankly an embarrassment that Microsoft creates these horrific directory trees, with over-long path names and cyclic symbolic links -- and the builtin commands for dealing with them (TakeOwn.exe, iCacls.exe and Cacls.exe ) all fail when encountering long path names, circular directory tree structures, or both.

PowerShell holds some promise, but as of ver 5.1 (native to Windows 10 build 19043) still doesn't offer easy way to avoid reentrantly looping through circular symlinks.

The GUI option (Explorer properties dialog) can be effective, but it can require multiple passes to acquire ownership and reset ACLs, and it is very slow (perhaps because it relies on propagating auto-inheritance, or because it doesn't handle recursively-cyclical symlink trees well).

Turning on support for long-paths in the OS filesystem policy, makes things *worse* not better -- where previously the circular looping in symlinks would bomb out after exceeding 260 chars, now they don't bomb out until exceeding 32,700 chars -- rendering an already slow process 100x slower.

This has been a customer pain-point for over 15 years, but with the advent of long path name support in .NET Framework v4.6.2, I decided to build a tool to (hopefully) solve it once and for all.