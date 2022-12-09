// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace NPlug.Interop;

internal static unsafe partial class LibVst
{
    public partial struct IParameterFinder
    {
        private static partial ComResult findParameter_ToManaged(IParameterFinder* self, int xPos, int yPos, LibVst.ParamID* resultTag)
        {
            throw new NotImplementedException();
        }
    }
}
