using AwesomeAssertions;
using StreamDeckSharp.Internals;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class ConcurrentBufferedQueueTests
    {
        [Fact(DisplayName = "IsCompleted is false after construction.")]
        public void IsCompletedIsFalseAfterConstruction()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();
            q.IsCompleted.Should().BeFalse();
        }

        [Fact(DisplayName = "IsAddingCompleted is false after construction.")]
        public void IsAddingCompletedIsFalseAfterConstruction()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();
            q.IsAddingCompleted.Should().BeFalse();
        }

        [Fact(DisplayName = "Count is 0 after construction.")]
        public void CountIsZeroAfterConstruction()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();
            q.Count.Should().Be(0);
        }

        [Fact(DisplayName = "IsAddingCompleted and IsCompleted is true after CompleteAdding is called (if queue was empty).")]
        public void CompleteAddingAnEmptyQueue()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            q.CompleteAdding();
            q.IsAddingCompleted.Should().BeTrue();
            q.IsCompleted.Should().BeTrue();
        }

        [Fact(DisplayName = "Adding an element increases Count.")]
        public void AddingAnElementIncreasesCount()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            q.Add(1, "Hallo");
            q.Count.Should().Be(1);

            q.Add(2, "Hallo");
            q.Count.Should().Be(2);
        }

        [Fact(DisplayName = "Adding and elements with same id doesn't increase the Count value.")]
        public void AddingAnElementWithSameIdDoesNotIncreasesCount()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            q.Add(1, "Hallo");
            q.Add(1, "Hallo");

            q.Count.Should().Be(1);
        }

        [Fact(DisplayName = "Taking an element retrieves the first added element (FIFO).")]
        public void TakingAnElementRetrievesTheFirstOne()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            q.Add(3, "Hallo 3");
            q.Add(7, "Hallo 7");
            q.Add(1, "Hallo 1");

            var result = q.Take();
            result.Key.Should().Be(3);
            result.Value.Should().Be("Hallo 3");

            result = q.Take();
            result.Key.Should().Be(7);
            result.Value.Should().Be("Hallo 7");

            result = q.Take();
            result.Key.Should().Be(1);
            result.Value.Should().Be("Hallo 1");
        }

        [Fact(DisplayName = "Taking an element blocks until an element is available.")]
        public void TakingAnElementBlocksUnitAnElementIsAvailable()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            Task.Factory.StartNew(() =>
            {
#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests
                Thread.Sleep(50);  // Make sure Take is called before the next line is executed
#pragma warning restore S2925
                q.Add(3, "Hallo");
            });

            var (success, key, value) = q.Take();

            success.Should().BeTrue();
            key.Should().Be(3);
            value.Should().Be("Hallo");
        }

        [Fact(DisplayName = "Complete Adding causes waiting Take methods to throw.")]
        public void CompletingTheBufferWhileTakeIsWaitingThrowsAnException()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            Task.Factory.StartNew(() =>
            {
#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests
                Thread.Sleep(50);  // Make sure Take is called before the next line is executed
#pragma warning restore S2925
                q.CompleteAdding();
            });

            var (success, _, _) = q.Take();
            success.Should().BeFalse();
        }

        [Fact]
        public void CallTakeAfterCompletingThrowsAnException()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            bool? success = null;
            var takeAction = new Action(() => (success, _, _) = q.Take());

            q.CompleteAdding();
            takeAction();

#pragma warning disable AV2202 // Prefer language syntax over explicit calls to underlying implementations
            success.HasValue.Should().BeTrue();
#pragma warning restore AV2202

#pragma warning disable S3655 // Empty nullable value should not be accessed
            success.Value.Should().BeFalse();
#pragma warning restore S3655
        }

        [Fact]
        public void CompleteAddingPropertiesWorkAsExpected()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            q.Add(1, "Hallo 1");
            q.Add(2, "Hallo 2");

            q.CompleteAdding();
            q.IsAddingCompleted.Should().BeTrue();
            q.IsCompleted.Should().BeFalse();

            _ = q.Take();
            _ = q.Take();

            q.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void OverrideQueueElementWithSameKey()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            q.Add(1, "Hallo 1");
            q.Add(2, "Hallo 2");
            q.Add(1, "Hallo NEW");

            var (success, key, value) = q.Take();

            success.Should().BeTrue();
            key.Should().Be(1);
            value.Should().Be("Hallo NEW");
        }

        [Fact]
        public void AddAfterCompletionThrowsException()
        {
            using var q = new ConcurrentBufferedQueue<int, string>();

            q.CompleteAdding();

            var addAction = new Action(() => q.Add(1, "Hallo 1"));

            addAction.Should().Throw<InvalidOperationException>();
        }
    }
}
