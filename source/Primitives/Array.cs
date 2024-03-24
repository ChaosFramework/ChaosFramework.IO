using System;
using System.IO;
using static ChaosFramework.IO.ChaosIO;

namespace ChaosFramework.IO.Primitives
{
    static class Array
    {
        /// <summary> Reads an array object from a stream. </summary>
        /// <param name="reader">
        ///     The reader used to read the array.
        ///     baseStream.Position must point to the beginning of the array object.
        /// </param>
        /// <param name="arrayType"> The type of array to be read. </param>
        /// <param name="lengthDecodingType"> The type used for decoding the array length (<see langword="int"/> by default). </param>
        public static System.Array Read(BinaryReader reader, System.Type arrayType, System.Type lengthDecodingType = null)
        {
            if (reader.ReadBoolean()) //isNull
                return null;

            System.Type elementType = arrayType.GetElementType();
            System.Type lengthType = lengthDecodingType ?? typeof(int);
            Reader lengthReader = GetReader(lengthType);
            if (lengthReader == null)
                throw new ArgumentException($"Invalid datatype for array size: \"{lengthType.FullName}\".", nameof(lengthDecodingType));

            int dimensions = arrayType.GetArrayRank();
            int[] arrayLength = new int[dimensions];
            for (int i = 0; i < dimensions; i++)
                arrayLength[i] = Convert.ToInt32(lengthReader(reader));

            System.Array array = System.Array.CreateInstance(elementType, arrayLength);
            for (int i = 0; i < dimensions; i++)
                if (arrayLength[i] == 0)
                    return array;

            ReadContentOnly(reader, elementType, array, arrayLength);
            return array;
        }

        /// <summary> Reads only an array's content (without any information about length or dimensions). </summary>
        /// <param name="reader">
        ///     The reader used to read the array content.
        ///     baseStream.Position must point to the beginning of the first element.
        /// </param>
        /// <param name="elementType"> The element type of the array to be read. </param>
        /// <param name="target"> The array object to write the read data to. </param>
        /// <param name="arrayLength"> The lengths of the array. </param>
        internal static void ReadContentOnly(BinaryReader reader, System.Type elementType, System.Array target, params int[] arrayLength)
        {
            int[] arrayCounters = new int[arrayLength.Length];
            while (true)
            {
                target.SetValue(reader.Read(elementType), arrayCounters);
                arrayCounters[0]++;
                int currentDimension = 0;
                while (arrayCounters[currentDimension] >= arrayLength[currentDimension])
                {
                    arrayCounters[currentDimension++] = 0;
                    if (currentDimension >= arrayLength.Length)
                        return;

                    arrayCounters[currentDimension]++;
                }
            }
        }

        /// <summary> Writes an array object to a stream. </summary>
        /// <param name="writer"> The writer used to write the array. </param>
        /// <param name="array"> The array to be written. </param>
        /// <param name="lengthEncodingType"> The type used for encoding the array length (<see langword="int"/> by default). </param>
        public static void Write(BinaryWriter writer, System.Array array, System.Type lengthEncodingType = null)
        {
            System.Type lengthType = lengthEncodingType ?? typeof(int);

            bool isNull = array == null;
            writer.Write(isNull);
            if (isNull)
                return;

            System.Type arrayType = array.GetType();
            int dimensions = arrayType.GetArrayRank();

            Writer lengthWriter = GetWriter(lengthType);
            if (lengthWriter == null)
                throw new ArgumentException($"Invalid datatype for array size: \"{lengthType.FullName}\".", nameof(lengthEncodingType));

            int[] arrayLength = new int[dimensions];
            for (int i = 0; i < dimensions; i++)
                lengthWriter(writer, arrayLength[i] = array.GetLength(i));

            WriteContentOnly(writer, array, arrayLength);
        }

        /// <summary> Writes only an array's content (without any information about length or dimensions). </summary>
        /// <param name="writer"> The writer used to write the array. </param>
        /// <param name="array"> The array whose content is to be written. </param>
        /// <param name="arrayLength"> The lengths of the array. </param>
        internal static void WriteContentOnly(BinaryWriter writer, System.Array array, params int[] arrayLength)
        {
            System.Type elementType = array.GetType().GetElementType();
            int[] arrayCounters = new int[arrayLength.Length];
            foreach (int len in arrayLength)
                if (len == 0)
                    return;

            while (true)
            {
                writer.WriteAs(elementType, array.GetValue(arrayCounters));
                arrayCounters[0]++;
                int currentDimension = 0;
                while (arrayCounters[currentDimension] >= arrayLength[currentDimension])
                {
                    arrayCounters[currentDimension++] = 0;
                    if (currentDimension >= arrayLength.Length)
                        return;

                    arrayCounters[currentDimension]++;
                }
            }
        }
    }
}
