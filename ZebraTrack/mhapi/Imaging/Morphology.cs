using System;
using System.Runtime.InteropServices;
using MHApi.DrewsClasses;
using ipp;

namespace MHApi.Imaging
{
    public unsafe class Morphology : IDisposable
    {

        /// <summary>
        /// The mask image used in morphology operations
        /// </summary>
        private Image8 _mask;

        /// <summary>
        /// The maximal size of images processed by the object
        /// </summary>
        private IppiSize _maxSize;

        /// <summary>
        /// Internal morphology buffer for dilation/erosion
        /// </summary>
        private byte* _morphBuffer;

        /// <summary>
        /// Internal state buffer for dilation/erosion
        /// </summary>
        private IppiMorphState* _morphState;

        /// <summary>
        /// Internal morphology buffer for closing/opening
        /// </summary>
        private byte* _advMorphBuffer;

        /// <summary>
        /// Internal state buffer for closing/opening
        /// </summary>
        private IppiMorphAdvState* _advMorphState;

        /// <summary>
        /// The border behavior of morphological operations
        /// </summary>
        private IppiBorderType _bType;

        /// <summary>
        /// The value to assign if constant border type
        /// </summary>
        private byte _bVal;

        /// <summary>
        /// Indicates if the class has been disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The width of our mask
        /// </summary>
        public int MaskWidth
        {
            get
            {
                return _mask.Width;
            }
        }

        /// <summary>
        /// The height of our mask
        /// </summary>
        public int MaskHeight
        {
            get
            {
                return _mask.Height;
            }
        }

