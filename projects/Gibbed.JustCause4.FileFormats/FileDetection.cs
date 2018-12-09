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

            public FileTypeInfo(string name, string extension)
            {
                this.Name = name;
                this.Extension = extension;
            }
        }

        private static readonly Dictionary<uint, FileTypeInfo> _Simple4Lookup =
            new Dictionary<uint, FileTypeInfo>()
            {
                { 0x20534444, new FileTypeInfo("texture", "dds") },
                { 0x30474154, new FileTypeInfo("?", "tag0") },
                { 0x35425346, new FileTypeInfo("audio", "fsb5") },
                { 0x41444620, new FileTypeInfo("arbitrary data format", "adf") },
                { 0x43505452, new FileTypeInfo("runtime property container?", "rtpc") },
                { 0x43524153, new FileTypeInfo("small archive", "sarc") },
                { 0x57E0E057, new FileTypeInfo("animation", "ban") },
            };

        private static readonly Dictionary<ulong, FileTypeInfo> _Simple8Lookup =
            new Dictionary<ulong, FileTypeInfo>()
            {
                { 0x000000300000000EUL, new FileTypeInfo("ai", "btc") },
                { 0x444E425200000005UL, new FileTypeInfo("RBN", "rbn") },
                { 0x4453425200000005UL, new FileTypeInfo("RBS", "rbs") },
            };

        public static string Detect(byte[] guess, int read)
        {
            if (read == 0)
            {
                return "null";
            }

            if (read >= 4)
            {
                var magic = BitConverter.ToUInt32(guess, 0);
                if (_Simple4Lookup.ContainsKey(magic) == true)
                {
                    return _Simple4Lookup[magic].Extension;
                }
            }

            if (read >= 8)
            {
                var magic = BitConverter.ToUInt32(guess, 4);
                if (_Simple4Lookup.ContainsKey(magic) == true)
                {
                    return _Simple4Lookup[magic].Extension;
                }
            }

            if (read >= 8)
            {
                var magic = BitConverter.ToUInt64(guess, 0);
                if (_Simple8Lookup.ContainsKey(magic) == true)
                {
                    return _Simple8Lookup[magic].Extension;
                }
            }

            if (read >= 3)
            {
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
