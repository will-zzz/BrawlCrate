using BrawlLib.Internal;
using System.Runtime.InteropServices;

namespace BrawlLib.SSBB.Types
{
    /// <summary>
    /// Wii Sports Golf Pack MaP file header.
    /// Magic "PMPF" at 0x00, object count at 0x10, object data offset at 0x40.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PMPHeader
    {
        public const int Size = 0x44;
        public const uint Tag = 0x504D5046; // 'PMPF' big-endian

        public buint _tag;
        private fixed byte _pad1[0x0C];
        public bushort _objectCount;
        private fixed byte _pad2[0x2E]; // 0x12 to 0x40
        public bint _objectDataOffset;   // at 0x40

        internal VoidPtr Address
        {
            get
            {
                fixed (void* ptr = &this)
                {
                    return ptr;
                }
            }
        }

        public bool IsValid => _tag == Tag && (ushort)_objectCount > 0;
    }

    /// <summary>
    /// One object entry in a PMP file (~0x58 bytes).
    /// Object ID identifies Cup, Tee, etc.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PMPObjectEntry
    {
        public const int Size = 0x58;

        public bushort _objectGroupId;
        public bushort _objectId;
        private fixed byte _pad1[4];
        public BVec3 _position;
        public BVec3 _scale;
        private fixed byte _matrix[0x24]; // 3x3 floats
        private fixed byte _objectParams[0x10]; // object-specific, variable in practice

        internal VoidPtr Address
        {
            get
            {
                fixed (void* ptr = &this)
                {
                    return ptr;
                }
            }
        }
    }
}
