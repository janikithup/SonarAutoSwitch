using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sonar.AutoSwitch.Services.Win32;

public class NetworkHelper
{
    public static IEnumerable<int> GetPortById(int pid, bool isRemote = true)
    {
        foreach (var row in GetAllTCPv4Connections())
        {
            if (row.owningPid != pid) continue;
            var portBytes = isRemote ? row.remotePort : row.localPort;
            int port = (portBytes[0] << 8) | portBytes[1];
            if (port != 0) yield return port;
        }
    }

    private static List<MIB_TCPROW_OWNER_PID> GetAllTCPv4Connections()
    {
        int buffSize = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL);
        var ptr = Marshal.AllocHGlobal(buffSize);
        try
        {
            if (GetExtendedTcpTable(ptr, ref buffSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL) != 0)
                return [];
            uint count = (uint)Marshal.ReadInt32(ptr);
            var rows = new List<MIB_TCPROW_OWNER_PID>((int)count);
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
            nint rowPtr = (nint)ptr + 4;
            for (uint i = 0; i < count; i++, rowPtr += rowSize)
                rows.Add(Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>((IntPtr)rowPtr));
            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] localPort;
        public uint remoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] remotePort;
        public uint owningPid;
    }

    private const int AF_INET = 2;
    private const int TCP_TABLE_OWNER_PID_ALL = 5;

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength,
        bool sort, int ipVersion, int tcpTableType, int reserved = 0);
}
