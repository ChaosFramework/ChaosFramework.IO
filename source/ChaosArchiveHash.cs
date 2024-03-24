using ChaosFramework.Core;
using System;
using System.IO;
using System.Linq;
using SysCol = System.Collections.Generic;
using VerySafe = System.Security.Cryptography;

namespace ChaosFramework.IO
{
    public partial class ChaosArchive
    {
        public class ArchiveHash : Disposable
        {
            public const int NUM_HASH_BYTES = 16;

            static readonly System.Text.Encoding stringEncoding = System.Text.Encoding.UTF8;

            public static ArchiveHash GetHash(SysCol.IEnumerable<string> keys, Func<string, byte[]> getBytes)
            {
                ArchiveHash hash = new ArchiveHash();
                int numFiles = 0;
                SysCol.IEnumerable<string> normalized = keys
                    .Select(ChaosUtil.Platform.Paths.Normalization.NormalizeRelative)
                    .OrderBy(Collections.Linq.SelectIdentity);

                foreach (string f in normalized)
                {
                    numFiles++;
                    hash.Transform(f);
                }

                int i = 0;
                foreach (string f in normalized)
                {
                    byte[] bytes = getBytes(f);
                    if (++i < numFiles)
                        hash.Transform(bytes);
                    else
                        hash.TransformFinal(bytes);
                }

                return hash;
            }

            readonly VerySafe.HashAlgorithm hash = VerySafe.MD5.Create();

            public SysCol.IEnumerable<byte> bytes => hash.Hash;

            public void Transform(string str) => Transform(stringEncoding.GetBytes(str));
            public void Transform(byte[] data) => hash.TransformBlock(data, 0, data.Length, data, 0);

            public void TransformFinal(byte[] data) => hash.TransformFinalBlock(data, 0, data.Length);

            public void Write(BinaryWriter wr) => wr.Write(hash.Hash);

            public override string ToString() => BitConverter.ToString(hash.Hash);

            protected override void DoDispose()
            {
                base.DoDispose();
                hash.Dispose();
            }
        }
    }
}
