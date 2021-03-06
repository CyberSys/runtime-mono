// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Xunit;

namespace Tests.System.IO
{
    public class PathInternal_Windows_Tests
    {
        [Theory,
            InlineData("", "", true, 0),
            InlineData("", "", false, 0),
            InlineData("a", "", true, 0),
            InlineData("a", "", false, 0),
            InlineData("", "b", true, 0),
            InlineData("", "b", false, 0),
            InlineData("\0", "\0", true, 1),
            InlineData("\0", "\0", false, 1),
            InlineData("ABcd", "ABCD", true, 4),
            InlineData("ABCD", "ABcd", true, 4),
            InlineData("ABcd", "ABCD", false, 2),
            InlineData("ABCD", "ABcd", false, 2),
            InlineData("AB\0cd", "AB\0CD", true, 5),
            InlineData("AB\0CD", "AB\0cd", true, 5),
            InlineData("AB\0cd", "AB\0CD", false, 3),
            InlineData("AB\0CD", "AB\0cd", false, 3),
            InlineData("ABc\0", "ABC\0", true, 4),
            InlineData("ABC\0", "ABc\0", true, 4),
            InlineData("ABc\0", "ABC\0", false, 2),
            InlineData("ABC\0", "ABc\0", false, 2),
            InlineData("ABcdxyzl", "ABCDpdq", true, 4),
            InlineData("ABCDxyz", "ABcdpdql", true, 4),
            InlineData("ABcdxyz", "ABCDpdq", false, 2),
            InlineData("ABCDxyzoo", "ABcdpdq", false, 2)]
        public void EqualStartingCharacterCount(string first, string second, bool ignoreCase, int expected)
        {
            Assert.Equal(expected, PathInternal.EqualStartingCharacterCount(first, second, ignoreCase));
        }


        [Theory,
            InlineData(@"", @"", true, 0),
            InlineData(@"", @"", false, 0),
            InlineData(@"a", @"A", true, 1),
            InlineData(@"A", @"a", true, 1),
            InlineData(@"a", @"A", false, 0),
            InlineData(@"A", @"a", false, 0),
            InlineData(@"foo", @"foobar", true, 0),
            InlineData(@"foo", @"foobar", false, 0),
            InlineData(@"foo", @"foo/bar", true, 3),
            InlineData(@"foo", @"foo/bar", false, 3),
            InlineData(@"foo/", @"foo/bar", true, 4),
            InlineData(@"foo/", @"foo/bar", false, 4),
            InlineData(@"foo/bar", @"foo/bar", true, 7),
            InlineData(@"foo/bar", @"foo/bar", false, 7),
            InlineData(@"foo/bar", @"foo/BAR", true, 7),
            InlineData(@"foo/bar", @"foo/BAR", false, 4),
            InlineData(@"foo/bar", @"foo/barb", true, 4),
            InlineData(@"foo/bar", @"foo/barb", false, 4)]
        public void GetCommonPathLength(string first, string second, bool ignoreCase, int expected)
        {
            Assert.Equal(expected, PathInternal.GetCommonPathLength(first, second, ignoreCase));
        }

