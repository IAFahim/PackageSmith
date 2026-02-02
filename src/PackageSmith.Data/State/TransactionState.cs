using System;
using System.Runtime.InteropServices;

namespace PackageSmith.Data.State;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct TransactionState
{
    public Guid Id;
    public string TargetPath;
    public string TempPath;
    public bool IsCommitted;
    public long Timestamp;

    public readonly override string ToString()
    {
        return $"[Tx] {Id} -> {TempPath} ({(IsCommitted ? "COMMITTED" : "PENDING")})";
    }
}