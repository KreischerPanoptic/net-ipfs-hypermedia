﻿/*
 * The package was named SHA3 it is really Keccak
 * https://medium.com/@ConsenSys/are-you-really-using-sha-3-or-old-code-c5df31ad2b0
 * See 
 * 
 * The SHA3 package doesn't create .Net Standard package.
 * This is a copy of https://bitbucket.org/jdluzen/sha3/raw/d1fd55dc225d18a7fb61515b62d3c8f164d2e788/SHA3/SHA3.cs
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Ipfs.Hypermedia.Cryptography
{
    internal abstract class Keccak :
#if PORTABLE
    IHashAlgorithm
#else
    System.Security.Cryptography.HashAlgorithm
#endif
    {
        #region Implementation
        public const int KeccakB = 1600;
        public const int KeccakNumberOfRounds = 24;
        public const int KeccakLaneSizeInBits = 8 * 8;

        public readonly ulong[] RoundConstants;

        protected ulong[] state;
        protected byte[] buffer;
        protected int buffLength;
#if PORTABLE || NETSTANDARD14
        protected byte[] HashValue;
        protected int HashSizeValue;
#endif
        protected int keccakR;

        public int KeccakR
        {
            get
            {
                return keccakR;
            }
            protected set
            {
                keccakR = value;
            }
        }

        public int SizeInBytes
        {
            get
            {
                return KeccakR / 8;
            }
        }

        public int HashByteLength
        {
            get
            {
                return HashSizeValue / 8;
            }
        }

        public
#if (!PORTABLE && !NETSTANDARD14)
        override
#endif
        bool CanReuseTransform
        {
            get
            {
                return true;
            }
        }

        protected Keccak(int hashBitLength)
        {
            Initialize();
            HashSizeValue = hashBitLength;
            switch (hashBitLength)
            {
                case 224:
                    KeccakR = 1152;
                    break;
                case 256:
                    KeccakR = 1088;
                    break;
                case 384:
                    KeccakR = 832;
                    break;
                case 512:
                    KeccakR = 576;
                    break;
                default:
                    throw new ArgumentException("hashBitLength must be 224, 256, 384, or 512", nameof(hashBitLength));
            }
            RoundConstants = new []
            {
                0x0000000000000001UL,
                0x0000000000008082UL,
                0x800000000000808aUL,
                0x8000000080008000UL,
                0x000000000000808bUL,
                0x0000000080000001UL,
                0x8000000080008081UL,
                0x8000000000008009UL,
                0x000000000000008aUL,
                0x0000000000000088UL,
                0x0000000080008009UL,
                0x000000008000000aUL,
                0x000000008000808bUL,
                0x800000000000008bUL,
                0x8000000000008089UL,
                0x8000000000008003UL,
                0x8000000000008002UL,
                0x8000000000000080UL,
                0x000000000000800aUL,
                0x800000008000000aUL,
                0x8000000080008081UL,
                0x8000000000008080UL,
                0x0000000080000001UL,
                0x8000000080008008UL
            };
        }

        protected static ulong ROL(ulong a, int offset)
        {
            return (((a) << ((offset) % KeccakLaneSizeInBits)) ^ ((a) >> (KeccakLaneSizeInBits - ((offset) % KeccakLaneSizeInBits))));
        }

        protected void AddToBuffer(byte[] array, ref int offset, ref int count)
        {
            int amount = Math.Min(count, buffer.Length - buffLength);
            Buffer.BlockCopy(array, offset, buffer, buffLength, amount);
            offset += amount;
            buffLength += amount;
            count -= amount;
        }

        public
#if !PORTABLE && !NETSTANDARD14
        override
#endif
        byte[] Hash
        {
            get
            {
                return HashValue;
            }
        }

        public
#if !PORTABLE
        override
#endif
        int HashSize
        {
            get
            {
                return HashSizeValue;
            }
        }

        #endregion

        public
#if !PORTABLE
        override
#endif
        void Initialize()
        {
            buffLength = 0;
            state = new ulong[5 * 5];//1600 bits
            HashValue = null;
        }

        protected
#if !PORTABLE
        override
#endif
        void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (ibStart < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ibStart));
            }
            if (cbSize > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(cbSize));
            }
            if (ibStart + cbSize > array.Length)
            {
                throw new ArgumentOutOfRangeException("ibStart or cbSize");
            }
        }

#if PORTABLE
        public abstract int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);
        public abstract byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
#endif
    }
}