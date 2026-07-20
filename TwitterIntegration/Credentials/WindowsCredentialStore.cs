using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Divination.TwitterIntegration.Credentials;

public sealed class WindowsCredentialStore : ICredentialStore
{
    private const uint CredTypeGeneric = 1;
    private const uint CredPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public string? Read(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!CredRead(key, CredTypeGeneric, 0, out var credentialPtr))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound)
            {
                return null;
            }

            throw new Win32Exception(error, "Unable to read a credential from Windows Credential Manager.");
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPtr);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
            {
                return string.Empty;
            }

            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes);
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public void Write(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrEmpty(value);

        var bytes = Encoding.Unicode.GetBytes(value);
        var blob = Marshal.AllocCoTaskMem(bytes.Length);

        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);

            var credential = new NativeCredential
            {
                Type = CredTypeGeneric,
                TargetName = key,
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blob,
                Persist = CredPersistLocalMachine,
                UserName = Environment.UserName,
            };

            if (!CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to write a credential to Windows Credential Manager.");
            }
        }
        finally
        {
            ZeroMemory(blob, bytes.Length);
            Marshal.FreeCoTaskMem(blob);
            Array.Clear(bytes, 0, bytes.Length);
        }
    }

    public void Delete(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (CredDelete(key, CredTypeGeneric, 0))
        {
            return;
        }

        var error = Marshal.GetLastWin32Error();
        if (error != ErrorNotFound)
        {
            throw new Win32Exception(error, "Unable to delete a credential from Windows Credential Manager.");
        }
    }

    private static void ZeroMemory(IntPtr pointer, int length)
    {
        for (var index = 0; index < length; index++)
        {
            Marshal.WriteByte(pointer, index, 0);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite([In] ref NativeCredential credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);
}
