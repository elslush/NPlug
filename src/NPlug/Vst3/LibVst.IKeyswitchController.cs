// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
namespace NPlug.Vst3;


using System;

internal static unsafe partial class LibVst
{
    public partial struct IKeyswitchController
    {
        private static partial int getKeyswitchCount_ccw(ComObject* self, int busIndex, short channel)
        {
            throw new NotImplementedException();
        }
        
        private static partial ComResult getKeyswitchInfo_ccw(ComObject* self, int busIndex, short channel, int keySwitchIndex, LibVst.KeyswitchInfo* info)
        {
            throw new NotImplementedException();
        }
    }
}
