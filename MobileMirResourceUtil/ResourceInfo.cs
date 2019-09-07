using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MobileMirResourceUtil
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceInfo
    {
        public byte TypeCode { get; set; }
        public List<ChildFile> ChildFiles { get; private set; }

        public async Task DePackageAsync(string sourceFile)
        {
            /*
             * 
            最后一位为:8的偏移量是:17,对应资源文件:cboeffect.zip,cbohair.zip,cbohum.zip,cboweapon.zip,chrsel.zip,effect.zip,hair.zip,hair2.zip,hum.zip,hum2.zip,humeffect.zip,magic.zip,magic2.zip,magic3.zip,magic4.zip,magic5.zip,magic6.zip,mon-kulou.zip,mon1.zip,mon10.zip,mon11.zip,mon12.zip,mon13.zip,mon14.zip,mon15.zip,mon16.zip,mon17.zip,mon18.zip,mon19.zip,mon2.zip,mon20.zip,mon21.zip,mon22.zip,mon23.zip,mon24.zip,mon28.zip,mon3.zip,mon4.zip,mon5.zip,mon6.zip,mon7.zip,mon8.zip,mon9.zip,npc.zip,objects.zip,objects10.zip,objects13.zip,objects14.zip,objects2.zip,objects3.zip,objects4.zip,objects5.zip,objects6.zip,objects7.zip,objects8.zip,objects9.zip,prguse.zip,prguse2.zip,prguse3.zip,smtiles.zip,tiles.zip,weapon.zip,weapon2.zip

            最后一位为:16的偏移量是:0,对应资源文件:dnitems.zip,hair2_ball.zip,hum3_ball.zip,items.zip,magic10.zip,magic7.zip,magic8.zip,mon34.zip,stateitem.zip,ui1.zip

            最后一位为:0的偏移量是:0,后9位全是0,对应资源文件:mmap.zip,上一层目录的rs.zip
            */
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
                        Offset = offset,
                        Length = length,
                        X = BitConverter.ToInt16(otherParamArray, 0),
                        Y = BitConverter.ToInt16(otherParamArray, 2),
                        Width = BitConverter.ToInt16(otherParamArray, 4),
                        Height = BitConverter.ToInt16(otherParamArray, 6),
                        TypeCode = otherParamArray[8]
                    };
                    ChildFiles.Add(childFile);
                    //Console.WriteLine($"{offset}\t{length}");
                    //Console.WriteLine(childFile.ToString());
                }

            }

            if (ChildFiles.Count > 0)
                this.TypeCode = ChildFiles[0].TypeCode;
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
            this.ChildFiles.AsParallel().ForAll(file =>
           {
               using (var otherStream = CreateReadStream(sourceFile))
               {
                   file.SaveToFileAsync(dir, otherStream).Wait();
               }
           });
            this.WriteIniFile(GetIniPath(dir));
        }

        private void WriteIniFile(string iniPath)
        {
            File.WriteAllText(iniPath, $"类型={this.TypeCode}", Encoding.UTF8);
        }

        private string GetIniPath(string dir)
        {
            return Path.Combine(dir, "resource.description.ini");
        }
        private void ReadIniFile(string path)
        {
            this.TypeCode = byte.Parse(File.ReadAllLines(path, Encoding.UTF8)[0].Split('=')[1]);
        }

        public async Task PackageAsync(string dir)
        {
            string iniPath = GetIniPath(dir);
            if (!File.Exists(iniPath))
                throw new FileNotFoundException("打包的文件夹必须是使用本软件解包的资源!");
            ReadIniFile(iniPath);
            this.ChildFiles = new List<ChildFile>();
            //AsyncBinaryWriter Dispose时会关闭传入的流
            using (var stream = File.Create(dir + ".zip"))
            {
                //写头
                await stream.WriteAsync("www.sdo.com").ConfigureAwait(false);
                await stream.WriteAsync(byte.MinValue).ConfigureAwait(false);
                //写文件列表读取偏移量,先占位,后面写完后再修改
                await stream.WriteAsync(0).ConfigureAwait(false);
                foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    if (IsIniFile(file))
                        continue;
                    var childFile = new ChildFile()
                    {
                        FileName = file.Replace(dir + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/'),
                        TypeCode = this.TypeCode,
                        Offset = (int)stream.Length
                    };
                    //通过读取ini文件加载坐标
                    childFile.ReadIni(file);
                    using (var childStream = CreateReadStream(file))
                    {
                        childFile.Length = (int)childStream.Length + (this.TypeCode == 8 ? 17 : 0);
                        //写入流
                        await stream.WriteAsync(await childStream.ReadBytesAsync((int)childStream.Length).ConfigureAwait(false)).ConfigureAwait(false);

                        //加载长宽
                        if (this.TypeCode != 0)
                        {
                            using (Image image = Image.FromStream(childStream))
                            {
                                childFile.Width = (short)image.Width;
                                childFile.Height = (short)image.Height;
                            }
                        }
                    }
                    this.ChildFiles.Add(childFile);
                }
                //从此地址开始写文件列表
                int fileListoffset = (int)stream.Length;
                //开始文件列表地址偏移量
                stream.Seek(12, SeekOrigin.Begin);
                //覆盖最早写的int值(4字节)
                await stream.WriteAsync(fileListoffset).ConfigureAwait(false);
                //开始写文件列表
                stream.Seek(0, SeekOrigin.End);
                //写文件数量
                await stream.WriteAsync(this.ChildFiles.Count).ConfigureAwait(false);
                foreach (var childFile in this.ChildFiles)
                {
                    await childFile.WriteFileInfoAsync(stream).ConfigureAwait(false);
                }
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }


        private bool IsIniFile(string path)
        {
            return path.EndsWith(".description.ini", StringComparison.OrdinalIgnoreCase);
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
            string header = Encoding.UTF8.GetString(array, offset, readLength);
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
        internal string FileName { get; set; }

        public short X { get; set; }
        public short Y { get; set; }
        internal short Width { get; set; }
        internal short Height { get; set; }
        internal byte TypeCode { get; set; }

        /// <summary>
        /// 偏移量
        /// </summary>
        internal int Offset { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        internal int Length { get; set; }

        internal async Task SaveToFileAsync(string dir, Stream stream)
        {
            var data = await ReadFromStreamAsync(stream).ConfigureAwait(false);
            await SaveFileAsync(dir, data).ConfigureAwait(false);
        }

        internal async Task WriteFileInfoAsync(Stream stream)
        {
            //文件名长度
            await stream.WriteAsync(this.FileName.Length).ConfigureAwait(false);
            //文件名                
            await stream.WriteAsync(this.FileName).ConfigureAwait(false);
            //文件长度              
            await stream.WriteAsync(this.Length).ConfigureAwait(false);
            //文件地址              
            await stream.WriteAsync(this.Offset).ConfigureAwait(false);
            //x                     
            await stream.WriteAsync(this.X).ConfigureAwait(false);
            //y                     
            await stream.WriteAsync(this.Y).ConfigureAwait(false);
            //with                  
            await stream.WriteAsync(this.Width).ConfigureAwait(false);
            //height                
            await stream.WriteAsync(this.Height).ConfigureAwait(false);
            //typecode              
            await stream.WriteAsync(this.TypeCode).ConfigureAwait(false);
        }

        private async Task<byte[]> ReadFromStreamAsync(Stream stream)
        {
            var realLength = this.Length;
            if (this.TypeCode == 8)
                realLength -= 17;
            var buffer = new byte[realLength];
            stream.Seek(this.Offset, SeekOrigin.Begin);
            await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            return buffer;
        }

        private async Task SaveFileAsync(string dir, byte[] data)
        {
            string path = Path.Combine(dir, this.FileName);
            // 这里的路径分隔符是linux系统的,因为在安卓上运行
            if (this.FileName.IndexOf('/') != -1)
            {
                string childDir = Path.GetDirectoryName(path);
                if (!Directory.Exists(childDir))
                    Directory.CreateDirectory(childDir);
            }
            using (var stream = File.Create(path))
            {
                await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            }

            WriteFileInfo(path);
        }

        public override string ToString()
        {
            return ToIniFormat();
        }

        private string ToIniFormat()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"X坐标={this.X}");
            sb.AppendLine($"Y坐标={this.Y}");
            return sb.ToString();
        }

        private void FromIni(string iniPath)
        {
            if (!File.Exists(iniPath))
                return;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (var fileLine in File.ReadLines(iniPath, Encoding.UTF8))
            {
                var temp = fileLine.Split('=');
                dictionary.Add(temp[0], temp[1]);
            }
            this.X = short.Parse(dictionary["X坐标"]);
            this.Y = short.Parse(dictionary["Y坐标"]);
        }

        internal void ReadIni(string sourcePath)
        {
            FromIni(GetIniPath(sourcePath));
        }

        private void WriteFileInfo(string path)
        {
            //最后一位为:0的偏移量是:0,后9位全是0,对应资源文件:mmap.zip,上一层目录的rs.zip
            //TypeCode==0的是rs.zip与mmap.zip,观测发现,后面9个字节全是0,所以没有必要写入,打开时也没有必要读取
            if (this.TypeCode == 0)
            {
                return;
            }

            File.WriteAllText(GetIniPath(path), this.ToIniFormat(), Encoding.UTF8);
        }

        private string GetIniPath(string filePath)
        {
            return filePath + ".description.ini";
        }
    }

}
