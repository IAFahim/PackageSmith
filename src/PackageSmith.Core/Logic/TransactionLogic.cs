using System;
using System.IO;
using System.Runtime.CompilerServices;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Logic;

public static class TransactionLogic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateTransaction(string targetPath, out TransactionState state)
    {
        var id = Guid.NewGuid();
        state = new TransactionState
        {
            Id = id,
            TargetPath = targetPath,
            TempPath = Path.Combine(Path.GetTempPath(), $"pksmith_{id}"),
            IsCommitted = false,
            Timestamp = DateTime.UtcNow.Ticks
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetShadowPath(in TransactionState state, string relativePath, out string shadowPath)
    {
        shadowPath = Path.Combine(state.TempPath, relativePath);
    }
}