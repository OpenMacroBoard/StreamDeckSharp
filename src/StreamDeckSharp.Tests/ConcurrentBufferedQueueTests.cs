using FluentAssertions;
using StreamDeckSharp.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.IsCompleted.Should().BeFalse();
            }
        }

        [Fact(DisplayName = "IsAddingCompleted is false after construction.")]
        public void IsAddingCompletedIsFalseAfterConstruction()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.IsAddingCompleted.Should().BeFalse();
            }
        }

        [Fact(DisplayName = "Count is 0 after construction.")]
        public void CountIsZeroAfterConstruction()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.Count.Should().Be(0);
            }
        }

        [Fact(DisplayName = "IsAddingCompleted and IsCompleted is true after CompleteAdding is called (if queue was empty).")]
        public void CompleteAddingAnEmptyQueue()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.CompleteAdding();
                q.IsAddingCompleted.Should().BeTrue();
                q.IsCompleted.Should().BeTrue();
            }
        }

        [Fact(DisplayName = "Adding an element increases Count.")]
        public void AddingAnElementIncreasesCount()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.Add(1, "Hallo");
                q.Count.Should().Be(1);
                q.Add(2, "Hallo");
                q.Count.Should().Be(2);
            }
        }

        [Fact(DisplayName = "Adding and elements with same id doesn't increase the Count value.")]
        public void AddingAnElementWithSameIdDoesntIncreasesCount()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.Add(1, "Hallo");
                q.Add(1, "Hallo");
                q.Count.Should().Be(1);
            }
        }

        [Fact(DisplayName = "Taking an element retrives the first added element (FIFO).")]
        public void TakingAnElementRetrievesTheFirstOne()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
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
        }

        [Fact(DisplayName = "Taking an element blocks until an element is available.")]
        public void TakingAnElementBlocksUnitAnElementIsAvailable()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(50);  //Make sure Take is called before the next line is executed
                    q.Add(3, "Hallo");
                });

                var result = q.Take();
                result.Key.Should().Be(3);
                result.Value.Should().Be("Hallo");
            }
        }

        [Fact(DisplayName = "Complete Adding causes waiting Take mathods to throw.")]
        public void CompletingTheBufferWhileTakeIsWaitingThrowsAnException()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(50);  //Make sure Take is called before the next line is executed
                    q.CompleteAdding();
                });

                var takeAction = new Action(() =>
                {
                    var result = q.Take();
                });

                takeAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CallTakeAfterCompletingThrowsAnException()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                var takeAction = new Action(() =>
                {
                    var result = q.Take();
                });

                q.CompleteAdding();
                takeAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CompleteAddingPropertiesWorkAsExpected()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.Add(1, "Hallo 1");
                q.Add(2, "Hallo 2");

                q.CompleteAdding();
                q.IsAddingCompleted.Should().BeTrue();
                q.IsCompleted.Should().BeFalse();

                var r = q.Take();
                r = q.Take();

                q.IsCompleted.Should().BeTrue();
            }
        }

        [Fact]
        public void OverrideQueueElementWithSameKey()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.Add(1, "Hallo 1");
                q.Add(2, "Hallo 2");
                q.Add(1, "Hallo NEW");

                var r = q.Take();

                r.Key.Should().Be(1);
                r.Value.Should().Be("Hallo NEW");
            }
        }

        [Fact]
        public void AddAfterCompletionThrowsException()
        {
            using (var q = new ConcurrentBufferedQueue<int, string>(NullTime.Source))
            {
                q.CompleteAdding();

                var addAction = new Action(() =>
                {
                    q.Add(1, "Hallo 1");
                });

                addAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void TheSecondTakeIsBlockedUntilTheKeyCooledDown()
        {
            var timeSource = new ControllableTime();
            using (var q = new ConcurrentBufferedQueue<int, string>(timeSource))
            {
                q.Add(1, "Hallo 1");
                q.Add(1, "Hallo new");

                var r = q.Take();
                bool clockWasSet = false;

                r.Key.Should().Be(1);
                r.Value.Should().Be("Hallo new");
                q.Count.Should().Be(0);

                q.Add(1, "Hallo after cooldown");

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(50);
                    clockWasSet = true;
                    timeSource.Add(100);
                });

                r = q.Take();

                clockWasSet.Should().BeTrue();
                r.Key.Should().Be(1);
                r.Value.Should().Be("Hallo after cooldown");
            }
        }
    }
}
