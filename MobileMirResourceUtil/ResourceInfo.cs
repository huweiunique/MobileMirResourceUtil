using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Swifter.Json;

namespace MobileMirResourceUtil
{
    public class ResourceInfo
    {
        /// <summary>
        /// 文件地址偏移量
        /// </summary>
        public int FileLengthOffset
        {
            get;
            set;
        }
        public List<ChildFile> ChildFiles { get; private set; }

        public async Task DePackageAsync(string sourceFile)
        {
            ChildFiles = new List<ChildFile>();
            using (var stream = CreateReadStream(sourceFile))
            {
                if (!IsMobileResourceFile(stream))
                {
                    throw new Exception("不是正确的资源压缩包");
                }
                //读取4个字节.应该png是长度
                int pngNameListOffset = GetPngNameListOffset(stream);

                var array = new byte[4];
                stream.Seek(pngNameListOffset, SeekOrigin.Begin);
                await stream.ReadAsync(array, 0, array.Length).ConfigureAwait(false);
                //读取4位,是有多少个文件
                int fileCount = BitConverter.ToInt32(array, 0);
                for (int i = 0; i < fileCount; i++)
                {
                    //读取4位是文件名长度
                    await stream.ReadAsync(array, 0, array.Length).ConfigureAwait(false);
                    var fileLength = BitConverter.ToInt32(array, 0);
                    //根据文件名长度,读取文件名
                    var fileNameArray = new byte[fileLength];

                    await stream.ReadAsync(fileNameArray, 0, fileLength).ConfigureAwait(false);
                    var fileName = Encoding.UTF8.GetString(fileNameArray);
                    //往下再读取4个byte,得到长度
                    await stream.ReadAsync(array, 0, array.Length).ConfigureAwait(false);
                    var length = BitConverter.ToInt32(array, 0);
                    //往下再读取4个byte,得到文件地址
                    await stream.ReadAsync(array, 0, array.Length).ConfigureAwait(false);
                    var offset = BitConverter.ToInt32(array, 0);

                    //剩下9个字节不知道干什么的
                    var otherParamArray = new byte[9];
                    stream.Read(otherParamArray, 0, otherParamArray.Length);
                    var childFile = new ChildFile()
                    {
                        FileName = fileName,
                        LastNineBytes = otherParamArray,
                        Offset = offset,
                        Length = length
                    };
                    ChildFiles.Add(childFile);
                    //Console.WriteLine($"{offset}\t{length}");
                    //Console.WriteLine(childFile.ToString());
                }
            }

            AutoCalcFileLengthOffset();
            SaveToFile(sourceFile);
        }

        private void SaveToFile(string sourceFile)
        {
            //多线程读取实际文件内容,并保存
            //先恢复到原始位置
            string dir = Path.Combine(Path.GetDirectoryName(sourceFile),
                Path.GetFileNameWithoutExtension(sourceFile));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            //用共享方式多次打开文件流同时读取,以最快的方式解压
            this.ChildFiles.AsParallel().ForAll(async file =>
            {
                using (var otherStream = CreateReadStream(sourceFile))
                {
                    await file.SaveToFileAsync(dir, otherStream, this.FileLengthOffset).ConfigureAwait(false);
                }
            });
            WritePackageInfo(dir);
        }

        private void WritePackageInfo(string dir)
        {
            string json = JsonFormatter.SerializeObject(this);
            File.WriteAllText(Path.Combine(dir, "descript.txt"), json, Encoding.UTF8);
        }

        private void AutoCalcFileLengthOffset()
        {

            //计算文件读取长度的偏移量,有的资源包偏移17,有的不偏移,这里智能判定
            if (ChildFiles.Count > 2)
            {
                if (!Calc(ChildFiles))
                {
                    //发现有时候读取出来的顺序对不上,则按地址排序
                    Calc(ChildFiles.OrderBy(x => x.Offset).Take(2).ToArray());
                }
            }

            bool Calc(IList<ChildFile> array)
            {
                var temp = array[0].Length - (array[1].Offset - array[0].Offset);
                //only accept 0 and 17
                if (temp == 0 || temp == 17)
                {
                    this.FileLengthOffset = temp;
                    return true;
                }

                return false;
            }
        }

        private FileStream CreateReadStream(string path)
        {
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }


        private bool IsMobileResourceFile(Stream stream)
        {
            var array = new byte[12];
            int readLength = 12;
            int offset = 0;
            stream.Read(array, offset, readLength);
            string header = Encoding.ASCII.GetString(array, offset, readLength);
            return header.TrimEnd('\0') == "www.sdo.com";
        }
        private int GetPngNameListOffset(Stream stream)
        {
            var array = new byte[4];
            stream.Seek(12, SeekOrigin.Begin);
            stream.Read(array, 0, array.Length);
            return BitConverter.ToInt32(array, 0);
        }

    }

    public class ChildFile
    {
        public string FileName { get; set; }


        /// <summary>
        /// 最后9位参数记录的数据
        /// </summary>
        public byte[] LastNineBytes { get; set; }
        /// <summary>
        /// 偏移量
        /// </summary>
        internal int Offset { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        internal int Length { get; set; }

        internal async Task SaveToFileAsync(string dir, Stream stream, int fileLengthOffset)
        {
            var data = await ReadFromStreamAsync(stream, fileLengthOffset).ConfigureAwait(false);
            await SaveFileAsync(dir, data).ConfigureAwait(false);
        }

        private async Task<byte[]> ReadFromStreamAsync(Stream stream, int fileLengthOffset)
        {
            var buffer = new byte[this.Length - fileLengthOffset];
            stream.Seek(this.Offset, SeekOrigin.Begin);
            await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            return buffer;
        }

        private async Task SaveFileAsync(string dir, byte[] data)
        {
            using (var stream = File.Create(Path.Combine(dir, this.FileName)))
            {
                await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"文件名:{FileName}");
            sb.AppendLine($"后9位:{ BitConverter.ToString(LastNineBytes)}前4位:{BitConverter.ToUInt32(LastNineBytes, 0)},5-8位:{BitConverter.ToUInt32(LastNineBytes, 4)},9位:{LastNineBytes[8]}");
            sb.AppendLine(($"后9位把第一位单独拿出来:{ BitConverter.ToString(LastNineBytes)}第一位:{LastNineBytes[0]},2-5位:{BitConverter.ToUInt32(LastNineBytes, 1)},6-9位:{BitConverter.ToUInt32(LastNineBytes, 5)}"));
            return sb.ToString();
        }
    }

}
