using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace KeyboardExtension
{
    public class ExtraView : UIView
    {
        GlobalColors _globalColors;
        public bool _darkMode;
        public bool _solidColorMode;

        public ExtraView(GlobalColors globalColors, bool darkMode, bool solidColorMode) : 
            base (frame: CGRect.Empty)
        {
            this._globalColors = globalColors;
            this._darkMode = darkMode;
            this._solidColorMode = solidColorMode;
    
                
        }

        public ExtraView(NSCoder aDecoder) :
            base(coder: aDecoder)
        {
            this._globalColors = null;
            this._darkMode = false;
            this._solidColorMode = false;
        }
    }
}
