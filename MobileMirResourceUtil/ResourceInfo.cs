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
    /// <summary>
    /// 
    /// </summary>
    public class ResourceInfo
    {
        public string FileName { get; set; }
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
            /*
             * 
            最后一位为:8的偏移量是:17,对应资源文件:cboeffect.zip,cbohair.zip,cbohum.zip,cboweapon.zip,chrsel.zip,effect.zip,hair.zip,hair2.zip,hum.zip,hum2.zip,humeffect.zip,magic.zip,magic2.zip,magic3.zip,magic4.zip,magic5.zip,magic6.zip,mon-kulou.zip,mon1.zip,mon10.zip,mon11.zip,mon12.zip,mon13.zip,mon14.zip,mon15.zip,mon16.zip,mon17.zip,mon18.zip,mon19.zip,mon2.zip,mon20.zip,mon21.zip,mon22.zip,mon23.zip,mon24.zip,mon28.zip,mon3.zip,mon4.zip,mon5.zip,mon6.zip,mon7.zip,mon8.zip,mon9.zip,npc.zip,objects.zip,objects10.zip,objects13.zip,objects14.zip,objects2.zip,objects3.zip,objects4.zip,objects5.zip,objects6.zip,objects7.zip,objects8.zip,objects9.zip,prguse.zip,prguse2.zip,prguse3.zip,smtiles.zip,tiles.zip,weapon.zip,weapon2.zip

            最后一位为:16的偏移量是:0,对应资源文件:dnitems.zip,hair2_ball.zip,hum3_ball.zip,items.zip,magic10.zip,magic7.zip,magic8.zip,mon34.zip,stateitem.zip,ui1.zip

            最后一位为:0的偏移量是:0,后9位全是0,对应资源文件:mmap.zip,上一层目录的rs.zip
            */
            this.FileName = Path.GetFileName(sourceFile);
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
            //早期代码通过计算偏移量得到是否有17,后期分析后,发现最后9位的第9位对应的字节,如果是8就是偏移17,16和0都是偏移0,具体规则请看此方法的第一句注释
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
            File.WriteAllText(Path.Combine(dir, "description.txt"), json, Encoding.UTF8);
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
        private byte[] _lastNineBytes;
        public string FileName { get; set; }


        /// <summary>
        /// 最后9位参数记录的数据
        /// </summary>
        public byte[] LastNineBytes
        {
            get => _lastNineBytes;
            set
            {
                _lastNineBytes = value;
                SetLastByteDescription(_lastNineBytes);
            }
        }

        private void SetLastByteDescription(byte[] lastNineBytes)
        {
            if (lastNineBytes == null || lastNineBytes.Length != 9)
                return;
            LastByteDescript =
                $"坐标X:{BitConverter.ToInt16(lastNineBytes, 0)},坐标Y:{BitConverter.ToInt16(lastNineBytes, 2)},图片宽度:{BitConverter.ToInt16(lastNineBytes, 4)},高度:{BitConverter.ToInt16(lastNineBytes, 6)},最后一位(猜测是分类代码):{lastNineBytes[8]}";
        }
        public string LastByteDescript { get; set; }
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
            string path = Path.Combine(dir, this.FileName);
            if (this.FileName.IndexOfAny(new[] { '/', '\\' }) != -1)
            {
                string childDir = Path.GetDirectoryName(path);
                if (!Directory.Exists(childDir))
                    Directory.CreateDirectory(childDir);
            }
            using (var stream = File.Create(path))
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
