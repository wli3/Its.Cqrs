// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Its.Validation;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     A command that can be applied to an target to trigger some action and record an applicable state change.
    /// </summary>
    /// <typeparam name="T">The type of the target.</typeparam>
    public interface ICommand<in T> : ICommand
    {
        /// <summary>
        ///     Performs the action of the command upon the target.
        /// </summary>
        /// <param name="target">The target to which to apply the command.</param>
        /// <exception cref="CommandValidationException">
        ///     If the command cannot be applied due its state or the state of the target, it should throw a
        ///     <see
        ///         cref="CommandValidationException" />
        ///     indicating the specifics of the failure.
        /// </exception>
        void ApplyTo(T target);

        /// <summary>
        ///     Performs the action of the command upon the target.
        /// </summary>
        /// <param name="target">The target to which to apply the command.</param>
        /// <exception cref="CommandValidationException">
        ///     If the command cannot be applied due its state or the state of the target, it should throw a
        ///     <see
        ///         cref="CommandValidationException" />
        ///     indicating the specifics of the failure.
        /// </exception>
        Task ApplyToAsync(T target);

        /// <summary>
        ///     Gets a validator that can be used to check the valididty of the command against the state of the target before it is applied.
        /// </summary>
        IValidationRule<T> Validator { get; }
    }
}