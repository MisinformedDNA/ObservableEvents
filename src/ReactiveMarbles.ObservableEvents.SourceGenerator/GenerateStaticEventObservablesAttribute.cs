// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace ReactiveMarbles.ObservableEvents
{
    /// <summary>
    /// Generates a IObservable`T` wrapper for the specified type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class GenerateStaticEventObservablesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateStaticEventObservablesAttribute"/> class.
        /// </summary>
        /// <param name="type">The static type to generate event observable wrappers for.</param>
        public GenerateStaticEventObservablesAttribute(Type type)
        {
            Type = type;
        }

        /// <summary>Gets the Type to generate the static event observable wrappers for.</summary>
        public Type Type { get; }
    }
}
