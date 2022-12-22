using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MHApi.DrewsClasses;
using ipp;

namespace MHApi.Threading
{
    public unsafe class AsyncTiffWriter : IDisposable
    {
        private TiffWriter _tiffWriter;

        private ProducerConsumer<Image8> _imageQueue;

        private AutoResetEvent _taskStop;

        private Task _writeTask;

        public AsyncTiffWriter(string fileName)
        {
            _tiffWriter = new TiffWriter(fileName);
            _imageQueue = new ProducerConsumer<Image8>(4000);
            _taskStop = new AutoResetEvent(false);
            _writeTask = new Task(TaskRun);
            _writeTask.Start();
        }

        private void TaskRun()
        {
            Image8 nextImage;
            while (true)
            {
                try
                {
                    nextImage = _imageQueue.Consume(_taskStop);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("Image writing canceled");
                    return;
                }
                if (nextImage != null)
                {
                    _tiffWriter.WriteFrame(nextImage);
                    nextImage.Dispose();
                    nextImage = null;
                }
            }
        }

        public void WriteFrame(Image8 image)
        {
            //Create new image buffer and copy over image, then produce on ProducerConsumer
            Image8 copy = new Image8(image.Size);
            ip.ippiCopy_8u_C1R(image.Image, image.Stride, copy.Image, copy.Stride, image.Size);
            _imageQueue.Produce(copy);
            //Note: Consumer task will free buffer!!!
        }

        public bool IsDisposed { get; private set; }
        public void Dispose()
        {
            if (IsDisposed)
                return;
            //Signal to our task to stop
            _taskStop.Set();
            //Wait on task and then dispose task and tiff writer
            if (!_writeTask.Wait(5000))
                System.Diagnostics.Debug.WriteLine("Timed out waiting for write task to finish");
            _writeTask.Dispose();
            _tiffWriter.Dispose();
            _taskStop.Dispose();
            IsDisposed = true;
        }
    }
}
