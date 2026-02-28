using BrawlLib.Internal;
using BrawlLib.Internal.IO;
using BrawlLib.SSBB.Types;
using System;
using System.ComponentModel;
using System.IO;

namespace BrawlLib.SSBB.ResourceNodes
{
    /// <summary>
    /// Wii Sports Golf Pack MaP (PMP) file. Contains object placements (Tee, Cup, etc.) for a hole.
    /// </summary>
    public unsafe class PMPNode : ARCEntryNode
    {
        internal PMPHeader* Header => (PMPHeader*)WorkingUncompressed.Address;
        public override ResourceType ResourceFileType => ResourceType.PMP;
        public override Type[] AllowedChildTypes => new Type[] { typeof(PMPObjectEntryNode) };

        public override bool OnInitialize()
        {
            if (!Header->IsValid)
                return false;
            int count = (ushort)Header->_objectCount;
            int dataOffset = Header->_objectDataOffset;
            if (dataOffset <= 0)
                dataOffset = PMPHeader.Size;
            return WorkingUncompressed.Length >= dataOffset + count * PMPObjectEntry.Size;
        }

        public override void OnPopulate()
        {
            byte* baseAddr = (byte*)WorkingUncompressed.Address;
            int dataOffset = Header->_objectDataOffset;
            if (dataOffset <= 0)
                dataOffset = PMPHeader.Size;
            ushort count = (ushort)Header->_objectCount;
            for (int i = 0; i < count; i++)
            {
                VoidPtr entryAddr = baseAddr + dataOffset + i * PMPObjectEntry.Size;
                DataSource source = new DataSource(entryAddr, PMPObjectEntry.Size);
                var entry = new PMPObjectEntryNode();
                entry.Initialize(this, source);
            }
        }

        public override string GetName()
        {
            return base.GetName("Golf Map (PMP)");
        }

        public override int OnCalculateSize(bool force)
        {
            int dataOffset = 0x44;
            return dataOffset + Children.Count * PMPObjectEntry.Size;
        }

        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            PMPHeader* header = (PMPHeader*)address;
            header->_tag = PMPHeader.Tag;
            header->_objectCount = (ushort)Children.Count;
            header->_objectDataOffset = 0x44;

            VoidPtr entryAddr = address + 0x44;
            foreach (ResourceNode child in Children)
            {
                if (child is PMPObjectEntryNode entryNode)
                {
                    entryNode.Rebuild(entryAddr, PMPObjectEntry.Size, force);
                    entryAddr += PMPObjectEntry.Size;
                }
            }
        }

        internal static ResourceNode TryParse(DataSource source, ResourceNode parent)
        {
            if (source.Length < 0x44)
                return null;
            PMPHeader* h = (PMPHeader*)source.Address;
            if (h->_tag != PMPHeader.Tag)
                return null;
            int count = (ushort)h->_objectCount;
            if (count <= 0)
                return null;
            int dataOffset = h->_objectDataOffset;
            if (dataOffset <= 0)
                dataOffset = PMPHeader.Size;
            if (source.Length < dataOffset + count * PMPObjectEntry.Size)
                return null;
            return new PMPNode();
        }
    }

    /// <summary>
    /// One object in a PMP (e.g. Tee or Cup). Position at 0x08 is used for start (tee) or hole (cup) location.
    /// </summary>
    public unsafe class PMPObjectEntryNode : ResourceNode
    {
        public override ResourceType ResourceFileType => ResourceType.PMPObjectEntry;

        private ushort _objectGroupId;
        private ushort _objectId;
        private Vector3 _position;
        private Vector3 _scale;
        private byte[] _rest = new byte[0x34]; // 0x24 matrix + 0x10 params

        [Category("PMP Object")]
        public ushort ObjectGroupId
        {
            get => _objectGroupId;
            set { _objectGroupId = value; SignalPropertyChange(); }
        }

        [Category("PMP Object")]
        [Description("Object type: identifies Tee (ID 0), Cup (ID 1), etc. in Wii Sports Golf.")]
        public ushort ObjectId
        {
            get => _objectId;
            set { _objectId = value; SignalPropertyChange(); }
        }

        [Category("PMP Object")]
        [TypeConverter(typeof(Vector3StringConverter))]
        [Description("Position in world space. Change this to move the tee (start) or hole (Cup).")]
        public Vector3 Position
        {
            get => _position;
            set { _position = value; SignalPropertyChange(); }
        }

        [Category("PMP Object")]
        [TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 Scale
        {
            get => _scale;
            set { _scale = value; SignalPropertyChange(); }
        }

        public override bool OnInitialize()
        {
            PMPObjectEntry* e = (PMPObjectEntry*)WorkingUncompressed.Address;
            _objectGroupId = (ushort)e->_objectGroupId;
            _objectId = (ushort)e->_objectId;
            _position = (Vector3)e->_position;
            _scale = (Vector3)e->_scale;
            int restLen = PMPObjectEntry.Size - 0x20; // after scale: matrix 0x24 + params 0x10
            _rest = new byte[restLen];
            byte* src = (byte*)WorkingUncompressed.Address + 0x20;
            for (int i = 0; i < restLen; i++)
                _rest[i] = src[i];
            _name = GetDisplayName();
            return false;
        }

        private string GetDisplayName()
        {
            string kind = "Object";
            if (ObjectId == 0) kind = "Tee";
            else if (ObjectId == 1) kind = "Cup";
            return $"{kind} [{Index}] (Group {_objectGroupId}, ID {_objectId})";
        }

        public override int OnCalculateSize(bool force)
        {
            return PMPObjectEntry.Size;
        }

        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            PMPObjectEntry* e = (PMPObjectEntry*)address;
            e->_objectGroupId = (ushort)_objectGroupId;
            e->_objectId = (ushort)_objectId;
            e->_position = (BVec3)_position;
            e->_scale = (BVec3)_scale;
            byte* dst = (byte*)address + 0x20;
            for (int i = 0; i < _rest.Length; i++)
                dst[i] = _rest[i];
        }
    }
}
