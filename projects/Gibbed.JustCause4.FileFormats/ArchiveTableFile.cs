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
        private uint _MaxCompressedBlockSize;
        private uint _UncompressedBlockSize;
        private readonly List<CompressedBlockInfo> _CompressedBlocks;
        private readonly List<EntryInfo> _Entries;

        public ArchiveTableFile()
        {
            this._CompressedBlocks = new List<CompressedBlockInfo>();
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

        public uint MaxCompressedBlockSize
        {
            get { return this._MaxCompressedBlockSize; }
            set { this._MaxCompressedBlockSize = value; }
        }

        public uint UncompressedBlockSize
        {
            get { return this._UncompressedBlockSize; }
            set { this._UncompressedBlockSize = value; }
        }

        public List<CompressedBlockInfo> CompressedBlocks
        {
            get { return this._CompressedBlocks; }
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
            var maxCompressedBlockSize = input.ReadValueU32(endian);
            var uncompressedBlockSize = input.ReadValueU32(endian);

            if (alignment != 0x1000 || unknown0C != 0)
            {
                throw new FormatException();
            }

            var compressedBlockCount = input.ReadValueU32(endian);
            var compressedBlocks = new CompressedBlockInfo[compressedBlockCount];
            for (uint i = 0; i < compressedBlockCount; i++)
            {
                RawCompressedBlockInfo raw;
                raw.CompressedSize = input.ReadValueU32(endian);
                raw.UncompressedSize = input.ReadValueU32(endian);
                compressedBlocks[i] = new CompressedBlockInfo(raw);
            }

            var entries = new List<EntryInfo>();
            while (input.Position + 20 <= input.Length)
            {
                RawEntryInfo raw;
                raw.NameHash = input.ReadValueU32(endian);
                raw.Offset = input.ReadValueU32(endian);
                raw.CompressedSize = input.ReadValueU32(endian);
                raw.UncompressedSize = input.ReadValueU32(endian);
                raw.CompressedBlockIndex = input.ReadValueU8();
                raw.Unknown11 = input.ReadValueU8();
                raw.CompressionType = (CompressionType)input.ReadValueU8();
                raw.Unknown13 = input.ReadValueU8();
                entries.Add(new EntryInfo(raw));
            }

            this._Endian = endian;
            this._Alignment = alignment;
            this._MaxCompressedBlockSize = maxCompressedBlockSize;
            this._UncompressedBlockSize = uncompressedBlockSize;
            this._CompressedBlocks.Clear();
            this._CompressedBlocks.AddRange(compressedBlocks);
            this._Entries.Clear();
            this._Entries.AddRange(entries);
        }

        internal struct RawCompressedBlockInfo
        {
            public uint CompressedSize;
            public uint UncompressedSize;
        }

        public struct CompressedBlockInfo
        {
            public readonly uint CompressedSize;
            public readonly uint UncompressedSize;

            internal CompressedBlockInfo(RawCompressedBlockInfo raw)
            {
                this.CompressedSize = raw.CompressedSize;
                this.UncompressedSize = raw.UncompressedSize;
            }

            public CompressedBlockInfo(uint compressedSize, uint uncompressedSize)
            {
                this.CompressedSize = compressedSize;
                this.UncompressedSize = uncompressedSize;
            }

            public bool IsValid
            {
                get
                {
                    return this.CompressedSize != uint.MaxValue &&
                           this.UncompressedSize != uint.MaxValue;
                }
            }

            public override string ToString()
            {
                return string.Format("{0:X} -> {1:X}", this.CompressedSize, this.UncompressedSize);
            }
        }

        internal struct RawEntryInfo
        {
            public uint NameHash;
            public uint Offset;
            public uint CompressedSize;
            public uint UncompressedSize;
            public byte CompressedBlockIndex;
            public byte Unknown11;
            public CompressionType CompressionType;
            public byte Unknown13;
        }

        public struct EntryInfo
        {
            public readonly uint NameHash;
            public readonly uint Offset;
            public readonly uint CompressedSize;
            public readonly uint UncompressedSize;
            public readonly byte CompressedBlockIndex;
            public readonly byte Unknown11;
            public readonly CompressionType CompressionType;
            public readonly byte Unknown13;

            internal EntryInfo(RawEntryInfo raw)
            {
                this.NameHash = raw.NameHash;
                this.Offset = raw.Offset;
                this.CompressedSize = raw.CompressedSize;
                this.UncompressedSize = raw.UncompressedSize;
                this.CompressedBlockIndex = raw.CompressedBlockIndex;
                this.Unknown11 = raw.Unknown11;
                this.CompressionType = raw.CompressionType;
                this.Unknown13 = raw.Unknown13;
            }

            public EntryInfo(
                uint nameHash,
                uint offset,
                uint compressedSize,
                uint uncompressedSize,
                byte unknown10,
                byte unknown11,
                CompressionType compressionType,
                byte unknown13)
            {
                this.NameHash = nameHash;
                this.Offset = offset;
                this.CompressedSize = compressedSize;
                this.UncompressedSize = uncompressedSize;
                this.CompressedBlockIndex = unknown10;
                this.Unknown11 = unknown11;
                this.CompressionType = compressionType;
                this.Unknown13 = unknown13;
            }

            public override string ToString()
            {
                return string.Format(
                    "{0:X8} @{1:X} {2:X}, {3:X} [{4:X} {5:X} {6} {7:X}]",
                    this.NameHash,
                    this.Offset,
                    this.CompressedSize,
                    this.UncompressedSize,
                    this.CompressedBlockIndex,
                    this.Unknown11,
                    this.CompressionType,
                    this.Unknown13);
            }
        }
    }
}
