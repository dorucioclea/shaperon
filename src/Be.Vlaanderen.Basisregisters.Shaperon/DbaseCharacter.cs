namespace Be.Vlaanderen.Basisregisters.Shaperon
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    public class DbaseCharacterOptions
    {
        public const string DefaultDateTimeFormat = "yyyyMMdd\\THHmmss";
        public const string DefaultDateTimeOffsetFormat = "yyyy-MM-dd\\THH:mm:ss%K";

        public static readonly DbaseCharacterOptions Default = new DbaseCharacterOptions(
            DefaultDateTimeFormat,
            DefaultDateTimeOffsetFormat
            );

        public DbaseCharacterOptions(string dateTimeFormat, string dateTimeOffsetFormat)
        {
            DateTimeFormat = dateTimeFormat ?? throw new ArgumentNullException(nameof(dateTimeFormat));
            DateTimeOffsetFormat = dateTimeOffsetFormat ?? throw new ArgumentNullException(nameof(dateTimeOffsetFormat));
        }

        public DbaseCharacterOptions WithDateTimeFormat(string format)
        {
            return new DbaseCharacterOptions(format, DateTimeOffsetFormat);
        }

        public string DateTimeFormat { get; }

        public DbaseCharacterOptions WithDateTimeOffsetFormat(string format)
        {
            return new DbaseCharacterOptions(DateTimeFormat, format);
        }

        public string DateTimeOffsetFormat { get; }
    }

    public class DbaseCharacter : DbaseFieldValue
    {
        private string _value;

        public DbaseCharacter(DbaseField field, string value = null, DbaseCharacterOptions options = null) : base(field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (field.FieldType != DbaseFieldType.Character)
                throw new ArgumentException(
                    $"The field {field.Name} 's type must be character to use it as a character field.", nameof(field));

            Value = value;
            Options = options ?? DbaseCharacterOptions.Default;
        }

        public bool AcceptsValue(string value)
        {
            if (value != null)
                return value.Length <= Field.Length.ToInt32();

            return true;
        }


        public bool AcceptsValue(DateTime? value)
        {
            if (value.HasValue)
            {
                var formatted = value.Value.ToString(Options.DateTimeFormat);
                if (formatted.Length > Field.Length.ToInt32())
                    return false;
            }

            return true;
        }

        public bool AcceptsValue(DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                var formatted = value.Value.ToString(Options.DateTimeOffsetFormat);
                if (formatted.Length > Field.Length.ToInt32())
                    return false;
            }

            return true;
        }

        public string Value
        {
            get => _value;
            set
            {
                if (value != null && value.Length > Field.Length.ToInt32())
                    throw new ArgumentException(
                        $"The value length {value.Length} of field {Field.Name} is greater than its field length {Field.Length}.");

                _value = value;
            }
        }

        public DbaseCharacterOptions Options { get; }

        public bool TryGetValueAsDateTime(out DateTime? value)
        {
            if (Value == null)
            {
                value = null;
                return true;
            }

            if (DateTime.TryParseExact(Value,
                Options.DateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite,
                out var parsed))
            {
                value = parsed;
                return true;
            }

            value = default;
            return false;
        }

        public bool TrySetValueAsDateTime(DateTime? value)
        {
            if (!value.HasValue)
            {
                Value = null;
                return true;
            }

            var formatted = value.Value.ToString(Options.DateTimeFormat);
            if (formatted.Length > Field.Length.ToInt32())
                return false;

            Value = formatted;
            return true;
        }

        public DateTime? ValueAsDateTime
        {
            get
            {
                if (!TryGetValueAsDateTime(out var parsed))
                {
                    throw new FormatException($"The field {Field.Name} its value needs to be null or an actual date time formatted as '{Options.DateTimeFormat}'.");
                }

                return parsed;
            }
            set
            {
                if (!TrySetValueAsDateTime(value))
                {
                    throw new FormatException($"The field {Field.Name} needs to be longer to hold an actual date time formatted as '{Options.DateTimeFormat}'.");
                }
            }
        }

        public bool TryGetValueAsDateTimeOffset(out DateTimeOffset? value)
        {
            if (Value == null)
            {
                value = null;
                return true;
            }

            if (DateTimeOffset.TryParseExact(Value,
                Options.DateTimeOffsetFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite,
                out var parsed))
            {
                value = parsed;
                return true;
            }

            value = default;
            return false;
        }

        public bool TrySetValueAsDateTimeOffset(DateTimeOffset? value)
        {
            if (!value.HasValue)
            {
                Value = null;
                return true;
            }

            var formatted = value.Value.ToString(Options.DateTimeOffsetFormat);
            if (formatted.Length > Field.Length.ToInt32())
                return false;

            Value = formatted;
            return true;
        }

        public DateTimeOffset? ValueAsDateTimeOffset
        {
            get
            {
                if (!TryGetValueAsDateTimeOffset(out var parsed))
                {
                    throw new FormatException($"The field {Field.Name} its value needs to be null or an actual date time with offset formatted as '{Options.DateTimeOffsetFormat}'.");
                }

                return parsed;
            }
            set
            {
                if (!TrySetValueAsDateTimeOffset(value))
                {
                    throw new FormatException($"The field {Field.Name} needs to be longer to hold an actual date time offset formatted as '{Options.DateTimeOffsetFormat}'.");
                }
            }
        }

        public override void Read(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (reader.PeekChar() == '\0')
            {
                var read = reader.ReadBytes(Field.Length.ToInt32());
                if (read.Length != Field.Length.ToInt32())
                {
                    throw new EndOfStreamException(
                        $"Unable to read beyond the end of the stream. Expected stream to have {Field.Length.ToInt32()} byte(s) available but only found {read.Length} byte(s) as part of reading field {Field.Name.ToString()}."
                    );
                }
                Value = null;
            }
            else
            {
                Value = reader.ReadRightPaddedString(Field.Name.ToString(), Field.Length.ToInt32(), ' ');
            }
        }

        public override void Write(BinaryWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (Value == null)
            {
                writer.Write(new byte[Field.Length.ToInt32()]);
            }
            else
            {
                writer.WriteRightPaddedString(Value, Field.Length.ToInt32(), ' ');
            }
        }

        public override void Accept(IDbaseFieldValueVisitor writer) => writer.Visit(this);
    }
}