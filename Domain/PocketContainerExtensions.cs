// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Pocket;

namespace Microsoft.Its.Domain
{
    internal static class PocketContainerExtensions
    {
        public static PocketContainer UseImmediateCommandScheduling(this PocketContainer container)
        {
            container.AddStrategy(type =>
            {
                if (type.IsInterface &&  
                    type.IsGenericType && 
                    type.GetGenericTypeDefinition() == typeof(ICommandScheduler<>))
                {
                    var targetType = type.GetGenericArguments().First();
                    var schedulerType = typeof (CommandScheduler<>).MakeGenericType(targetType);

                    return c => c.Resolve(schedulerType);
                }

                return null;
            });

            container.AddStrategy(type =>
            {
                if (type.IsInterface &&
                    type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(ICanHaveCommandsApplied<>))
                {
                    var targetType = type.GetGenericArguments().First();
                    var schedulerType = typeof(EventSourcedCommandApplier<>).MakeGenericType(targetType);

                    return c => c.Resolve(schedulerType);
                }

                return null;
            });



            return container;
        }
    }
}
