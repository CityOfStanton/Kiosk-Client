﻿/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

namespace System.Runtime.CompilerServices
{
    // This is required because of the use of c# 9.0 features in UWP.
    // See: https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
    internal static class IsExternalInit { }
}