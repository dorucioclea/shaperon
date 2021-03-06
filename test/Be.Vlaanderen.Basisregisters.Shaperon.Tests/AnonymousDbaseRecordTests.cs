namespace Be.Vlaanderen.Basisregisters.Shaperon
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using AutoFixture;
    using Xunit;

    public class AnonymousDbaseRecordTests
    {
        private readonly Fixture _fixture;

        public AnonymousDbaseRecordTests()
        {
            _fixture = new Fixture();
            _fixture.CustomizeDbaseFieldName();
            _fixture.CustomizeDbaseFieldLength();
            _fixture.CustomizeDbaseDecimalCount();
            _fixture.CustomizeDbaseField();
        }

        [Fact]
        public void ConstructionUsingFieldsHasExpectedResult()
        {
            var fields = _fixture.CreateMany<DbaseField>().ToArray();
            var expectedValues = Array.ConvertAll(fields, field => field.CreateFieldValue());

            var sut = new AnonymousDbaseRecord(fields);

            Assert.False(sut.IsDeleted);
            Assert.Equal(expectedValues, sut.Values, new DbaseFieldValueEqualityComparer());
        }

        [Fact]
        public void ConstructionUsingValuesHasExpectedResult()
        {
            var fields = _fixture.CreateMany<DbaseField>().ToArray();
            var values = Array.ConvertAll(fields, field => field.CreateFieldValue());

            var sut = new AnonymousDbaseRecord(values);

            Assert.False(sut.IsDeleted);
            Assert.Same(values, sut.Values);
        }

        [Fact]
        public void ReadingEndOfFileHasExpectedResult()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    writer.Write(DbaseRecord.EndOfFile);
                    writer.Flush();
                }

                stream.Position = 0;

                using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    var sut = new AnonymousDbaseRecord(new DbaseField[0]);

                    //Act
                    var exception = Assert.Throws<EndOfStreamException>(() => sut.Read(reader));
                    Assert.Equal("The end of file marker was reached.", exception.Message);
                }
            }
        }

        [Fact]
        public void ReadingUnacceptableIsDeletedFlagHasExpectedResult()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    var unacceptable = new Generator<byte>(_fixture).First(candidate => candidate != 0x20 && candidate != 0x2A);
                    writer.Write(unacceptable);
                    writer.Flush();
                }

                stream.Position = 0;

                using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    var sut = new AnonymousDbaseRecord(new DbaseField[0]);

                    //Act
                    Assert.Throws<DbaseRecordException>(() => sut.Read(reader));
                }
            }
        }
    }
}
