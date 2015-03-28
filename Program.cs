// Copyright © Cloudbase Solutions Srl 2015
// All Rights Reserved
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at 
// http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using Alphaleonis.Win32.Vss;

namespace Cloudbase.VSSSnapshotCopy
{
    class Program
    {
        static void BackupFile(string srcPath, string destPath)
        {
            srcPath = Path.GetFullPath(srcPath);

            IVssImplementation vssImpl = VssUtils.LoadImplementation();
            using (IVssBackupComponents vss = vssImpl.CreateVssBackupComponents())
            {
                vss.InitializeForBackup(null);

                vss.SetBackupState(true, true, VssBackupType.Full, false);
                vss.SetContext(VssSnapshotContext.FileShareBackup);

                using (IVssAsync async = vss.GatherWriterMetadata())
                    async.Wait();

                Guid vssSet = vss.StartSnapshotSet();

                var rootPath = Path.GetPathRoot(srcPath);
                var snapshotId = vss.AddToSnapshotSet(rootPath, Guid.Empty);

                using (IVssAsync async = vss.DoSnapshotSet())
                    async.Wait();

                try
                {
                    var snapshotPath = vss.GetSnapshotProperties(snapshotId).SnapshotDeviceObject;

                    var pathNoRoot = srcPath.Substring(rootPath.Length);
                    var path = Path.Combine(snapshotPath, pathNoRoot);

                    if (File.Exists(destPath))
                        File.Delete(destPath);
                    Alphaleonis.Win32.Filesystem.File.Copy(path, destPath);
                }
                finally
                {
                    vss.DeleteSnapshotSet(vssSet, true);
                }
            }
        }

        static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: VSSSnapshotCopy <source> <destination>");
                Environment.Exit(2);
            }

            var srcPath = args[0];
            var destPath = args[1];

            try
            {
                if(!File.Exists(srcPath))
                    throw new FileNotFoundException("Source path not found", srcPath);

                var dirName = Path.GetDirectoryName(destPath);
                if(!Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                Console.WriteLine(string.Format("Backing up \"{0}\" to \"{1}\"", srcPath, destPath));

                BackupFile(srcPath, destPath);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
