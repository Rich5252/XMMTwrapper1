using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;


//Like the incoming events the data to be sent back to OCX requires special ahandling too
internal static class XMMSpecComMsgDispatch
{
    // VARTYPE (use ushort to avoid sign/convert headaches)
    private const ushort VT_I2 = 2;
    public const ushort VT_I4 = 0x0003;
    private const ushort VT_BYREF = 0x4000;

    // Replace if your actual DISPID differs
    // See file XMMT_OCX_IDL_INTERFACE_DEFS.txt for all DISPIDs
    public const int DISPID_SpecDraw = 14;
    public const int DISPID_WaterfallDraw = 6;

    // Invoke flags
    private const ushort DISPATCH_METHOD = 0x1;

    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")] // IID_IDispatch
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDispatch
    {
        [PreserveSig] int GetTypeInfoCount(out uint pctinfo);
        [PreserveSig] int GetTypeInfo(uint iTInfo, uint lcid, out IntPtr ppTInfo);

        [PreserveSig]
        int GetIDsOfNames(
            ref Guid riid,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
            string[] rgszNames,
            uint cNames,
            uint lcid,
            [MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

        [PreserveSig]
        int Invoke(
            int dispIdMember,
            ref Guid riid,
            uint lcid,
            ushort wFlags,
            ref DISPPARAMS pDispParams,
            IntPtr pVarResult,
            IntPtr pExcepInfo,
            out uint puArgErr);
    }

    // Correct VARIANT layout: 16 bytes: vt(2) + reserved(6) + data(8)
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct VARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(2)] public ushort wReserved1;
        [FieldOffset(4)] public ushort wReserved2;
        [FieldOffset(6)] public ushort wReserved3;

        [FieldOffset(8)] public short iVal;     // VT_I2
        [FieldOffset(8)] public IntPtr byref;   // VT_*|VT_BYREF (e.g. piVal)
    }

    /// <summary>
    /// Calls Spectrum.Draw(pFFT As Integer(ByRef), size As Integer, sampfreq As Integer) where VB6 Integer = Int16.
    /// Expects pFftShorts to be a pointer to the first short sample (short*), pinned for the duration of the call.
    /// </summary>
    public static void InvokeSpecDraw(object ocx, IntPtr pFftShorts, short size, short sampHz, int DISPID)
    {
        if (ocx is null) throw new ArgumentNullException(nameof(ocx));
        if (pFftShorts == IntPtr.Zero) throw new ArgumentNullException(nameof(pFftShorts));

        var disp = (IDispatch)ocx;

        // COM rgvarg is reversed: [0]=last arg, [1]=middle, [2]=first
        // VB signature: Draw(pFFT, size, sampfreq)
        // So rgvarg must be: sampfreq, size, pFFT
        VARIANT[] args = new VARIANT[3];

        args[0] = new VARIANT { vt = VT_I2, iVal = sampHz };                         // sampfreq
        args[1] = new VARIANT { vt = VT_I2, iVal = size };                         // size
        args[2] = new VARIANT { vt = (ushort)(VT_I2 | VT_BYREF), byref = pFftShorts }; // pFFT (piVal)

        int cbVar = Marshal.SizeOf<VARIANT>();
        IntPtr pArgs = Marshal.AllocHGlobal(cbVar * args.Length);

        var dp = new DISPPARAMS
        {
            cArgs = args.Length,
            cNamedArgs = 0,
            rgdispidNamedArgs = IntPtr.Zero,
            rgvarg = pArgs
        };

        try
        {
            for (int i = 0; i < args.Length; i++)
                Marshal.StructureToPtr(args[i], pArgs + (i * cbVar), fDeleteOld: false);

            Guid iidNull = Guid.Empty;
            uint argErr;

            int hr = disp.Invoke(
                DISPID,
                ref iidNull,
                lcid: 0,                 // LOCALE_USER_DEFAULT also fine; 0 usually OK
                wFlags: DISPATCH_METHOD,
                ref dp,
                pVarResult: IntPtr.Zero,
                pExcepInfo: IntPtr.Zero,
                out argErr);

            if (hr != 0)
            {
                // 0x80020005 + argErr=2 -> pFFT VARIANT mismatch
                throw new COMException($"SpecDraw IDispatch.Invoke failed. hr=0x{hr:X8}, argErr={argErr}", hr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pArgs);
        }
    }


    //This format is suitable to send both XY and NMMR data arrays (also probably NMMT)
    public static void InvokeSendIntArray(object ocx, IntPtr pArray, int dispid)
    {
        if (ocx is null) throw new ArgumentNullException(nameof(ocx));
        if (pArray == IntPtr.Zero) throw new ArgumentNullException(nameof(pArray));

        var disp = (IDispatch)ocx;

        // One argument: VT_I4 | VT_BYREF, pointing at LONG[1024]
        VARIANT[] args = new VARIANT[1];
        args[0] = new VARIANT
        {
            vt = (ushort)(VariantUtil.VT_I4 | VariantUtil.VT_BYREF),
            byref = pArray
        };

        int cbVar = Marshal.SizeOf<VARIANT>();
        IntPtr pArgs = Marshal.AllocHGlobal(cbVar * args.Length);

        var dp = new DISPPARAMS
        {
            cArgs = args.Length,
            cNamedArgs = 0,
            rgdispidNamedArgs = IntPtr.Zero,
            rgvarg = pArgs
        };

        try
        {
            // For IDispatch, args are stored in reverse order.
            // With 1 arg, that's still just args[0].
            Marshal.StructureToPtr(args[0], pArgs, fDeleteOld: false);

            Guid iidNull = Guid.Empty;
            uint argErr;

            const ushort DISPATCH_METHOD = 0x0001;

            int hr = disp.Invoke(
                dispid,
                ref iidNull,
                lcid: 0,
                wFlags: DISPATCH_METHOD,
                ref dp,
                pVarResult: IntPtr.Zero,
                pExcepInfo: IntPtr.Zero,
                out argErr);

            if (hr != 0)
                throw new COMException($"IDispatch.Invoke failed. hr=0x{hr:X8}, argErr={argErr}", hr);
        }
        finally
        {
            Marshal.FreeHGlobal(pArgs);
        }
    }


}