        /// <summary>
        /// Creates a new Morphology object
        /// </summary>
        /// <param name="mask">The mask to use by this object - note object will dispose mask!</param>
        /// <param name="maxImageSize">The maximal size of the images this class operates on</param>
        /// <param name="borderType">The border type for morphological operations</param>
        protected Morphology(Image8 mask, IppiSize maxImageSize, IppiBorderType borderType)
        {
            if (mask == null)
                throw new ArgumentNullException("mask", "Supplied mask can't be null");
            if (mask.Width > maxImageSize.width || mask.Height > maxImageSize.height)
                throw new ArgumentException("The size of the mask cannot be larger than the processed image size");
            if (maxImageSize.width <= 0 || maxImageSize.height <= 0)
                throw new ArgumentOutOfRangeException("Image size parameters must be larger than 0");
            if (borderType != IppiBorderType.ippBorderRepl && borderType != IppiBorderType.ippBorderInMem)
                throw new ArgumentException("Border type has to be one of ippBorderRepl or ippBorderInMem", "borderType");
            _mask = mask;
            _maxSize = new IppiSize(maxImageSize.width, maxImageSize.height);
            _bType = borderType;
            _bVal = 0; //Since constant border anyway not allowed
            // allocate buffers and initialize morph specs
            int ms_size =0, mb_size=0;
            IppHelper.IppCheckCall(cv.ippiMorphologyBorderGetSize_8u_C1R(_maxSize, _mask.Size, &ms_size, &mb_size));
            _morphBuffer = (byte*)Marshal.AllocHGlobal(mb_size);
            _morphState = (IppiMorphState*)Marshal.AllocHGlobal(ms_size);
            IppHelper.IppCheckCall(cv.ippiMorphologyBorderInit_8u_C1R(_maxSize, _mask.Image,
                _mask.Size, _morphState, _morphBuffer));
            IppHelper.IppCheckCall(cv.ippiMorphAdvGetSize_8u_C1R(_maxSize, _mask.Size, &ms_size, &mb_size));
            _advMorphBuffer = (byte*)Marshal.AllocHGlobal(mb_size);
            _advMorphState = (IppiMorphAdvState*)Marshal.AllocHGlobal(ms_size);
            IppHelper.IppCheckCall(cv.ippiMorphAdvInit_8u_C1R(_maxSize, _mask.Image,
                _mask.Size, _advMorphState, _advMorphBuffer));
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            if (_mask != null)
            {
                _mask.Dispose();
                _mask = null;
            }
            if(_morphBuffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)_morphBuffer);
                _morphBuffer = null;
            }
            if(_morphState != null)
            {
                Marshal.FreeHGlobal((IntPtr)_morphState);
                _morphState = null;
            }
            if(_advMorphState != null)
            {
                Marshal.FreeHGlobal((IntPtr)_advMorphState);
                _advMorphState = null;
            }
            if(_advMorphBuffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)_advMorphBuffer);
                _advMorphBuffer = null;
            }
            IsDisposed = true;
        }

        ~Morphology()
        {
            if (!IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine("Morphology object not properly disposed.");
                Dispose();
            }
        }

        /// <summary>
        /// Ensures that this object can operate on images of the requested size
        /// </summary>
        /// <param name="size">The roi size that should be processed</param>
        /// <exception cref="ArgumentException">If size is larger than maximal size</exception>
        public void CheckSizeValid(IppiSize size)
        {
            if (size.width > _maxSize.width || size.height > _maxSize.height)
                throw new ArgumentException("ROI size too large for this object. Create object with larger max size");
        }

        /// <summary>
        /// Performs dilation operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="roi">The roi to process in the image</param>
        public void Dilate(Image8 imSrc, Image8 imDst, IppiROI roi)
        {
            CheckSizeValid(roi.Size);
            IppHelper.IppCheckCall(cv.ippiDilateBorder_8u_C1R(imSrc[roi.TopLeft], imSrc.Stride,
                imDst[roi.TopLeft], imDst.Stride, roi.Size, _bType, _bVal, _morphState, _morphBuffer));
        }

        /// <summary>
        /// Performs dilation operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="size">The regionsize to process in the image</param>
        public void Dilate(Image8 imSrc, Image8 imDst, IppiSize size)
        {
            CheckSizeValid(size);
            IppHelper.IppCheckCall(cv.ippiDilateBorder_8u_C1R(imSrc.Image, imSrc.Stride, imDst.Image, imDst.Stride,
                size, _bType, _bVal, _morphState, _morphBuffer));
        }

        /// <summary>
        /// Performs erosion operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="roi">The roi to process in the image</param>
        public void Erode(Image8 imSrc, Image8 imDst, IppiROI roi)
        {
            CheckSizeValid(roi.Size);
            IppHelper.IppCheckCall(cv.ippiErodeBorder_8u_C1R(imSrc[roi.TopLeft], imSrc.Stride, imDst[roi.TopLeft],
                imDst.Stride, roi.Size, _bType, _bVal, _morphState, _morphBuffer));
        }

        /// <summary>
        /// Performs erosion operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="size">The regionsize to process in the image</param>
        public void Erode(Image8 imSrc, Image8 imDst, IppiSize size)
        {
            CheckSizeValid(size);
            IppHelper.IppCheckCall(cv.ippiErodeBorder_8u_C1R(imSrc.Image, imSrc.Stride, imDst.Image, imDst.Stride,
                size, _bType, _bVal, _morphState, _morphBuffer));
        }

        /// <summary>
        /// Performs opening operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="roi">The roi to process in the image</param>
        public void Open(Image8 imSrc, Image8 imDst, IppiROI roi)
        {
            CheckSizeValid(roi.Size);
            IppHelper.IppCheckCall(cv.ippiMorphOpenBorder_8u_C1R(imSrc[roi.TopLeft], imSrc.Stride,
                imDst[roi.TopLeft], imDst.Stride, roi.Size, _bType, _bVal, _advMorphState, _advMorphBuffer));
        }

        /// <summary>
        /// Performs opening operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="size">The roi size to process in the image</param>
        public void Open(Image8 imSrc, Image8 imDst, IppiSize size)
        {
            CheckSizeValid(size);
            IppHelper.IppCheckCall(cv.ippiMorphOpenBorder_8u_C1R(imSrc.Image, imSrc.Stride, imDst.Image,
                imDst.Stride, size, _bType, _bVal, _advMorphState, _advMorphBuffer));
        }

        /// <summary>
        /// Performs closing operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="roi">The roi to process in the image</param>
        public void Close(Image8 imSrc, Image8 imDst, IppiROI roi)
        {
            CheckSizeValid(roi.Size);
            IppHelper.IppCheckCall(cv.ippiMorphCloseBorder_8u_C1R(imSrc[roi.TopLeft], imSrc.Stride,
                imDst[roi.TopLeft], imDst.Stride, roi.Size, _bType, _bVal, _advMorphState, _advMorphBuffer));
        }

        /// <summary>
        /// Performs closing operation with the underlying mask
        /// </summary>
        /// <param name="imSrc">The source image</param>
        /// <param name="imDst">The destination image</param>
        /// <param name="size">The roi size to process in the image</param>
        public void Close(Image8 imSrc, Image8 imDst, IppiSize size)
        {
            CheckSizeValid(size);
            IppHelper.IppCheckCall(cv.ippiMorphCloseBorder_8u_C1R(imSrc.Image, imSrc.Stride, imDst.Image,
                imDst.Stride, size, _bType, _bVal, _advMorphState, _advMorphBuffer));
        }

        /// <summary>
        /// Generates a Morphology object with circular mask of the specified radius
        /// </summary>
        /// <param name="radius">The radius of the mask as maximum distance from center pixel (i.e. radius=0 would be 1-pixel mask)</param>
        /// <param name="maxRoiSize">The maximum ROI size to process by this morphology instance</param>
        /// <returns>Morphology object using this mask</returns>
        public static Morphology GenerateDiskMask(int radius, IppiSize maxRoiSize)
        {
            if (radius < 1)
                throw new ArgumentOutOfRangeException("radius");
            //Masks in ipps morphological functions don't really allow for stride
            //unless it is guaranteed, that the pixels within the stride are of value0
            //therefore we generate the mask with the minimum possible width and height
            //that is dividable by 4
            int diameter = 1 + 2 * radius;//center pixel is extra
            int imageWidth = (int)(Math.Ceiling(diameter / 4.0) * 4);
            IppiPoint center = new IppiPoint(radius, radius);
            Image8 mask = new Image8(imageWidth, diameter);
            //Set all pixels to 0
            IppHelper.IppCheckCall(ip.ippiSet_8u_C1R(0, mask.Image, mask.Stride, mask.Size));
            //Loop over pixels, check distance and set to 1 if within circle
            for (int x = 0; x < mask.Width; x++)
                for (int y = 0; y < mask.Width; y++)
                {
                    if (Distance.Euclidian(new IppiPoint(x, y), center) <= radius)
                        *mask[x, y] = 1;
                }
            //default behavior replicate border
            return new Morphology(mask, maxRoiSize, IppiBorderType.ippBorderRepl);
        }

        /// <summary>
        /// Generates a morphology object with rectangular mask
        /// </summary>
        /// <param name="width">The width of the rectangle</param>
        /// <param name="height">The height of the rectangle</param>
        /// <param name="maxRoiSize">The maximum ROI size to process by this morphology instance</param>
        /// <returns>Morphology object using this mask</returns>
        public static Morphology GenerateRectMask(int width, int height, IppiSize maxRoiSize)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException("width");
            if (height < 1)
                throw new ArgumentOutOfRangeException("height");
            int imageWidth = (int)(Math.Ceiling(width / 4.0) * 4);
            IppiPoint center = new IppiPoint((int)Math.Floor(width / 4.0), (int)Math.Floor(height / 4.0));
            Image8 mask = new Image8(imageWidth, height);
            //Set all pixels to zero
            IppHelper.IppCheckCall(ip.ippiSet_8u_C1R(0, mask.Image, mask.Stride, mask.Size));
            //Set rectangle to 1
            IppHelper.IppCheckCall(ip.ippiSet_8u_C1R(1, mask.Image, mask.Stride, new IppiSize(width, height)));
            return new Morphology(mask, maxRoiSize, IppiBorderType.ippBorderRepl);
        }

        /// <summary>
        /// Generates a morphology object with square 3x3 mask
        /// </summary>
        /// <param name="maxRoiSize">The maximum ROI size to process by this morphology instance</param>
        /// <returns>Morphology object using this mask</returns>
        public static Morphology Generate3x3Mask(IppiSize maxRoiSize)
        {
            return GenerateRectMask(3, 3, maxRoiSize);
        }

        /// <summary>
        /// Generates a morphology object with specified mask and border behavior
        /// </summary>
        /// <param name="mask">The mask to use for the operation</param>
        /// <param name="maxRoiSize">The maximum roi size processed by this object</param>
        /// <param name="borderType">The border type. Only InMem and Repl are supported</param>
        /// <returns></returns>
        public static Morphology GenerateMorphology(Image8 mask, IppiSize maxRoiSize, IppiBorderType borderType)
        {
            if (borderType != IppiBorderType.ippBorderRepl && borderType != IppiBorderType.ippBorderInMem)
                throw new ArgumentException("Border type has to be one of ippBorderRepl or ippBorderInMem",
                    "borderType");
            return new Morphology(mask, maxRoiSize, borderType);
        }
    }
}
