using ChaosUtil.Reflection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ChaosFramework.IO
{
    public static class ClassSerializer
    {
        const BindingFlags DEFAULT_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        public class SerializationAttribute : Attribute
        {
            public enum Usage
            {
                Default,
                Ignore,
                Safe
            }

            public readonly Usage usage;

            public SerializationAttribute(Usage usage = Usage.Default)
            { this.usage = usage; }
        }

        public static T Read<T>(T target, BinaryReader reader, BindingFlags fieldBindingFlags = DEFAULT_BINDING_FLAGS)
        {
            FieldInfo[] allFields = target.GetType().GetFields(fieldBindingFlags);
            foreach (FieldInfo field in allFields)
            {
                SerializationAttribute attr = field.GetAttributes<SerializationAttribute>().FirstOrDefault();
                if (attr != null && attr.usage == SerializationAttribute.Usage.Ignore)
                    continue;

                if (attr != null && attr.usage == SerializationAttribute.Usage.Safe)
                    field.SetValue(target, reader.ReadSafe(field.FieldType));
                else
                    field.SetValue(target, reader.Read(field.FieldType));
            }

            return target;
        }

        public static void Write(
            BinaryWriter writer,
            object toBeSaved,
            BindingFlags fieldBindingFlags = DEFAULT_BINDING_FLAGS
            )
        {
            foreach (FieldInfo field in toBeSaved.GetType().GetFields(fieldBindingFlags))
            {
                SerializationAttribute attr = field.GetAttributes<SerializationAttribute>().FirstOrDefault();
                if (attr != null && attr.usage == SerializationAttribute.Usage.Ignore)
                    continue;

                if (attr != null && attr.usage == SerializationAttribute.Usage.Safe)
                    writer.WriteSafe(field.GetValue(toBeSaved));
                else
                    writer.WriteAs(field.FieldType, field.GetValue(toBeSaved));
            }
        }
    }
}
