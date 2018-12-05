/* Copyright (c) 2018 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause4.FileFormats
{
    public class ArchiveTableFile
    {
        public const uint Signature = 0x00424154; // 'TAB\0'

        private Endian _Endian;
        private uint _Alignment;
        private readonly List<KeyValuePair<uint, uint>> _Unknowns;
        private readonly List<EntryInfo> _Entries;

        public ArchiveTableFile()
        {
            this._Unknowns = new List<KeyValuePair<uint, uint>>();
            this._Entries = new List<EntryInfo>();
        }

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public uint Alignment
        {
            get { return this._Alignment; }
            set { this._Alignment = value; }
        }

        public List<KeyValuePair<uint, uint>> Unknowns
        {
            get { return this._Unknowns; }
        }

        public List<EntryInfo> Entries
        {
            get { return this._Entries; }
        }

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var majorVersion = input.ReadValueU16(endian);
            var minorVersion = input.ReadValueU16(endian);
            if (majorVersion != 2 || minorVersion != 1)
            {
                throw new FormatException();
            }

            var alignment = input.ReadValueU32(endian);
            var unknown0C = input.ReadValueU32(endian);
            var unknown10 = input.ReadValueU32(endian);
            var unknown14 = input.ReadValueU32(endian);

            if (alignment != 0x1000 || unknown0C != 0)
            {
                throw new FormatException();
            }

            var unknownCount = input.ReadValueU32(endian);
            var unknowns = new KeyValuePair<uint, uint>[unknownCount];
            for (uint i = 0; i < unknownCount; i++)
            {
                var unknown00 = input.ReadValueU32(endian);
                var unknown04 = input.ReadValueU32(endian);
                unknowns[i] = new KeyValuePair<uint, uint>(unknown00, unknown04);
            }

            var entries = new List<EntryInfo>();
            while (input.Position + 20 <= input.Length)
            {
                RawEntryInfo entryInfo;
                entryInfo.NameHash = input.ReadValueU32(endian);
                entryInfo.Offset = input.ReadValueU32(endian);
                entryInfo.CompressedSize = input.ReadValueU32(endian);
                entryInfo.UncompressedSize = input.ReadValueU32(endian);
                entryInfo.Unknown = input.ReadValueU32(endian);
                entries.Add(new EntryInfo(entryInfo));
            }

            this._Endian = endian;
            this._Alignment = alignment;
            this._Unknowns.Clear();
            this._Unknowns.AddRange(unknowns);
            this._Entries.Clear();
            this._Entries.AddRange(entries);
        }

        internal struct RawEntryInfo
        {
            public uint NameHash;
            public uint Offset;
            public uint CompressedSize;
            public uint UncompressedSize;
            public uint Unknown;
        }

        public struct EntryInfo
        {
            public readonly uint NameHash;
            public readonly uint Offset;
            public readonly uint CompressedSize;
            public readonly uint UncompressedSize;
            public readonly uint Unknown;

            internal EntryInfo(RawEntryInfo raw)
            {
                this.NameHash = raw.NameHash;
                this.Offset = raw.Offset;
                this.CompressedSize = raw.CompressedSize;
                this.UncompressedSize = raw.UncompressedSize;
                this.Unknown = raw.Unknown;
            }

            public EntryInfo(uint nameHash, uint offset, uint compressedSize, uint uncompressedSize, uint unknown)
            {
                this.NameHash = nameHash;
                this.Offset = offset;
                this.CompressedSize = compressedSize;
                this.UncompressedSize = uncompressedSize;
                this.Unknown = unknown;
            }
        }
    }
}
