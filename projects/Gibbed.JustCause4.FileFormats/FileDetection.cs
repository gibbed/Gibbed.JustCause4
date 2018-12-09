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

namespace Gibbed.JustCause4.FileFormats
{
    public static class FileDetection
    {
        private struct FileTypeInfo
        {
            public readonly string Name;
            public readonly string Extension;
            public readonly int[] ValidOffsets;

            public FileTypeInfo(string name, string extension, params int[] validOffsets)
            {
                this.Name = name;
                this.Extension = extension;
                this.ValidOffsets = validOffsets;
            }

            public bool IsValidOffset(int offset)
            {
                return this.ValidOffsets == null || Array.IndexOf(this.ValidOffsets, offset) >= 0;
            }
        }

        private static readonly Dictionary<uint, FileTypeInfo> _Simple4Lookup;
        private static readonly Dictionary<ulong, FileTypeInfo> _Simple8Lookup;

        static FileDetection()
        {
            _Simple4Lookup = new Dictionary<uint, FileTypeInfo>()
            {
                { 0x20534444, new FileTypeInfo("D3D texture", "dds", 0) },
                { 0x20564546, new FileTypeInfo("FMOD bank", "fmod_bankc", 8) },
                { 0x30474154, new FileTypeInfo("Havok tagfile", "hkt", 4) },
                { 0x35425346, new FileTypeInfo("audio", "fsb5") },
                { 0x41444620, new FileTypeInfo("arbitrary data format", "adf") },
                { 0x43505452, new FileTypeInfo("runtime property container?", "rtpc", 0) },
                { 0x43524153, new FileTypeInfo("small archive", "sarc", 4) },
                { 0x58545641, new FileTypeInfo("Avalanche texture", "ddsc", 0) },
                { 0x694B4942, new FileTypeInfo("Bink movie", "bikc", 0) },
            };

            _Simple8Lookup = new Dictionary<ulong, FileTypeInfo>()
            {
                { 0x444E425200000005UL, new FileTypeInfo("RBN", "rbn") },
                { 0x4453425200000005UL, new FileTypeInfo("RBS", "rbs") },
            };
        }

        public static string Detect(byte[] guess, int read)
        {
            if (read == 0)
            {
                return "null";
            }

            for (int offset = 0; offset + 4 <= read && offset < 20; offset += 4)
            {
                var magic = BitConverter.ToUInt32(guess, offset);
                FileTypeInfo fileTypeInfo;
                if (_Simple4Lookup.TryGetValue(magic, out fileTypeInfo) == true &&
                    fileTypeInfo.IsValidOffset(offset) == true)
                {
                    return fileTypeInfo.Extension;
                }
            }

            for (int offset = 0; offset + 8 <= read && offset < 24; offset += 8)
            {
                var magic = BitConverter.ToUInt64(guess, 0);
                FileTypeInfo fileTypeInfo;
                if (_Simple8Lookup.TryGetValue(magic, out fileTypeInfo) == true &&
                    fileTypeInfo.IsValidOffset(offset) == true)
                {
                    return fileTypeInfo.Extension;
                }
            }

            if (read >= 3)
            {
                if ((guess[0] == 0x47 || guess[0] == 0x43) && // 'G'/'C'
                    guess[1] == 0x46 && // 'F'
                    guess[2] == 0x58) // 'X'
                {
                    return "gfx";
                }

                if (guess[0] == 1 &&
                    guess[1] == 4 &&
                    guess[2] == 0)
                {
                    return "bin";
                }
            }

            return "unknown";
        }
    }
}
