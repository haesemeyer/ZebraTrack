/*
Copyright 2011 Drew Robson
   Licensed under the MIT License, see License.txt.
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
*/

using System;
using System.IO;
using MHApi.Threading;

namespace MHApi.DrewsClasses {
    public class Saver{

        public static string BasePath
        {
#if laser
            get { return "e:\\LaserTrack\\Test";}
#else
            get { return "e:\\24WellTrainer\\"; }
#endif
        }

        public string BaseName { get; set; }
        public string SavePath { get; set; }

        public Saver(string path, string baseName) {
            SavePath = Path.IsPathRooted(path) ? path : Path.Combine(BasePath, path);
            BaseName = baseName;
        }

        public Saver(string path, string baseName, bool appendDateFolder) {
            var basePath = Path.IsPathRooted(path) ? path : Path.Combine(BasePath, path);
            var now = DateTime.Now;
            var dateFolder = (now.Year % 100).ToString("D2") + now.Month.ToString("D2") + now.Day.ToString("D2");
            SavePath = Path.Combine(basePath, dateFolder);
            SavePath = Path.Combine(SavePath, baseName);
            BaseName = baseName;
        }

        void CheckPath() {
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
        }

        public FileStream GetFileStream(string extension) {
            return GetFileStream("", extension, true);
        }

        public FileStream GetFileStream(string suffix, string extension) {
            return GetFileStream(suffix, extension, true);
        }

        public FileStream GetFileStream(string suffix, string extension, bool renameIfExists) {
            CheckPath();
            if (extension[0] != '.')
                extension = "." + extension;
            var fullPath = Path.Combine(SavePath, BaseName + suffix + extension);
            if (File.Exists(fullPath))
                if (renameIfExists)
                    return GetFileStream(suffix + "_", extension, true);
                else
                    throw new IOException("File " + fullPath + " already exists");
            return new FileStream(fullPath, FileMode.CreateNew);
        }

        public StreamWriter GetStreamWriter(string extension) {
            return GetStreamWriter("", extension, true);
        }

        public StreamWriter GetStreamWriter(string suffix, string extension) {
            return GetStreamWriter(suffix, extension, true);
        }

        public StreamWriter GetStreamWriter(string suffix, string extension, bool renameIfExists) {
            CheckPath();
            if (extension[0] != '.')
                extension = "." + extension;
            var fullPath = Path.Combine(SavePath, BaseName + suffix + extension);
            if (File.Exists(fullPath))
                if (renameIfExists)
                    return GetStreamWriter(suffix + "_", extension, true);
                else
                    throw new IOException("File " + fullPath + " already exists");
            return new StreamWriter(fullPath);
        }

        public AsyncTiffWriter GetTiffWriter(string suffix, bool renameIfExists) {
            CheckPath();
            var fullPath = Path.Combine(SavePath, BaseName + suffix + ".tif");
            if (File.Exists(fullPath))
                if (renameIfExists)
                    return GetTiffWriter(suffix + "_", true);
                else
                    throw new IOException("File " + fullPath + " already exists");
            return new AsyncTiffWriter(fullPath);
        }

    }
}
