// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A basic command scheduler implementation that can be used as the basis for composing command scheduling behaviors.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    internal  class CommandScheduler<TAggregate> :
        ICommandScheduler<TAggregate>
        where TAggregate : class, IEventSourced
    {
        protected readonly ICanHaveCommandsApplied<TAggregate> Target;
        private readonly ICommandPreconditionVerifier preconditionVerifier;

        public CommandScheduler(
            ICanHaveCommandsApplied<TAggregate> target,
            ICommandPreconditionVerifier preconditionVerifier = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            this.Target = target;
            this.preconditionVerifier = preconditionVerifier ??
                                        Configuration.Current.CommandPreconditionVerifier();
        }

        /// <summary>
        /// Schedules the specified command.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command.</param>
        /// <returns>
        /// A task that is complete when the command has been successfully scheduled.
        /// </returns>
        /// <exception cref="System.NotSupportedException">Non-immediate scheduling is not supported.</exception>
        public virtual async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand.Command.CanBeDeliveredDuringScheduling() && scheduledCommand.IsDue())
            {
                if (!await VerifyPrecondition(scheduledCommand))
                {
                    CommandScheduler.DeliverIfPreconditionIsSatisfiedSoon(
                        scheduledCommand,
                        Configuration.Current);
                }
                else
                {
                    // resolve the command scheduler so that delivery goes through the whole pipeline
                    await Configuration.Current.CommandScheduler<TAggregate>().Deliver(scheduledCommand);
                    return;
                }
            }

            if (scheduledCommand.Result == null)
            {
                throw new NotSupportedException("Deferred scheduling is not supported.");
            }
        }

        /// <summary>
        /// Delivers the specified scheduled command to the target aggregate.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command to be applied to the aggregate.</param>
        /// <returns>
        /// A task that is complete when the command has been applied.
        /// </returns>
        /// <remarks>
        /// The scheduler will apply the command and save it, potentially triggering additional consequences.
        /// </remarks>
        public virtual async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            await Target.ApplyScheduledCommand(scheduledCommand, preconditionVerifier);
        }

        /// <summary>
        /// Verifies that the command precondition has been met.
        /// </summary>
        protected async Task<bool> VerifyPrecondition(IScheduledCommand scheduledCommand)
        {
            return await preconditionVerifier.IsPreconditionSatisfied(scheduledCommand);
        }
    }

    internal interface ICanHaveCommandsApplied<out T>
    {
        Task ApplyScheduledCommand(IScheduledCommand<T> scheduledCommand, ICommandPreconditionVerifier preconditionVerifier);
    }
}