using System.Runtime.InteropServices;

namespace NmmrContract
{
    /// <summary>
    /// NMMR structure (MMTTY → APP).
    /// Layout must match the native definition exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Nmmr
    {
        public uint MarkFreq;     // DWORD m_markfreq
        public uint SpaceFreq;    // DWORD m_spacefreq
        public uint SigLevel;     // DWORD m_siglevel
        public uint SqLevel;      // DWORD m_sqlevel
        public uint CodeSwitch;   // DWORD m_codeswitch
        public uint CodeView;     // DWORD m_codeview
        public uint Notch1;       // DWORD m_notch1
        public uint Notch2;       // DWORD m_notch2
        public uint Baud;         // DWORD m_baud
        public uint Fig;          // DWORD m_fig
        public uint RadioFreq;    // DWORD m_radiofreq

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 53)]
        public uint[] Reserved;   // DWORD m_Reserved[53]
    }
}
