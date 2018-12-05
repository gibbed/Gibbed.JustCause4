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
using System.Runtime.InteropServices;

namespace Gibbed.JustCause4.Unpack
{
    internal class Oodle
    {
        public static int Decompress(
            byte[] inputBytes,
            int inputOffset,
            int inputCount,
            byte[] outputBytes,
            int outputOffset,
            int outputCount)
        {
            if (inputBytes == null)
            {
                throw new ArgumentNullException("inputBytes");
            }

            if (inputOffset < 0 || inputOffset >= inputBytes.Length)
            {
                throw new ArgumentOutOfRangeException("inputOffset");
            }

            if (inputCount <= 0 || inputOffset + inputCount > inputBytes.Length)
            {
                throw new ArgumentOutOfRangeException("inputCount");
            }

            if (outputBytes == null)
            {
                throw new ArgumentNullException("outputBytes");
            }

            if (outputOffset < 0 || outputOffset >= outputBytes.Length)
            {
                throw new ArgumentOutOfRangeException("outputOffset");
            }

            if (outputCount <= 0 || outputOffset + outputCount > outputBytes.Length)
            {
                throw new ArgumentOutOfRangeException("outputCount");
            }

            int result;
            var inputHandle = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);
            var inputAddress = inputHandle.AddrOfPinnedObject() + inputOffset;
            var outputHandle = GCHandle.Alloc(outputBytes, GCHandleType.Pinned);
            var outputAddress = outputHandle.AddrOfPinnedObject() + outputOffset;
            result = (int)DecompressNative(inputAddress, inputCount, outputAddress, outputCount);
            inputHandle.Free();
            outputHandle.Free();
            return result;
        }

        internal const string DllName = "oo2core_7_win64";
        internal const CallingConvention StdCall = CallingConvention.StdCall;

        [DllImport(DllName, EntryPoint = "OodleLZ_Decompress", CallingConvention = StdCall)]
        private static extern long DecompressNative(
            IntPtr inputBuffer,
            long inputSize,
            IntPtr outputBuffer,
            long outputSize,
            uint flags,
            int unk6,
            int unk7,
            IntPtr unk8,
            long unk9,
            IntPtr unk10,
            long unk11,
            long unk12,
            long unk13,
            int unk14);

        private static long DecompressNative(
            IntPtr inputBuffer,
            long inputSize,
            IntPtr outputBuffer,
            long outputSize)
        {
            return DecompressNative(
                inputBuffer,
                inputSize,
                outputBuffer,
                outputSize,
                1,
                0,
                0,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                0,
                0,
                0,
                3);
        }
    }
}
