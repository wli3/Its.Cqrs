// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public class RequestUserName : Command<CustomerAccount>, ISpecifySchedulingBehavior
    {
        public string UserName { get; set; }

        public override IValidationRule CommandValidator
        {
            get
            {
                var isNotEmpty = Validate.That<RequestUserName>(cmd => !string.IsNullOrEmpty(cmd.UserName))
                                         .WithErrorMessage("User name cannot be empty.");

                var isUnique = Validate.That<RequestUserName>(
                    cmd =>
                        cmd.RequiresReserved(c => c.UserName,
                                             "UserName",
                                             cmd.Principal
                                                .Identity
                                                .Name).Result)
                                       .WithErrorMessage(
                                           (f, c) => $"The user name {c.UserName} is taken. Please choose another.");

                return new ValidationPlan<RequestUserName>
                       {
                           isNotEmpty,
                           isUnique.When(isNotEmpty)
                       };
            }
        }

        public bool RequiresDurableScheduling => false;

        public bool CanBeDeliveredDuringScheduling => true;
    }
}
