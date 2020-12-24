/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

namespace System.Runtime.CompilerServices
{
    // This is required because of the use of c# 9.0 features in UWP.
    // See: https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
    internal static class IsExternalInit { }
}