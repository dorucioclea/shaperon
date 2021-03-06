namespace Be.Vlaanderen.Basisregisters.Shaperon
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    public class DbaseNullableSingle : DbaseFieldValue
    {
        public static readonly DbaseIntegerDigits MaximumIntegerDigits = DbaseFloat.MaximumIntegerDigits;
        public static readonly DbaseFieldLength MaximumLength = DbaseFloat.MaximumLength;
        public static readonly DbaseFieldLength MinimumLength = DbaseFloat.MinimumLength;
        public static readonly DbaseFieldLength PositiveValueMinimumLength = DbaseFloat.PositiveValueMinimumLength;
        public static readonly DbaseFieldLength NegativeValueMinimumLength = DbaseFloat.NegativeValueMinimumLength;
        public static readonly DbaseDecimalCount MaximumDecimalCount = DbaseFloat.MaximumDecimalCount;

        private NumberFormatInfo Provider { get; }

        private float? _value;

        public DbaseNullableSingle(DbaseField field, float? value = null) : base(field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (field.FieldType != DbaseFieldType.Float)
                throw new ArgumentException(
                    $"The field {field.Name} 's type must be float to use it as a single field.", nameof(field));

            if (field.Length < MinimumLength || field.Length > MaximumLength)
                throw new ArgumentException(
                    $"The field {field.Name} 's length ({field.Length}) must be between {MinimumLength} and {MaximumLength}.",
                    nameof(field));

            Provider = new NumberFormatInfo
            {
                NumberDecimalDigits = DbaseDecimalCount.Min(MaximumDecimalCount, field.DecimalCount).ToInt32(),
                NumberDecimalSeparator = "."
            };

            Value = value;
        }

        public bool AcceptsValue(float? value)
        {
            if (!value.HasValue)
                return true;

            if (Field.DecimalCount.ToInt32() == 0)
                return ((float) Math.Truncate(value.Value)).ToString("F", Provider).Length <= Field.Length.ToInt32();

            var digits = DbaseDecimalCount.Min(MaximumDecimalCount, Field.DecimalCount).ToInt32();
            var rounded = (float) Math.Round(value.Value, digits);
            return rounded.ToString("F", Provider).Length <= Field.Length.ToInt32();
        }

        public float? Value
        {
            get => _value;
            set
            {
                if (value.HasValue)
                {
                    if (Field.DecimalCount.ToInt32() == 0)
                    {
                        var truncated = (float) Math.Truncate(value.Value);
                        var length = truncated.ToString(DbaseFloat.FixedPointFormatSpecifier, Provider).Length;
                        if (length > Field.Length.ToInt32())
                            throw new ArgumentException(
                                $"The length ({length}) of the value ({truncated}) of field {Field.Name} is greater than its field length {Field.Length}, which would result in loss of precision.");

                        _value = truncated;
                    }
                    else
                    {
                        var digits = DbaseDecimalCount.Min(MaximumDecimalCount, Field.DecimalCount).ToInt32();
                        var rounded = (float) Math.Round(value.Value, digits);
                        var roundedFormatted = rounded.ToString(DbaseFloat.FixedPointFormatSpecifier, Provider);
                        var length = roundedFormatted.Length;

                        if (length > Field.Length.ToInt32())
                            throw new ArgumentException(
                                $"The length ({length}) of the value ({roundedFormatted}) of field {Field.Name} is greater than its field length {Field.Length}, which would result in loss of precision.");

                        _value = float.Parse(roundedFormatted,
                            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Provider);
                    }
                }
                else
                {
                    _value = default;
                }
            }
        }

        public override void Reset() => _value = default;

        public override void Read(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Value = reader.ReadAsNullableSingle(Field, Provider);
        }

        public override void Write(BinaryWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.WriteAsNullableSingle(Field, Provider, Value);
        }

        public override void Accept(IDbaseFieldValueVisitor visitor) => (visitor as ITypedDbaseFieldValueVisitor)?.Visit(this);
    }
}
