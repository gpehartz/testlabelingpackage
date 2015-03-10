// Guids.cs
// MUST match guids.h
using System;

namespace ICETeam.TestPackage
{
    static class GuidList
    {
        public const string guidTestPackagePkgString = "2ce7c7d8-29da-48d7-8a32-ec1fdc842258";
        public const string guidTestPackageCmdSetString = "834abcc2-72e7-4765-8f8d-8db68066cdef";

        public static readonly Guid guidTestPackageCmdSet = new Guid(guidTestPackageCmdSetString);


        public const string guidNavigationCmdSetString = "4c4c0def-62df-4b7f-acca-5dc36c58cb16";

        public static readonly Guid guidNavigationCmdSet = new Guid(guidNavigationCmdSetString);
    };
}