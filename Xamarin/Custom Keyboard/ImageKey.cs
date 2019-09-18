using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace KeyboardExtension
{
    public class ImageKey : KeyboardKey
    {
        public ImageKey() :
            base(VibrancyType.DarkSpecial)
        {

        }
        UIImageView _image;
        public UIImageView image
        {
            get
            {
                return _image;
            }
            set
            {

                if (_image != null)
                {
                    _image.RemoveFromSuperview();
                }
                _image = value;

                if (value != null)
                { 
                    var imageView = value;
                    this.AddSubview(imageView);
                    imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
                    this.redrawImage();
                    updateColors();
                }
            }
        }

    public override void updateColors()
    {
        base.updateColors();


        var switchColors = this.Highlighted || this.Selected;


        if (switchColors)
        {
            var downTextColor = this.downTextColor;
            if (image != null)
            { 
                if (downTextColor != null)
                {
                    this.image.TintColor = downTextColor;
                }
                else {
                    this.image.TintColor = this.textColor;
                }
            }
        }
        else
            {
                if (image != null)
                {
                    this.image.TintColor = this.textColor;
                }
        }
    }

    public override void refreshShapes()
    {
        base.refreshShapes();
        this.redrawImage();
    }

        void redrawImage()
        {
            var image = this.image;
            if (image != null)
            {

                var imageSize = new CGSize(20, 20);
                var imageOrigin = new CGPoint(
                (this.Bounds.Width - imageSize.Width) / (nfloat)(2),
                (this.Bounds.Height - imageSize.Height) / (nfloat)(2));
                var imageFrame = CGRect.Empty;
                imageFrame.Location = imageOrigin;
                imageFrame.Size = imageSize;


                image.Frame = imageFrame;
             }
        }
   
    }
}