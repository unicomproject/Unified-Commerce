using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;
using Microsoft.Extensions.Options;

namespace E_POS.LocalPrintAgent.Printing;

public sealed class WindowsRawPrinterService(
    IOptions<PrintAgentOptions> options,
    ILogger<WindowsRawPrinterService> logger) : IPrinterService
{
    private const uint PrinterStatusPaused = 0x00000001;
    private const uint PrinterStatusError = 0x00000002;
    private const uint PrinterStatusPaperJam = 0x00000008;
    private const uint PrinterStatusPaperOut = 0x00000010;
    private const uint PrinterStatusOffline = 0x00000080;
    private const uint PrinterStatusNotAvailable = 0x00001000;
    private const uint PrinterStatusDoorOpen = 0x00400000;
    private const uint PrinterStatusUserIntervention = 0x00100000;
    private const uint BlockingStatus = PrinterStatusPaused | PrinterStatusError | PrinterStatusPaperJam |
                                        PrinterStatusPaperOut | PrinterStatusOffline | PrinterStatusNotAvailable |
                                        PrinterStatusDoorOpen | PrinterStatusUserIntervention;
    private readonly PrintAgentOptions _options = options.Value;

    public async Task<PrinterHealth> GetHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(GetHealthCore, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(_options.SpoolerTimeoutSeconds), cancellationToken);
        }
        catch (System.TimeoutException)
        {
            logger.LogWarning("Windows spooler status query timed out for printer {PrinterName}.", _options.PrinterName);
            return new PrinterHealth("degraded", _options.PrinterName, false, false,
                "Windows spooler status query timed out.");
        }
    }

    private PrinterHealth GetHealthCore()
    {
        if (!OperatingSystem.IsWindows())
            return new PrinterHealth("running", _options.PrinterName, false, false,
                "Windows spooler is available only when the agent runs on Windows.");

        string spoolerStatus;
        try
        {
            using var spooler = new ServiceController("Spooler");
            spoolerStatus = spooler.Status.ToString();
            if (spooler.Status != ServiceControllerStatus.Running)
                return new PrinterHealth(
                    "degraded", _options.PrinterName, false, false,
                    $"Windows Print Spooler is {spoolerStatus}.",
                    SpoolerStatus: spoolerStatus);
        }
        catch (InvalidOperationException)
        {
            return new PrinterHealth(
                "degraded", _options.PrinterName, false, false,
                "Windows Print Spooler status could not be read.",
                SpoolerStatus: "unknown");
        }

        if (!NativeMethods.OpenPrinter(_options.PrinterName, out var handle, IntPtr.Zero))
            return new PrinterHealth("running", _options.PrinterName, false, false,
                "Configured printer was not found in Windows.");

        try
        {
            var status = ReadStatus(handle);
            var ready = (status & BlockingStatus) == 0;
            return new PrinterHealth(
                "running", _options.PrinterName, true, ready,
                ready
                    ? "Windows spooler reports no blocking printer status; physical delivery is not proven."
                    : DescribeStatus(status),
                SpoolerStatus: spoolerStatus,
                FailureCategory: ready ? null : FailureCategory(status));
        }
        catch (Win32Exception exception)
        {
            logger.LogWarning(
                "Could not read status for configured printer {PrinterName}; category={Category}.",
                _options.PrinterName, exception.GetType().Name);
            return new PrinterHealth("degraded", _options.PrinterName, true, false,
                "Printer exists, but Windows status could not be read.");
        }
        finally
        {
            NativeMethods.ClosePrinter(handle);
        }
    }

    public async Task<PrintOperationResult> PrintRawAsync(
        string documentName,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        var health = await GetHealthAsync(cancellationToken);
        if (!health.PrinterExists)
            return new(false, "printer_not_found", "Configured printer was not found in Windows.", _options.PrinterName);
        if (!health.Ready)
            return new(false, health.FailureCategory ?? "printer_offline",
                health.Detail ?? "Printer is not ready.", _options.PrinterName);

        try
        {
            return await Task.Run(
                    () => PrintRawCore(documentName, data.ToArray()),
                    CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(_options.SpoolerTimeoutSeconds), cancellationToken);
        }
        catch (System.TimeoutException)
        {
            logger.LogWarning("Windows RAW print call timed out for printer {PrinterName}.", _options.PrinterName);
            return new(false, "spooler_timeout",
                "Windows spooler did not respond before the configured timeout. Printing may be indeterminate; do not reuse this request ID.",
                _options.PrinterName);
        }
    }

    private PrintOperationResult PrintRawCore(string documentName, byte[] bytes)
    {
        if (!NativeMethods.OpenPrinter(_options.PrinterName, out var handle, IntPtr.Zero))
            return Failure("spooler_rejected", "Windows could not open the configured printer.");

        var documentStarted = false;
        var pageStarted = false;
        try
        {
            var info = new NativeMethods.DocInfo
            {
                DocumentName = documentName,
                DataType = "RAW"
            };
            if (NativeMethods.StartDocPrinter(handle, 1, ref info) == 0)
                return Failure("spooler_rejected", LastWindowsError("Windows could not start the print document."));
            documentStarted = true;

            if (!NativeMethods.StartPagePrinter(handle))
                return Failure("spooler_rejected", LastWindowsError("Windows could not start the print page."));
            pageStarted = true;

            if (!NativeMethods.WritePrinter(handle, bytes, bytes.Length, out var written) || written != bytes.Length)
                return Failure("partial_or_unknown_output", LastWindowsError("Windows did not accept the complete receipt."));

            return new(true, "printed", "Receipt was accepted by the Windows print spooler.",
                _options.PrinterName, written);
        }
        finally
        {
            if (pageStarted) NativeMethods.EndPagePrinter(handle);
            if (documentStarted) NativeMethods.EndDocPrinter(handle);
            NativeMethods.ClosePrinter(handle);
        }

        PrintOperationResult Failure(string code, string message)
        {
            logger.LogWarning("RAW print failed with code {Code} for printer {PrinterName}.", code, _options.PrinterName);
            return new(false, code, message, _options.PrinterName);
        }
    }

    private static uint ReadStatus(IntPtr handle)
    {
        NativeMethods.GetPrinter(handle, 2, IntPtr.Zero, 0, out var required);
        if (required == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        var buffer = Marshal.AllocHGlobal((int)required);
        try
        {
            if (!NativeMethods.GetPrinter(handle, 2, buffer, required, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return Marshal.PtrToStructure<NativeMethods.PrinterInfo2>(buffer).Status;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string DescribeStatus(uint status)
    {
        var states = new List<string>();
        if ((status & PrinterStatusOffline) != 0) states.Add("offline");
        if ((status & PrinterStatusPaperOut) != 0) states.Add("paper out");
        if ((status & PrinterStatusPaperJam) != 0) states.Add("paper jam");
        if ((status & PrinterStatusDoorOpen) != 0) states.Add("door open");
        if ((status & PrinterStatusPaused) != 0) states.Add("paused");
        if ((status & PrinterStatusError) != 0) states.Add("error");
        if ((status & PrinterStatusUserIntervention) != 0) states.Add("user intervention required");
        if ((status & PrinterStatusNotAvailable) != 0) states.Add("not available");
        return states.Count == 0 ? $"Windows printer status is 0x{status:X8}." : $"Printer is {string.Join(", ", states)}.";
    }

    private static string FailureCategory(uint status)
    {
        if ((status & PrinterStatusPaperOut) != 0) return "paper_out";
        if ((status & PrinterStatusDoorOpen) != 0) return "cover_open";
        if ((status & PrinterStatusPaperJam) != 0) return "paper_jam";
        if ((status & PrinterStatusOffline) != 0) return "printer_offline";
        return "hardware_unavailable";
    }

    private static string LastWindowsError(string prefix)
    {
        var error = Marshal.GetLastWin32Error();
        return error == 0 ? prefix : $"{prefix} Windows error {error}.";
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DocInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string DocumentName;
            [MarshalAs(UnmanagedType.LPWStr)] public string? OutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string DataType;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PrinterInfo2
        {
            public IntPtr ServerName, PrinterName, ShareName, PortName, DriverName, Comment, Location, DevMode;
            public IntPtr SeparatorFile, PrintProcessor, DataType, Parameters, SecurityDescriptor;
            public uint Attributes, Priority, DefaultPriority, StartTime, UntilTime, Status, Jobs, AveragePpm;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaults);

        [DllImport("winspool.drv", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ClosePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint StartDocPrinter(IntPtr printerHandle, uint level, ref DocInfo documentInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EndDocPrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool StartPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EndPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrinter(IntPtr printerHandle, byte[] bytes, int count, out int written);

        [DllImport("winspool.drv", EntryPoint = "GetPrinterW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPrinter(
            IntPtr printerHandle, uint level, IntPtr buffer, uint bufferSize, out uint required);
    }
}
