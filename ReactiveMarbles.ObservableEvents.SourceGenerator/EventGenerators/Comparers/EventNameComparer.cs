// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace ReactiveMarbles.ObservableEvents.SourceGenerator.EventGenerators
{
    internal class EventNameComparer : IComparer<IEventSymbol>, IEqualityComparer<IEventSymbol>
    {
        public static EventNameComparer Default { get; } = new EventNameComparer();

        public int Compare(IEventSymbol x, IEventSymbol y)
        {
            return string.Compare(x.Name, y.Name, StringComparison.InvariantCulture);
        }

        public bool Equals(IEventSymbol x, IEventSymbol y)
        {
            return string.Equals(x.Name, y.Name, StringComparison.InvariantCulture);
        }

        public int GetHashCode(IEventSymbol obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
