// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectoryAsync_Roundtrip_Tests : TarTestsBase
    {
        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData("./file.txt", "subDirectory")]
        [InlineData("../file.txt", "subDirectory")]
        [InlineData("../file.txt", "subDirectory1/subDirectory1.1")]
        [InlineData("./file.txt", "subDirectory1/subDirectory1.1")]
        [InlineData("./file.txt", null)]
        public async Task SymlinkRelativeTargets_InsideTheArchive_RoundtripsSuccessfully_Async(string symlinkTargetPath, string? subDirectory)
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateDirectory(sourceDirectoryName);

            string destinationDirectoryName = Path.Join(root.Path, "destinationDirectory");
            Directory.CreateDirectory(destinationDirectoryName);

            string sourceSubDirectory = Path.Join(sourceDirectoryName, subDirectory);
            if (subDirectory != null) Directory.CreateDirectory(sourceSubDirectory);

            File.Create(Path.Join(sourceDirectoryName, subDirectory, symlinkTargetPath)).Dispose();
            File.CreateSymbolicLink(Path.Join(sourceSubDirectory, "linkToFile"), symlinkTargetPath.Replace('/', Path.DirectorySeparatorChar));

            await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchive, includeBaseDirectory: false);

            await using FileStream archiveStream = File.OpenRead(destinationArchive);
            await using (TarReader reader = new(archiveStream, leaveOpen: true))
            {
                TarEntry? symlinkEntry;
                do
                {
                    symlinkEntry = await reader.GetNextEntryAsync();
                }
                while (symlinkEntry is not null && symlinkEntry.EntryType is not TarEntryType.SymbolicLink);

                Assert.NotNull(symlinkEntry);
                Assert.Equal(TarEntryFormat.Pax, symlinkEntry.Format);
                Assert.Equal(symlinkTargetPath, symlinkEntry.LinkName);
                Assert.Equal(symlinkTargetPath, ((PaxTarEntry)symlinkEntry).ExtendedAttributes[PaxEaLinkName]);
            }

            archiveStream.Position = 0;
            await TarFile.ExtractToDirectoryAsync(archiveStream, destinationDirectoryName, overwriteFiles: true);

            string destinationSubDirectory = Path.Join(destinationDirectoryName, subDirectory);
            string symlinkPath = Path.Join(destinationSubDirectory, "linkToFile");
            Assert.True(File.Exists(symlinkPath));

            FileInfo? fileInfo = new(symlinkPath);
            string expectedTargetPath = OperatingSystem.IsWindows() ? symlinkTargetPath.Replace('/', '\\') : symlinkTargetPath;
            Assert.Equal(expectedTargetPath, fileInfo.LinkTarget);

            FileSystemInfo? symlinkTarget = File.ResolveLinkTarget(symlinkPath, returnFinalTarget: true);
            Assert.True(File.Exists(symlinkTarget.FullName));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData("../file.txt", null)]
        [InlineData("../../file.txt", "subDirectory")]
        public async Task SymlinkRelativeTargets_OutsideTheArchive_Fails_Async(string symlinkTargetPath, string? subDirectory)
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateDirectory(sourceDirectoryName);

            string destinationDirectoryName = Path.Join(root.Path, "destinationDirectory");
            Directory.CreateDirectory(destinationDirectoryName);

            string sourceSubDirectory = Path.Join(sourceDirectoryName, subDirectory);
            if (subDirectory != null) Directory.CreateDirectory(sourceSubDirectory);

            File.CreateSymbolicLink(Path.Join(sourceSubDirectory, "linkToFile"), symlinkTargetPath);

            await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchive, includeBaseDirectory: false);

            using FileStream archiveStream = File.OpenRead(destinationArchive);
            Exception exception = await Assert.ThrowsAsync<IOException>(() => TarFile.ExtractToDirectoryAsync(archiveStream, destinationDirectoryName, overwriteFiles: true));

            Assert.Equal(SR.Format(SR.TarExtractingResultsLinkOutside, symlinkTargetPath, $"{destinationDirectoryName}{Path.DirectorySeparatorChar}"), exception.Message);
        }
    }
}