        public static TheoryData<string, int, string> RemoveRelativeSegmentsData => new TheoryData<string, int, string>
        {
            { @"C:\git\runtime", 2, @"C:\git\runtime"},
            { @"C:\\git\runtime", 2, @"C:\git\runtime"},
            { @"C:\git\\runtime", 2, @"C:\git\runtime"},
            { @"C:\git\.\runtime\.\\", 2, @"C:\git\runtime\"},
            { @"C:\git\runtime", 2, @"C:\git\runtime"},
            { @"C:\git\..\runtime", 2, @"C:\runtime"},
            { @"C:\git\runtime\..\", 2, @"C:\git\"},
            { @"C:\git\runtime\..\..\..\", 2, @"C:\"},
            { @"C:\git\runtime\..\..\.\", 2, @"C:\"},
            { @"C:\git\..\.\runtime\temp\..", 2, @"C:\runtime"},
            { @"C:\git\..\\\.\..\runtime", 2, @"C:\runtime"},
            { @"C:\git\runtime\", 2, @"C:\git\runtime\"},
            { @"C:\git\temp\..\runtime\", 2, @"C:\git\runtime\"},

            { @"C:\.", 3, @"C:\"},
            { @"C:\..", 3, @"C:\"},
            { @"C:\..\..", 3, @"C:\"},
            { @"C:\.", 2, @"C:"},
            { @"C:\..", 2, @"C:"},
            { @"C:\..\..", 2, @"C:"},
            { @"C:A\.", 2, @"C:A"},
            { @"C:A\..", 2, @"C:"},
            { @"C:A\..\..", 2, @"C:"},
            { @"C:A\..\..\..", 2, @"C:"},

            { @"C:\tmp\home", 3, @"C:\tmp\home" },
            { @"C:\tmp\..", 3, @"C:\" },
            { @"C:\tmp\home\..\.\.\", 3, @"C:\tmp\" },
            { @"C:\tmp\..\..\..\", 3, @"C:\" },
            { @"C:\tmp\\home", 3, @"C:\tmp\home" },
            { @"C:\.\tmp\\home", 3, @"C:\tmp\home" },
            { @"C:\..\tmp\home", 3, @"C:\tmp\home" },
            { @"C:\..\..\..\tmp\.\home", 3, @"C:\tmp\home" },
            { @"C:\\tmp\\\home", 3, @"C:\tmp\home" },
            { @"C:\tmp\home\git\.\..\.\git\runtime\..\", 3, @"C:\tmp\home\git\" },
            { @"C:\.\tmp\home", 3, @"C:\tmp\home" },

            { @"C:\tmp\home", 6, @"C:\tmp\home" },
            { @"C:\tmp\..", 6, @"C:\tmp" },
            { @"C:\tmp\home\..\.\.\", 5, @"C:\tmp\" },
            { @"C:\tmp\..\..\..\", 6, @"C:\tmp\" },
            { @"C:\tmp\\home", 5, @"C:\tmp\home" },
            { @"C:\.\tmp\\home", 4, @"C:\.\tmp\home" },
            { @"C:\..\tmp\home", 5, @"C:\..\tmp\home" },
            { @"C:\..\..\..\tmp\.\home", 6, @"C:\..\tmp\home" },
            { @"C:\\tmp\\\home", 7, @"C:\\tmp\home" },
            { @"C:\tmp\home\git\.\..\.\git\runtime\..\", 7, @"C:\tmp\home\git\" },
            { @"C:\.\tmp\home", 5, @"C:\.\tmp\home" },

            { @"C:\tmp\..", 2, @"C:\" },
            { @"C:\tmp\home\..\..\.\", 2, @"C:\" },
            { @"C:\tmp\..\..\..\", 2, @"C:\" },
            { @"C:\tmp\\home", 2, @"C:\tmp\home" },
            { @"C:\.\tmp\\home", 2, @"C:\tmp\home" },
            { @"C:\..\tmp\home", 2, @"C:\tmp\home" },
            { @"C:\..\..\..\tmp\.\home", 2, @"C:\tmp\home" },
            { @"C:\\tmp\\\home", 2, @"C:\tmp\home" },
            { @"C:\tmp\home\git\.\..\.\git\runtime\..\", 2, @"C:\tmp\home\git\" },
            { @"C:\.\tmp\home", 2, @"C:\tmp\home" },

            { @"C:\tmp\..\..\", 10, @"C:\tmp\..\" },
            { @"C:\tmp\home\..\.\.\", 12, @"C:\tmp\home\" },
            { @"C:\tmp\..\..\..\", 10, @"C:\tmp\..\" },
            { @"C:\tmp\\home\..\.\\", 13, @"C:\tmp\\home\" },
            { @"C:\.\tmp\\home\git\git", 9, @"C:\.\tmp\home\git\git" },
            { @"C:\..\tmp\.\home", 10, @"C:\..\tmp\home" },
            { @"C:\..\..\..\tmp\.\home", 10, @"C:\..\..\..\tmp\home" },
            { @"C:\\tmp\\\home\..", 7, @"C:\\tmp\" },
            { @"C:\tmp\home\git\.\..\.\git\runtime\..\", 18, @"C:\tmp\home\git\.\git\" },
            { @"C:\.\tmp\home\.\.\", 9, @"C:\.\tmp\home\" },
        };

        public static TheoryData<string, int, string> RemoveRelativeSegmentsFirstRelativeSegment => new TheoryData<string, int, string>
        {
            { @"C:\\git\runtime", 2, @"C:\git\runtime"},
            { @"C:\.\git\runtime", 2, @"C:\git\runtime"},
            { @"C:\\.\git\.\runtime", 2, @"C:\git\runtime"},
            { @"C:\..\git\runtime", 2, @"C:\git\runtime"},
            { @"C:\.\git\..\runtime", 2, @"C:\runtime"},
            { @"C:\.\git\runtime\..\", 2, @"C:\git\"},
            { @"C:\.\git\runtime\..\..\..\", 2, @"C:\"},
            { @"C:\.\git\runtime\..\..\.\", 2, @"C:\"},
            { @"C:\.\git\..\.\runtime\temp\..", 2, @"C:\runtime"},
            { @"C:\.\git\..\\\.\..\runtime", 2, @"C:\runtime"},
            { @"C:\.\git\runtime\", 2, @"C:\git\runtime\"},
            { @"C:\.\git\temp\..\runtime\", 2, @"C:\git\runtime\"},
            { @"C:\\..\..", 3, @"C:\"}
        };

        public static TheoryData<string, int, string> RemoveRelativeSegmentsSkipAboveRoot => new TheoryData<string, int, string>
        {
            { @"C:\temp\..\" , 7, @"C:\temp\" },
            { @"C:\temp\..\git" , 7, @"C:\temp\git" },
            { @"C:\temp\..\git" , 8, @"C:\temp\git" },
            { @"C:\temp\..\.\" , 8, @"C:\temp\" },
            { @"C:\temp\..\" , 9, @"C:\temp\..\" },
            { @"C:\temp\..\git" , 9, @"C:\temp\..\git" },
            { @"C:\git\..\temp\..\" , 15, @"C:\git\..\temp\" },
            { @"C:\\\.\..\..\temp\..\" , 17, @"C:\\\.\..\..\temp\" },
        };

        public static TheoryData<string, int, string> RemoveRelativeSegmentsFirstRelativeSegmentRoot => new TheoryData<string, int, string>
        {
            { @"C:\\git\runtime", 3, @"C:\git\runtime"},
            { @"C:\.\git\runtime", 3, @"C:\git\runtime"},
            { @"C:\\.\git\.\runtime", 3, @"C:\git\runtime"},
            { @"C:\..\git\runtime", 3, @"C:\git\runtime"},
            { @"C:\.\git\..\runtime", 3, @"C:\runtime"},
            { @"C:\.\git\runtime\..\", 3, @"C:\git\"},
            { @"C:\.\git\runtime\..\..\..\", 3, @"C:\"},
            { @"C:\.\git\runtime\..\..\.\", 3, @"C:\"},
            { @"C:\.\git\..\.\runtime\temp\..", 3, @"C:\runtime"},
            { @"C:\.\git\..\\\.\..\runtime", 3, @"C:\runtime"},
            { @"C:\.\git\runtime\", 3, @"C:\git\runtime\"},
            { @"C:\.\git\temp\..\runtime\", 3, @"C:\git\runtime\"},
        };

        [Theory,
            MemberData(nameof(RemoveRelativeSegmentsData)),
            MemberData(nameof(RemoveRelativeSegmentsFirstRelativeSegment)),
            MemberData(nameof(RemoveRelativeSegmentsFirstRelativeSegmentRoot)),
            MemberData(nameof(RemoveRelativeSegmentsSkipAboveRoot))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void RemoveRelativeSegmentsTest(string path, int skip, string expected)
        {
            Assert.Equal(expected, PathInternal.RemoveRelativeSegments(path, skip));
            Assert.Equal(@"\\.\" + expected, PathInternal.RemoveRelativeSegments(@"\\.\" + path, skip + 4));
            Assert.Equal(@"\\?\" + expected, PathInternal.RemoveRelativeSegments(@"\\?\" + path, skip + 4));
        }

        public static TheoryData<string, int, string> RemoveRelativeSegmentsUncData => new TheoryData<string, int, string>
        {
            { @"Server\Share\git\runtime", 12, @"Server\Share\git\runtime"},
            { @"Server\Share\\git\runtime", 12, @"Server\Share\git\runtime"},
            { @"Server\Share\git\\runtime", 12, @"Server\Share\git\runtime"},
            { @"Server\Share\git\.\runtime\.\\", 12, @"Server\Share\git\runtime\"},
            { @"Server\Share\git\runtime", 12, @"Server\Share\git\runtime"},
            { @"Server\Share\git\..\runtime", 12, @"Server\Share\runtime"},
            { @"Server\Share\git\runtime\..\", 12, @"Server\Share\git\"},
            { @"Server\Share\git\runtime\..\..\..\", 12, @"Server\Share\"},
            { @"Server\Share\git\runtime\..\..\.\", 12, @"Server\Share\"},
            { @"Server\Share\git\..\.\runtime\temp\..", 12, @"Server\Share\runtime"},
            { @"Server\Share\git\..\\\.\..\runtime", 12, @"Server\Share\runtime"},
            { @"Server\Share\git\runtime\", 12, @"Server\Share\git\runtime\"},
            { @"Server\Share\git\temp\..\runtime\", 12, @"Server\Share\git\runtime\"},
        };

        [Theory,
            MemberData(nameof(RemoveRelativeSegmentsUncData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void RemoveRelativeSegmentsUncTest(string path, int skip, string expected)
        {
            Assert.Equal(@"\\" + expected, PathInternal.RemoveRelativeSegments(@"\\" + path, skip + 2));
            Assert.Equal(@"\\.\UNC\" + expected, PathInternal.RemoveRelativeSegments(@"\\.\UNC\" + path, skip + 8));
            Assert.Equal(@"\\?\UNC\" + expected, PathInternal.RemoveRelativeSegments(@"\\?\UNC\" + path, skip + 8));
        }

        public static TheoryData<string, int, string> RemoveRelativeSegmentsDeviceData => new TheoryData<string, int, string>
        {
            { @"\\.\git\runtime", 7, @"\\.\git\runtime"},
            { @"\\.\git\runtime", 7, @"\\.\git\runtime"},
            { @"\\.\git\\runtime", 7, @"\\.\git\runtime"},
            { @"\\.\git\.\runtime\.\\", 7, @"\\.\git\runtime\"},
            { @"\\.\git\runtime", 7, @"\\.\git\runtime"},
            { @"\\.\git\..\runtime", 7, @"\\.\git\runtime"},
            { @"\\.\git\runtime\..\", 7, @"\\.\git\"},
            { @"\\.\git\runtime\..\..\..\", 7, @"\\.\git\"},
            { @"\\.\git\runtime\..\..\.\", 7, @"\\.\git\"},
            { @"\\.\git\..\.\runtime\temp\..", 7, @"\\.\git\runtime"},
            { @"\\.\git\..\\\.\..\runtime", 7, @"\\.\git\runtime"},
            { @"\\.\git\runtime\", 7, @"\\.\git\runtime\"},
            { @"\\.\git\temp\..\runtime\", 7, @"\\.\git\runtime\"},

            { @"\\.\.\runtime", 5, @"\\.\.\runtime"},
            { @"\\.\.\runtime", 5, @"\\.\.\runtime"},
            { @"\\.\.\\runtime", 5, @"\\.\.\runtime"},
            { @"\\.\.\.\runtime\.\\", 5, @"\\.\.\runtime\"},
            { @"\\.\.\runtime", 5, @"\\.\.\runtime"},
            { @"\\.\.\..\runtime", 5, @"\\.\.\runtime"},
            { @"\\.\.\runtime\..\", 5, @"\\.\.\"},
            { @"\\.\.\runtime\..\..\..\", 5, @"\\.\.\"},
            { @"\\.\.\runtime\..\..\.\", 5, @"\\.\.\"},
            { @"\\.\.\..\.\runtime\temp\..", 5, @"\\.\.\runtime"},
            { @"\\.\.\..\\\.\..\runtime", 5, @"\\.\.\runtime"},
            { @"\\.\.\runtime\", 5, @"\\.\.\runtime\"},
            { @"\\.\.\temp\..\runtime\", 5, @"\\.\.\runtime\"},

            { @"\\.\..\runtime", 6, @"\\.\..\runtime"},
            { @"\\.\..\runtime", 6, @"\\.\..\runtime"},
            { @"\\.\..\\runtime", 6, @"\\.\..\runtime"},
            { @"\\.\..\.\runtime\.\\", 6, @"\\.\..\runtime\"},
            { @"\\.\..\runtime", 6, @"\\.\..\runtime"},
            { @"\\.\..\..\runtime", 6, @"\\.\..\runtime"},
            { @"\\.\..\runtime\..\", 6, @"\\.\..\"},
            { @"\\.\..\runtime\..\..\..\", 6, @"\\.\..\"},
            { @"\\.\..\runtime\..\..\.\", 6, @"\\.\..\"},
            { @"\\.\..\..\.\runtime\temp\..", 6, @"\\.\..\runtime"},
            { @"\\.\..\..\\\.\..\runtime", 6, @"\\.\..\runtime"},
            { @"\\.\..\runtime\", 6, @"\\.\..\runtime\"},
            { @"\\.\..\temp\..\runtime\", 6, @"\\.\..\runtime\"},

            { @"\\.\\runtime", 4, @"\\.\runtime"},
            { @"\\.\\runtime", 4, @"\\.\runtime"},
            { @"\\.\\\runtime", 4, @"\\.\runtime"},
            { @"\\.\\.\runtime\.\\", 4, @"\\.\runtime\"},
            { @"\\.\\runtime", 4, @"\\.\runtime"},
            { @"\\.\\..\runtime", 4, @"\\.\runtime"},
            { @"\\.\\runtime\..\", 4, @"\\.\"},
            { @"\\.\\runtime\..\..\..\", 4, @"\\.\"},
            { @"\\.\\runtime\..\..\.\", 4, @"\\.\"},
            { @"\\.\\..\.\runtime\temp\..", 4, @"\\.\runtime"},
            { @"\\.\\..\\\.\..\runtime", 4, @"\\.\runtime"},
            { @"\\.\\runtime\", 4, @"\\.\runtime\"},
            { @"\\.\\temp\..\runtime\", 4, @"\\.\runtime\"},
        };

        public static TheoryData<string, int, string> RemoveRelativeSegmentsDeviceRootData => new TheoryData<string, int, string>
        {
            { @"\\.\git\runtime", 8, @"\\.\git\runtime"},
            { @"\\.\git\runtime", 8, @"\\.\git\runtime"},
            { @"\\.\git\\runtime", 8, @"\\.\git\runtime"},
            { @"\\.\git\.\runtime\.\\", 8, @"\\.\git\runtime\"},
            { @"\\.\git\runtime", 8, @"\\.\git\runtime"},
            { @"\\.\git\..\runtime", 8, @"\\.\git\runtime"},
            { @"\\.\git\runtime\..\", 8, @"\\.\git\"},
            { @"\\.\git\runtime\..\..\..\", 8, @"\\.\git\"},
            { @"\\.\git\runtime\..\..\.\", 8, @"\\.\git\"},
            { @"\\.\git\..\.\runtime\temp\..", 8, @"\\.\git\runtime"},
            { @"\\.\git\..\\\.\..\runtime", 8, @"\\.\git\runtime"},
            { @"\\.\git\runtime\", 8, @"\\.\git\runtime\"},
            { @"\\.\git\temp\..\runtime\", 8, @"\\.\git\runtime\"},

            { @"\\.\.\runtime", 6, @"\\.\.\runtime"},
            { @"\\.\.\runtime", 6, @"\\.\.\runtime"},
            { @"\\.\.\\runtime", 6, @"\\.\.\runtime"},
            { @"\\.\.\.\runtime\.\\", 6, @"\\.\.\runtime\"},
            { @"\\.\.\runtime", 6, @"\\.\.\runtime"},
            { @"\\.\.\..\runtime", 6, @"\\.\.\runtime"},
            { @"\\.\.\runtime\..\", 6, @"\\.\.\"},
            { @"\\.\.\runtime\..\..\..\", 6, @"\\.\.\"},
            { @"\\.\.\runtime\..\..\.\", 6, @"\\.\.\"},
            { @"\\.\.\..\.\runtime\temp\..", 6, @"\\.\.\runtime"},
            { @"\\.\.\..\\\.\..\runtime", 6, @"\\.\.\runtime"},
            { @"\\.\.\runtime\", 6, @"\\.\.\runtime\"},
            { @"\\.\.\temp\..\runtime\", 6, @"\\.\.\runtime\"},

            { @"\\.\..\runtime", 7, @"\\.\..\runtime"},
            { @"\\.\..\runtime", 7, @"\\.\..\runtime"},
            { @"\\.\..\\runtime", 7, @"\\.\..\runtime"},
            { @"\\.\..\.\runtime\.\\", 7, @"\\.\..\runtime\"},
            { @"\\.\..\runtime", 7, @"\\.\..\runtime"},
            { @"\\.\..\..\runtime", 7, @"\\.\..\runtime"},
            { @"\\.\..\runtime\..\", 7, @"\\.\..\"},
            { @"\\.\..\runtime\..\..\..\", 7, @"\\.\..\"},
            { @"\\.\..\runtime\..\..\.\", 7, @"\\.\..\"},
            { @"\\.\..\..\.\runtime\temp\..", 7, @"\\.\..\runtime"},
            { @"\\.\..\..\\\.\..\runtime", 7, @"\\.\..\runtime"},
            { @"\\.\..\runtime\", 7, @"\\.\..\runtime\"},
            { @"\\.\..\temp\..\runtime\", 7, @"\\.\..\runtime\"},

            { @"\\.\\runtime", 5, @"\\.\\runtime"},
            { @"\\.\\runtime", 5, @"\\.\\runtime"},
            { @"\\.\\\runtime", 5, @"\\.\\runtime"},
            { @"\\.\\.\runtime\.\\", 5, @"\\.\\runtime\"},
            { @"\\.\\runtime", 5, @"\\.\\runtime"},
            { @"\\.\\..\runtime", 5, @"\\.\\runtime"},
            { @"\\.\\runtime\..\", 5, @"\\.\\"},
            { @"\\.\\runtime\..\..\..\", 5, @"\\.\\"},
            { @"\\.\\runtime\..\..\.\", 5, @"\\.\\"},
            { @"\\.\\..\.\runtime\temp\..", 5, @"\\.\\runtime"},
            { @"\\.\\..\\\.\..\runtime", 5, @"\\.\\runtime"},
            { @"\\.\\runtime\", 5, @"\\.\\runtime\"},
            { @"\\.\\temp\..\runtime\", 5, @"\\.\\runtime\"},
        };

        [Theory,
            MemberData(nameof(RemoveRelativeSegmentsDeviceData)),
            MemberData(nameof(RemoveRelativeSegmentsDeviceRootData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void RemoveRelativeSegmentsDeviceTest(string path, int skip, string expected)
        {
            Assert.Equal(expected, PathInternal.RemoveRelativeSegments(path, skip));
            StringBuilder sb = new StringBuilder(expected);
            sb.Replace('.', '?', 0, 4);
            expected = sb.ToString();

            sb = new StringBuilder(path);
            sb.Replace('.', '?', 0, 4);
            path = sb.ToString();
            Assert.Equal(expected, PathInternal.RemoveRelativeSegments(path, skip));
        }

        public static TheoryData<string, int, string> RemoveRelativeSegmentUnixData => new TheoryData<string, int, string>
        {
            { "/tmp/home", 1, "/tmp/home" },
            { "/tmp/..", 1, "/" },
            { "/tmp/home/../././", 1, "/tmp/" },
            { "/tmp/../../../", 1, "/" },
            { "/tmp//home", 1, "/tmp/home" },
            { "/./tmp//home", 1, "/tmp/home" },
            { "/../tmp/home", 1, "/tmp/home" },
            { "/../../../tmp/./home", 1, "/tmp/home" },
            { "//tmp///home", 1, "/tmp/home" },
            { "/tmp/home/git/./.././git/runtime/../", 1, "/tmp/home/git/" },
            { "/./tmp/home", 1, "/tmp/home" },

            { "/tmp/home", 4, "/tmp/home" },
            { "/tmp/..", 4, "/tmp" },
            { "/tmp/home/../././", 4, "/tmp/" },
            { "/tmp/../../../", 4, "/tmp/" },
            { "/tmp//home", 4, "/tmp/home" },
            { "/./tmp//home", 2, "/./tmp/home" },
            { "/../tmp/home", 3, "/../tmp/home" },
            { "/../../../tmp/./home", 4, "/../tmp/home" },
            { "//tmp///home", 5, "//tmp/home" },
            { "/tmp/home/git/./.././git/runtime/../", 5, "/tmp/home/git/" },
            { "/./tmp/home", 3, "/./tmp/home" },

            { "/tmp/../../", 8, "/tmp/../" },
            { "/tmp/home/../././", 10, "/tmp/home/" },
            { "/tmp/../../../", 8, "/tmp/../" },
            { "/tmp//home/.././/", 11, "/tmp//home/" },
            { "/./tmp//home/git/git", 7, "/./tmp/home/git/git" },
            { "/../tmp/./home", 8, "/../tmp/home" },
            { "/../../../tmp/./home", 8, "/../../../tmp/home" },
            { "//tmp///home/..", 5, "//tmp/" },
            { "/tmp/home/git/./.././git/runtime/../", 16, "/tmp/home/git/./git/" },
            { "/./tmp/home/././", 7, "/./tmp/home/" },
        };

        [Theory,
            MemberData(nameof(RemoveRelativeSegmentUnixData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void RemoveRelativeSegmentsUnix(string path, int skip, string expected)
        {
            Assert.Equal(expected, PathInternal.RemoveRelativeSegments(path, skip));
        }
    }
}
