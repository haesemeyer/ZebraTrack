/*
Copyright 2016 Martin Haesemeyer
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
using ipp;
using MHApi.DrewsClasses;


namespace MHApi.Imaging
{
    /// <summary>
    /// Abstract base class for background model builders
    /// </summary>
    public unsafe abstract class BackgroundModel : IDisposable
    {
        /// <summary>
        /// The current background image representation
        /// </summary>
        protected Image32F background;

        /// <summary>
        /// 8bit representation of the current background
        /// </summary>
        Image8 bgImage;

        /// <summary>
        /// The width of the background image
        /// </summary>
        protected int width;

        /// <summary>
        /// The height of the background image
        /// </summary>
        protected int height;

        

        /// <summary>
        /// Readonly property to get the (current) background image
        /// </summary>
        public Image8 Background
        {
            get
            {
                if (bgImage == null)
                {
                    bgImage = new Image8(background.Width,background.Height);
                }
                background.ReduceTo8U(bgImage);
                return bgImage;
            }
        }

        /// <summary>
        /// Default constructor initializing the background
        /// </summary>
        /// <param name="im">Initial background image</param>
        protected BackgroundModel(Image8 im)
        {
            //we allow null to be passed in the constructor to give derived classes the ability
            //to initialize the background
            if (im == null)
                return;
            
            background = new Image32F(im);
            width = im.Width;
            height = im.Height;
            
        }

        /// <summary>
        /// This method updates the current background image. Implemented in derived classes.
        /// </summary>
        /// <param name="im">The image used to update the background</param>
        public abstract void UpdateBackground(Image8 im);

        #region IDisposable Members

        public bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (background != null)
                background.Dispose();
            if (bgImage != null)
                bgImage.Dispose();
        }

        public void Dispose() {
            if(IsDisposed)
                return;
            Dispose(true);
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }

        ~BackgroundModel() {
            Dispose();
        }

        #endregion
    }
}
