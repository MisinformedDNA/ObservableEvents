// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace ReactiveMarbles.ObservableEvents
{
    /// <summary>
    /// Extension methods to generate IObservable for contained events on the class.
    /// </summary>
    public static partial class ObservableGeneratorExtensions
    {
        /// <summary>
        /// Gets observable wrappers for all the events contained within the class.
        /// </summary>
        /// <typeparam name="T">foo.</typeparam>
        /// <param name="eventHost">foo bar.</param>
        /// <returns>The events if available.</returns>
        public static NullEvents Events<T>(this T eventHost)
        {
            return default;
        }
    }
}
