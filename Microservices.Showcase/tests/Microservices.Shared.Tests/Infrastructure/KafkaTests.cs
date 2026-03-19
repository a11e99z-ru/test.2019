using Confluent.Kafka;
using Microservices.Shared.Infrastructure.Kafka;
using Xunit;

namespace Microservices.Shared.Tests.Infrastructure;

public class KafkaTests
{
    [Fact]
    public void StructsWriter_Serialize_LongArray_ShouldBeCorrect()
    {
        // Arrange
        long[] data = [1, 2, 3, 4, 5];
        
        // Act
        var bytes = StructsWriter<long>.Serialize(data);
        
        // Assert
        Assert.Equal(data.Length * sizeof(long), bytes.Length);
        for (int i = 0; i < data.Length; i++)
        {
            var value = BitConverter.ToInt64(bytes, i * sizeof(long));
            Assert.Equal(data[i], value);
        }
    }

    [Fact]
    public void StructsReader_Deserialize_LongArray_ShouldBeCorrect()
    {
        // Arrange
        long[] data = [10, 20, 30];
        var bytes = new byte[data.Length * sizeof(long)];
        for (int i = 0; i < data.Length; i++)
        {
            var b = BitConverter.GetBytes(data[i]);
            Buffer.BlockCopy(b, 0, bytes, i * sizeof(long), b.Length);
        }

        // Act
        var result = StructsReader<long>.Deserialize(bytes);

        // Assert
        Assert.Equal(data.Length, result.Length);
        Assert.Equal(data, result);
    }

    [Fact]
    public void RoundTrip_GuidArray_ShouldBeEquivalent()
    {
        // Arrange
        Guid[] original = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        // Act
        var bytes = StructsWriter<Guid>.Serialize(original);
        var roundTripped = StructsReader<Guid>.Deserialize(bytes);

        // Assert
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_DoubleArray_ShouldBeEquivalent()
    {
        // Arrange
        double[] original = [1.1, 2.2, 3.3, Math.PI, double.MaxValue, double.MinValue];

        // Act
        var bytes = StructsWriter<double>.Serialize(original);
        var roundTripped = StructsReader<double>.Deserialize(bytes);

        // Assert
        Assert.Equal(original, roundTripped);
    }
}
