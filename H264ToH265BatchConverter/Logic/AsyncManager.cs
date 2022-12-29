using H264ToH265BatchConverter.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace H264ToH265BatchConverter.Logic
{
    internal static class AsyncManager
    {
        internal static async Task ConvertAsync(FileConversion file)
        {
            var task = file.Convert();

            await task.WaitAsync(new CancellationToken());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="action">Actions to do after encoding detection (ex: update ui and global progress)</param>
        /// <returns></returns>
        internal static async Task DetectEncodingAsync(FileConversion file, Action<string> action)
        {
            await Task.Run(() =>
            {
                var encoding = H264Converter.DetectFileEncoding(file.File.File.FullName);

                action?.Invoke(encoding);
            });
        }
    }
}
