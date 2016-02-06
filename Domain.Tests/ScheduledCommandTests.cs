// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class ScheduledCommandTests
    {
        [Test]
        public void A_ScheduledCommand_is_due_if_no_due_time_is_specified()
        {
            var command = new ScheduledCommand<Order>(new AddItem(), Any.Guid());

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_current_domain_clock()
        {
            var command = new ScheduledCommand<Order>(
                new AddItem(),
                Any.Guid(),
                dueTime: Clock.Now().Subtract(TimeSpan.FromSeconds(1)));

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_specified_clock()
        {
            var command = new ScheduledCommand<Order>(
                new AddItem(),
                Any.Guid(),
                Clock.Now().Add(TimeSpan.FromDays(1)));

            command.IsDue(Clock.Create(() => Clock.Now().Add(TimeSpan.FromDays(2))))
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_a_due_time_is_specified_that_is_later_than_the_current_domain_clock()
        {
            var command = new ScheduledCommand<Order>(
                new AddItem(),
                Any.Guid(),
                Clock.Now().Add(TimeSpan.FromSeconds(1)));

            command.IsDue()
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_it_has_already_been_delivered_and_failed()
        {
            var command = new ScheduledCommand<Order>(
                new AddItem(),
                Any.Guid());

            command.Result = new CommandFailed(command);

            command.IsDue().Should().BeFalse();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_it_has_already_been_delivered_and_succeeded()
        {
            var command = new ScheduledCommand<Order>(
                new AddItem(),
                Any.Guid());

            command.Result = new CommandSucceeded(command);

            command.IsDue().Should().BeFalse();
        }

        [Test]
        public void A_ScheduledCommand_with_an_non_event_sourced_target_has_a_null_AggregateId()
        {
            var command = new ScheduledCommand<CommandTarget>(
                new TestCommand(),
                Any.Guid().ToString());

            command.AggregateId.Should().Be(null);
        }

        [Test]
        public void A_ScheduledCommand_with_an_event_sourced_target_has_a_non_null_AggregateId()
        {
            var id = Any.Guid();

            var command = new ScheduledCommand<Order>(new AddItem(), id.ToString());

            command.AggregateId.Should().Be(id);
        }
    }
}