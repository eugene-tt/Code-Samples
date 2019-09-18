using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using CoreAnimation;
using CoreGraphics;
using System.Linq;
namespace KeyboardExtension
{
    using CGFloat = nfloat;

    class LayoutConstants : NSObject
    {
        public CGFloat landscapeRatio { get { return 2 ;}}
        // side edges increase on 6 in portrait
        public CGFloat[] sideEdgesPortraitArray { get {return new CGFloat[] { 3, 4 };}}
        public CGFloat[] sideEdgesPortraitWidthThreshholds { get {return new CGFloat[]{400};}}
        public CGFloat sideEdgesLandscape { get { return 3 ;}}
    
        // top edges decrease on various devices in portrait
        public CGFloat[] topEdgePortraitArray { get {return new CGFloat[]{12, 10, 8};}}
        public CGFloat[] topEdgePortraitWidthThreshholds { get {return new CGFloat[]{350, 400};}}
        public CGFloat topEdgeLandscape { get { return 6 ;}}
    
        // keyboard area shrinks in size in landscape on 6 and 6+
        public CGFloat[] keyboardShrunkSizeArray { get {return new CGFloat[]{522, 524};}}
        public CGFloat[] keyboardShrunkSizeWidthThreshholds { get {return new CGFloat[]{700};}}
        public CGFloat keyboardShrunkSizeBaseWidthThreshhold { get { return 600 ;}}
    
        // row gaps are weird on 6 in portrait
        public CGFloat[] rowGapPortraitArray { get {return new CGFloat[]{15, 11, 10};}}
        public CGFloat[] rowGapPortraitThreshholds { get {return new CGFloat[]{350, 400};}}
        public CGFloat rowGapPortraitLastRow { get { return 9 ;}}
        int rowGapPortraitLastRowIndex { get { return 1 ;}}
        public CGFloat rowGapLandscape { get { return 7 ;}}
    
        // key gaps have weird and inconsistent rules
        public CGFloat keyGapPortraitNormal { get { return 6 ;}}
        public CGFloat keyGapPortraitSmall { get { return 5 ;}}
        public CGFloat keyGapPortraitNormalThreshhold { get { return 350 ;}}
        public CGFloat keyGapPortraitUncompressThreshhold { get { return 350 ;}}
        public CGFloat keyGapLandscapeNormal { get { return 6 ;}}
        public CGFloat keyGapLandscapeSmall { get { return 5 ;}}
        // TODO: 5.5 row gap on 5L
        // TODO: wider row gap on 6L
        public int keyCompressedThreshhold { get { return 11 ;}}
    
        // rows with two special keys on the side and characters in the middle (usually 3rd row)
        // TODO: these are not pixel-perfect, but should be correct within a few pixels
        // TODO: are there any "hidden constants" that would allow us to get rid of the multiplier? see: popup dimensions
        public CGFloat flexibleEndRowTotalWidthToKeyWidthMPortrait { get { return 1 ;}}
        public CGFloat flexibleEndRowTotalWidthToKeyWidthCPortrait { get { return -14 ;}}
        public CGFloat flexibleEndRowTotalWidthToKeyWidthMLandscape { get { return 0.9231f ;}}
        public CGFloat flexibleEndRowTotalWidthToKeyWidthCLandscape { get { return -9.4615f ;}}
        public CGFloat flexibleEndRowMinimumStandardCharacterWidth { get { return 7 ;}}
    
        public CGFloat lastRowKeyGapPortrait { get { return 6 ;}}
        public CGFloat[] lastRowKeyGapLandscapeArray { get {return new CGFloat[]{8, 7, 5};}}
        public CGFloat[] lastRowKeyGapLandscapeWidthThreshholds { get {return new CGFloat[]{500, 700};}}
    
        // TODO: approxmiate, but close enough
        public CGFloat lastRowPortraitFirstTwoButtonAreaWidthToKeyboardAreaWidth { get { return 0.24f ;}}
        public CGFloat lastRowLandscapeFirstTwoButtonAreaWidthToKeyboardAreaWidth { get { return 0.19f ;}}
        public CGFloat lastRowPortraitLastButtonAreaWidthToKeyboardAreaWidth { get { return 0.24f ;}}
        public CGFloat lastRowLandscapeLastButtonAreaWidthToKeyboardAreaWidth { get { return 0.19f ;}}
        public CGFloat micButtonPortraitWidthRatioToOtherSpecialButtons { get { return 0.765f ;}}
    
        // TODO: not exactly precise
        public CGFloat popupGap { get { return 8 ;}}
        public CGFloat popupWidthIncrement { get { return 26 ;}}
        public CGFloat[] popupTotalHeightArray { get {return new CGFloat[]{102, 108};}}
        public CGFloat[] popupTotalHeightDeviceWidthThreshholds { get {return new CGFloat[]{350};}}

        public CGFloat findThreshhold(CGFloat [] elements, CGFloat[] threshholds, CGFloat measurement)
        {
            //assert(elements.Count == threshholds.Count + 1, "elements and threshholds do not match")
            return elements[this.findThreshholdIndex(threshholds, measurement: measurement)];
        }
        int findThreshholdIndex(CGFloat[] threshholds, CGFloat measurement)
        {
            int i = 0;
            foreach ( var threshhold in threshholds.Reverse())
            {
                if((measurement >= threshhold)
                ){
                    var actualIndex = threshholds.Length - i;
                    return actualIndex;
                }
                i++;
            }
            return 0;
        }
        public CGFloat sideEdgesPortrait(CGFloat width) 
        {
            return this.findThreshhold(this.sideEdgesPortraitArray, threshholds: this.sideEdgesPortraitWidthThreshholds, measurement: width);
        }
        public CGFloat topEdgePortrait(CGFloat width)
        {
            return this.findThreshhold(this.topEdgePortraitArray, threshholds: this.topEdgePortraitWidthThreshholds, measurement: width);
        }
        public CGFloat rowGapPortrait(CGFloat width)
        {
            return this.findThreshhold(this.rowGapPortraitArray, threshholds: this.rowGapPortraitThreshholds, measurement: width);
        }
        public CGFloat rowGapPortraitLastRow_(CGFloat width)
        {
            var index = this.findThreshholdIndex(this.rowGapPortraitThreshholds, measurement: width);
            if(index == this.rowGapPortraitLastRowIndex)
            {
                return this.rowGapPortraitLastRow;
            }
            else
            {
                return this.rowGapPortraitArray[index];
            }
        }
        public CGFloat keyGapPortrait(CGFloat width, int rowCharacterCount)
        {
            var compressed = (rowCharacterCount >= this.keyCompressedThreshhold);
            if(compressed)
            {
                if(width >= this.keyGapPortraitUncompressThreshhold){
                    return this.keyGapPortraitNormal;
                }
                else
                {
                    return this.keyGapPortraitSmall;
                }
            }
            else
            {
                return this.keyGapPortraitNormal;
            }
        }
        public CGFloat keyGapLandscape(CGFloat width, int rowCharacterCount)
        {
            var compressed = (rowCharacterCount >= this.keyCompressedThreshhold);
            var shrunk = this.keyboardIsShrunk(width);
            if(compressed || shrunk)
            {
                return this.keyGapLandscapeSmall;
            }
            else
            {
                return this.keyGapLandscapeNormal;
            }
        }
        public CGFloat lastRowKeyGapLandscape(CGFloat width)
        {
            return this.findThreshhold(this.lastRowKeyGapLandscapeArray, threshholds: this.lastRowKeyGapLandscapeWidthThreshholds, measurement: width);
        }
    
        bool keyboardIsShrunk(CGFloat width)
        {
            var isPad = UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
            return (isPad ? false : width >= this.keyboardShrunkSizeBaseWidthThreshhold);
        }
        public CGFloat keyboardShrunkSize(CGFloat width)
        {
            var isPad = UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
            if (isPad)
            {
                return width;
            }
        
            if(width >= this.keyboardShrunkSizeBaseWidthThreshhold)
            {
                return this.findThreshhold(this.keyboardShrunkSizeArray, threshholds: this.keyboardShrunkSizeWidthThreshholds, measurement: width);
            }
            else
            {
                return width;
            }
        }
        public CGFloat popupTotalHeight(CGFloat deviceWidth)
        {
            return this.findThreshhold(this.popupTotalHeightArray, threshholds: this.popupTotalHeightDeviceWidthThreshholds, measurement: deviceWidth);
        }
    }

    public class GlobalColors : NSObject
    {
        public static UIColor lightModeRegularKey { get { return UIColor.White;; }}
        public static UIColor darkModeRegularKey { get { return UIColor.White.ColorWithAlpha((CGFloat)(0.3)); }}
        public static UIColor darkModeSolidColorRegularKey { get { return new UIColor(red: (CGFloat)(83)/(CGFloat)(255), green: (CGFloat)(83)/(CGFloat)(255), blue: (CGFloat)(83)/(CGFloat)(255), alpha: 1); }}
        public static UIColor lightModeSpecialKey { get { return GlobalColors.lightModeSolidColorSpecialKey; }}
        public static UIColor lightModeSolidColorSpecialKey { get { return new UIColor(red: (CGFloat)(177)/(CGFloat)(255), green: (CGFloat)(177)/(CGFloat)(255), blue: (CGFloat)(177)/(CGFloat)(255), alpha: 1); }}
        public static UIColor darkModeSpecialKey { get { return UIColor.Gray.ColorWithAlpha((CGFloat)(0.3)); }}
        public static UIColor darkModeSolidColorSpecialKey { get { return new UIColor(red: (CGFloat)(45)/(CGFloat)(255), green: (CGFloat)(45)/(CGFloat)(255), blue: (CGFloat)(45)/(CGFloat)(255), alpha: 1); }}
        public static UIColor darkModeShiftKeyDown { get { return new UIColor(red: (CGFloat)(214)/(CGFloat)(255), green: (CGFloat)(220)/(CGFloat)(255), blue: (CGFloat)(208)/(CGFloat)(255), alpha: 1); }}
        public static UIColor lightModePopup { get { return GlobalColors.lightModeRegularKey; }}
        public static UIColor darkModePopup { get { return UIColor.Gray; }}
        public static UIColor darkModeSolidColorPopup { get { return GlobalColors.darkModeSolidColorRegularKey; }}
            
        public static UIColor lightModeUnderColor { get { return UIColor.FromHSBA(hue: (CGFloat)(220/360.0), saturation: (CGFloat)0.04, brightness: (CGFloat)0.56, alpha: 1); }}
        public static UIColor darkModeUnderColor { get { return new UIColor(red: (CGFloat)(38.6)/(CGFloat)(255), green: (CGFloat)(18)/(CGFloat)(255), blue: (CGFloat)(39.3)/(CGFloat)(255), alpha: (CGFloat)0.4); }}
        public static UIColor lightModeTextColor { get { return UIColor.Black; }}
        public static UIColor darkModeTextColor { get { return UIColor.White; }}
        public static UIColor lightModeBorderColor { get { return UIColor.FromHSBA(hue: (CGFloat)(214/360.0), saturation: (CGFloat)0.04, brightness: (CGFloat)0.65, alpha: (CGFloat)1.0); }}
        public static UIColor darkModeBorderColor { get { return UIColor.Clear; }}
        public static UIColor keyboardBgColor { get { return UIColor.Clear; } }
        public static UIColor regularKey(bool darkMode, bool solidColorMode)
        {
            if(darkMode )
            {
                if(solidColorMode )
                {
                        return GlobalColors.darkModeSolidColorRegularKey;
                }
                else
                {
                        return GlobalColors.darkModeRegularKey;
                }
            }
            else
            {
                    return GlobalColors.lightModeRegularKey;
            }
        }
        public static UIColor popup(bool darkMode, bool solidColorMode)
        {
            if (darkMode)
            {
                if (solidColorMode)
                {
                    return GlobalColors.darkModeSolidColorPopup;
                }
                else
                {
                    return GlobalColors.darkModePopup;
                }
            }
            else
            {
                return GlobalColors.lightModePopup;
            }
        }
        public static UIColor specialKey(bool darkMode, bool solidColorMode)
        {
            if(darkMode ){
                if(solidColorMode ){
                        return GlobalColors.darkModeSolidColorSpecialKey;
                }
                else {
                        return GlobalColors.darkModeSpecialKey;
                }
            }
            else {
                if(solidColorMode ){
                        return GlobalColors.lightModeSolidColorSpecialKey;
                }
                else {
                        return GlobalColors.lightModeSpecialKey;
                }
            }
        }
    }

    //"darkShadowColor": UIColor(hue: (220/360.0), saturation: 0.04, brightness: 0.56, alpha: 1),
    //"blueColor": UIColor(hue: (211/360.0), saturation: 1.0, brightness: 1.0, alpha: 1),
    //"blueShadowColor": UIColor(hue: (216/360.0), saturation: 0.05, brightness: 0.43, alpha: 1),
    //extension CGRect: Hashable {
    //    public var hashValue: int {
    //        get {
    //            return (origin.x.hashValue ^ origin.y.hashValue ^ size.width.hashValue ^ size.Height.hashValue)
    //        }
    //    }
    //}
    //extension CGSize: Hashable {
    //    public var hashValue: int {
    //        get {
    //            return (width.hashValue ^ height.hashValue)
    //        }
    //    }
    //}
    // handles the layout for the keyboard, including key spacing and arrangement

    class KeyboardLayout : NSObject, KeyboardKeyProtocol
    {
        public bool shouldPoolKeys { get { return true; }}
        LayoutConstants layoutConstants;
        GlobalColors globalColors;
        Keyboard model;
        UIView superview;
        SafeDict<Key, KeyboardKey> modelToView = new SafeDict<Key, KeyboardKey>();
        public SafeDict<KeyboardKey, Key> viewToModel = new SafeDict<KeyboardKey, Key>();
        public List<KeyboardKey> keyPool = new List<KeyboardKey>();
        SafeDict<string, KeyboardKey> nonPooledMap = new SafeDict<string, KeyboardKey>();
        SafeDict<CGSize, List<KeyboardKey>> sizeToKeyMap = new SafeDict<CGSize, List<KeyboardKey>>();
        SafeDict<string, Shape> shapePool = new SafeDict<string, Shape>();
        public bool darkMode;
        public bool solidColorMode;
        public bool initialized;
        public KeyboardLayout(Keyboard model, UIView superview, LayoutConstants layoutConstants, GlobalColors globalColors, bool darkMode, bool solidColorMode)
        {
            this.layoutConstants = layoutConstants;
            this.globalColors = globalColors;

            this.initialized = false;
            this.model = model;
            this.superview = superview;

            this.darkMode = darkMode;
            this.solidColorMode = solidColorMode;
        }
        // TODO: remove this method;
        public void initialize()
        {
            //assert(!this.initialized, "already initialized");
            this.initialized = true;
        }
        public KeyboardKey viewForKey(Key model)
        {
            if (this.modelToView.ContainsKey(model))
            {
                return this.modelToView[model];
            }
            else
                return null;
        }
        public Key keyForView(KeyboardKey key)
        {
            if (this.viewToModel.ContainsKey(key))
            {
                return this.viewToModel[key];
            }
            else
                return null;
        }

        public virtual void setAppearanceForKey(KeyboardKey key, Key model, bool darkMode, bool solidColorMode)
        {
            if (model.type == Key.KeyType.Other)
            {
                this.setAppearanceForOtherKey(key, model: model, darkMode: darkMode, solidColorMode: solidColorMode);
            }
            switch (model.type)
            {
                case Key.KeyType.Character:
                case Key.KeyType.SpecialCharacter:
                case Key.KeyType.Period:
                    if (model.forceColor != null)
                    {
                        key.color = model.forceColor;

                        /*nfloat red, green, blue, alpha;
                        key.color.GetRGBA(out red, out green, out blue, out alpha);
                        var darkerColor = new UIColor(red: (nfloat)Math.Max(red - 0.2, 0.0),
                               green: (nfloat)Math.Max(green - 0.2, 0.0),
                                blue: (nfloat)Math.Max(blue - 0.2, 0.0),
                               alpha: alpha);*/
                        var darkerColor = key.color;
                        key.downColor = darkerColor;
                    }
                    else
                    {
                        key.color = GlobalColors.regularKey(darkMode, solidColorMode: solidColorMode);
                        if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
                        {
                            key.downColor = GlobalColors.specialKey(darkMode, solidColorMode: solidColorMode);
                        }
                        else
                        {
                            key.downColor = null;
                        }
                    }
                    key.textColor = (darkMode ? GlobalColors.darkModeTextColor : GlobalColors.lightModeTextColor);
                    key.downTextColor = null;
                    break;
                case Key.KeyType.Space:
                    key.color = GlobalColors.regularKey(darkMode, solidColorMode: solidColorMode);
                    key.downColor = GlobalColors.specialKey(darkMode, solidColorMode: solidColorMode);
                    key.textColor = (darkMode ? GlobalColors.darkModeTextColor : GlobalColors.lightModeTextColor);
                    key.downTextColor = null;
                    break;
                case Key.KeyType.Shift:
                    key.color = GlobalColors.specialKey(darkMode, solidColorMode: solidColorMode);
                    key.downColor = (darkMode ? GlobalColors.darkModeShiftKeyDown : GlobalColors.lightModeRegularKey);
                    key.textColor = GlobalColors.darkModeTextColor;
                    key.downTextColor = GlobalColors.lightModeTextColor;
                    break;
                case Key.KeyType.Backspace:
                    key.color = GlobalColors.specialKey(darkMode, solidColorMode: solidColorMode);

                    // TODO: actually a bit different;
                    key.downColor = GlobalColors.regularKey(darkMode, solidColorMode: solidColorMode);
                    key.textColor = GlobalColors.darkModeTextColor;
                    key.downTextColor = (darkMode ? null : GlobalColors.lightModeTextColor);
                    break;
                case Key.KeyType.ModeChange:
                    key.color = GlobalColors.specialKey(darkMode, solidColorMode: solidColorMode);
                    key.downColor = null;
                    key.textColor = (darkMode ? GlobalColors.darkModeTextColor : GlobalColors.lightModeTextColor);
                    key.downTextColor = null;
                    break;
                case Key.KeyType.Return:
                case Key.KeyType.Settings:
                    key.color = GlobalColors.specialKey(darkMode, solidColorMode: solidColorMode);
                    // TODO: actually a bit different;
                    key.downColor = GlobalColors.regularKey(darkMode, solidColorMode: solidColorMode);
                    key.textColor = (darkMode ? GlobalColors.darkModeTextColor : GlobalColors.lightModeTextColor);
                    key.downTextColor = null;
                    break;
                case Key.KeyType.KeyboardChange:
                    key.color = GlobalColors.specialKey(darkMode, solidColorMode: solidColorMode);
                    key.downColor = GlobalColors.regularKey(darkMode, solidColorMode: solidColorMode);
                    key.textColor = GlobalColors.darkModeTextColor;
                    key.downTextColor = (darkMode ? null : GlobalColors.lightModeTextColor);
                    break;
                default:
                    break;
            }

            if (model.forceColor != null)
            {
                key.popupColor = key.downColor;
            }
            else
            {
                key.popupColor = GlobalColors.popup(darkMode, solidColorMode: solidColorMode);
            }

            key.underColor = (this.darkMode ? GlobalColors.darkModeUnderColor : GlobalColors.lightModeUnderColor);
            key.borderColor = (this.darkMode ? GlobalColors.darkModeBorderColor : GlobalColors.lightModeBorderColor);
        }

        public virtual void setAppearanceForOtherKey(KeyboardKey key, Key model, bool darkMode, bool solidColorMode) { /* override this to handle special keys */ }

        CGFloat rounded(CGFloat measurement)
        {
            return (CGFloat)Math.Round(measurement * UIScreen.MainScreen.Scale) / UIScreen.MainScreen.Scale;
        }

        bool characterRowHeuristic(Key [] row)
        {
            return (row.Length >= 1 && row[0].isCharacter);
        }

        bool doubleSidedRowHeuristic(Key[] row)
        {
            return (row.Length >= 3 && !row[0].isCharacter && row[1].isCharacter);
        }


        public SafeDict<Key, CGRect> generateKeyFrames(Keyboard model, CGRect bounds, int pageToLayout)
        {
            if(bounds.Height == 0 || bounds.Width == 0 )
            {
                return null;
            }
        
            var keyMap = new SafeDict<Key, CGRect>();
            Func<bool> isLandscape = () =>
            {
                var boundsRatio = bounds.Width / bounds.Height;
                return (boundsRatio >= this.layoutConstants.landscapeRatio);
            };

        
            var sideEdges = (isLandscape() ? this.layoutConstants.sideEdgesLandscape : this.layoutConstants.sideEdgesPortrait(bounds.Width));
            var bottomEdge = sideEdges;
            var normalKeyboardSize = bounds.Width - (CGFloat)(2) * sideEdges;
            var shrunkKeyboardSize = this.layoutConstants.keyboardShrunkSize(normalKeyboardSize);


            sideEdges += ((normalKeyboardSize - shrunkKeyboardSize) / (CGFloat)(2));
        
            CGFloat topEdge = (isLandscape()? this.layoutConstants.topEdgeLandscape : this.layoutConstants.topEdgePortrait(bounds.Width));            
            CGFloat rowGap = (isLandscape()? this.layoutConstants.rowGapLandscape : this.layoutConstants.rowGapPortrait(bounds.Width));
            CGFloat lastRowGap = (isLandscape()? rowGap : this.layoutConstants.rowGapPortraitLastRow_(bounds.Width));

        
            //var flexibleEndRowM = (isLandscape() ? this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthMLandscape : this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthMPortrait);
            //var flexibleEndRowC = (isLandscape() ? this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthCLandscape : this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthCPortrait);
            var lastRowLeftSideRatio = (isLandscape() ? this.layoutConstants.lastRowLandscapeFirstTwoButtonAreaWidthToKeyboardAreaWidth : this.layoutConstants.lastRowPortraitFirstTwoButtonAreaWidthToKeyboardAreaWidth);
            var lastRowRightSideRatio = (isLandscape() ? this.layoutConstants.lastRowLandscapeLastButtonAreaWidthToKeyboardAreaWidth : this.layoutConstants.lastRowPortraitLastButtonAreaWidthToKeyboardAreaWidth);
            var lastRowKeyGap = (isLandscape() ? this.layoutConstants.lastRowKeyGapLandscape(bounds.Width) : this.layoutConstants.lastRowKeyGapPortrait);

            int p = 0;
            foreach( var page in model.pages)
            {
                if(p != pageToLayout )
                {
                    p++;
                    continue;
                }
            
                var numRows = page.rows.Count;

                int mostKeysInRow = 0;
                {
                    int currentMax = 0;
                    foreach(var row in page.rows)
                    {
                        currentMax = Math.Max(currentMax, row.Count);
                    }
                    mostKeysInRow = currentMax;
                };
            
                var rowGapTotal = (CGFloat)(numRows - 1 - 1) * rowGap + lastRowGap;

                CGFloat keyGap = (isLandscape()? this.layoutConstants.keyGapLandscape(bounds.Width, rowCharacterCount: mostKeysInRow) : this.layoutConstants.keyGapPortrait(bounds.Width, rowCharacterCount: mostKeysInRow));


                CGFloat keyHeight;
                {
                    var totalGaps = bottomEdge + topEdge + rowGapTotal;
                    var returnHeight = (bounds.Height - totalGaps) / (CGFloat)(numRows);
                    keyHeight =  this.rounded(returnHeight);
                };

                CGFloat letterKeyWidth = 0;
                {
                    var totalGaps = (sideEdges * (CGFloat)(2)) + (keyGap * (CGFloat)(mostKeysInRow - 1));
                    var returnWidth = (bounds.Width - totalGaps) / (CGFloat)(mostKeysInRow);
                    letterKeyWidth = this.rounded(returnWidth);
                };

                Func<Key[], CGRect[], SafeDict<Key, CGRect>, int> processRow = (row, frames, map) =>
                {
                    //assert(row.Count == frames.Count, "row and frames don't match");
                    int k = 0;
                    foreach (var kk in row)
                    {
                        map[kk] = frames[k];
                        k++;
                    }
                    return 0;
                };

                int r = 0;
                foreach(var row in page.rows)
                {
                    var rowGapCurrentTotal = (r == page.rows.Count - 1 ? rowGapTotal : (CGFloat)(r) * rowGap);

                    var frame = new CGRect(rounded(sideEdges), rounded(topEdge + ((CGFloat)(r) * keyHeight) + rowGapCurrentTotal), rounded(bounds.Width - (CGFloat)(2) * sideEdges), rounded(keyHeight));

                    var frames = new List<CGRect>();
                    
                    // basic character row: only typable characters;
                    if(this.characterRowHeuristic(row.ToArray()) )
                    {
                        frames = this.layoutCharacterRow(row.ToArray(), keyWidth: letterKeyWidth, gapWidth: keyGap, frame: frame);
                    }     
                    else 
                    if(this.doubleSidedRowHeuristic(row.ToArray()) )// character row with side buttons: shift, backspace, etc.
                    {
                        frames = this.layoutCharacterWithSidesRow(row.ToArray(), frame: frame, isLandscape: isLandscape(), keyWidth: letterKeyWidth, keyGap: keyGap);
                    }
                    else // bottom row with things like space, return, etc.
                    {
                        frames = this.layoutSpecialKeysRow(row.ToArray(), keyWidth: letterKeyWidth, gapWidth: lastRowKeyGap, 
                            leftSideRatio: lastRowLeftSideRatio, rightSideRatio: lastRowRightSideRatio, 
                            micButtonRatio: this.layoutConstants.micButtonPortraitWidthRatioToOtherSpecialButtons, 
                            isLandscape: isLandscape(), 
                            frame: frame);
                    }
                    processRow(row.ToArray(), frames.ToArray(), keyMap);
                    r++;
                }
            }        
            return keyMap;
        }

        //////////////////////////////////////////////
        // CALL THESE FOR LAYOUT/APPEARANCE CHANGES //
        //////////////////////////////////////////////    
        public void layoutKeys(int pageNum, bool uppercase, bool characterUppercase, ShiftState shiftState)
        {
            ////Console.WriteLine("layoutKeys 1");
            CATransaction.Begin();
            CATransaction.DisableActions = (true);

            ////Console.WriteLine("layoutKeys 2");
            // pre-allocate all keys if no cache;
            if (this.shouldPoolKeys )
            {
                ////Console.WriteLine("layoutKeys 3");
                if (this.keyPool.Count==0 )
                {
                    ////Console.WriteLine("layoutKeys 3a");
                    for (int p1=0; p1 < this.model.pages.Count; p1++)
                    {
                        this.positionKeys(p1);
                    }
                    ////Console.WriteLine("layoutKeys 3b");
                    this.updateKeyAppearance();

                    ////Console.WriteLine("layoutKeys 3c");
                    this.updateKeyCaps(true, uppercase: uppercase, characterUppercase: characterUppercase, shiftState: shiftState);
                }
                ////Console.WriteLine("layoutKeys 4");
            }

            ////Console.WriteLine("layoutKeys 5");
            this.positionKeys(pageNum);

            ////Console.WriteLine("layoutKeys 6");

            // reset state;

            int p = 0;
            foreach( var page in this.model.pages)
            {
                foreach (var row in page.rows)
                {
                    foreach (var key in row)
                    {
                        if(modelToView.ContainsKey(key))
                        {
                            var keyView = this.modelToView[key];
                            keyView.hidePopup();
                            keyView.Highlighted = false;
                            keyView.Hidden = (p != pageNum);
                        }
                    }
                }
                p++;
            }

            ////Console.WriteLine("layoutKeys 7");
            if (this.shouldPoolKeys)
            {

                ////Console.WriteLine("layoutKeys 8");
                this.updateKeyAppearance();
                ////Console.WriteLine("layoutKeys 9");
                this.updateKeyCaps(true, uppercase: uppercase, characterUppercase: characterUppercase, shiftState: shiftState);
            }

            ////Console.WriteLine("layoutKeys 10");
            CATransaction.Commit();
        }

        void positionKeys(int pageNum)
        {
            CATransaction.Begin();
            CATransaction.DisableActions = (true);


            ////Console.WriteLine("positionKeys 1");
            Func<KeyboardKey, Key, CGRect, int> setupKey = (view, model, frame) =>
            {
                ////Console.WriteLine("positionKeys 10a");
                if(model.forceColor!=null)
                {
                    view.color = model.forceColor;
                    view.downColor = model.forceColor;
                }
                view.Frame = frame;
                this.modelToView[model] = view;
                this.viewToModel[view] = model;
                ////Console.WriteLine("positionKeys 11a");
                return 0;
            };
            ////Console.WriteLine("positionKeys 2");
            var keyMap = this.generateKeyFrames(this.model, bounds: this.superview.Bounds, pageToLayout: pageNum);

            ////Console.WriteLine("positionKeys 3");
            if (keyMap!=null)
            {

                ////Console.WriteLine("positionKeys 4");
                if (this.shouldPoolKeys )
                {

                    ////Console.WriteLine("positionKeys 5");
                    this.modelToView.Clear();
                    this.viewToModel.Clear();            
                    this.resetKeyPool();

                    List<Key> foundCachedKeys = new List<Key>();

                    ////Console.WriteLine("positionKeys 6");
                    // pass 1: reuse any keys that match the required size;
                    foreach (var kf in keyMap)
                    {
                        var keyView = this.pooledKey(aKey: kf.Key, model: this.model, frame: kf.Value);
                        if (keyView!=null)
                        {
                            foundCachedKeys.Add(kf.Key);
                            setupKey(keyView, kf.Key, kf.Value);
                        }
                    }

                    ////Console.WriteLine("positionKeys 7");
                    foreach (var k in foundCachedKeys)
                    {
                        keyMap.Remove(k);
                    }


                    ////Console.WriteLine("positionKeys 8");
                    // pass 2: fill in the blanks;
                    foreach (var kf in keyMap)
                    { 
                        var keyView = this.generateKey();
                        setupKey(keyView, kf.Key, kf.Value);
                    }
                }
                else
                {

                    ////Console.WriteLine("positionKeys 9");
                    foreach (var kf in keyMap)
                    {
                        ////Console.WriteLine("positionKeys 9a");
                        var keyView = this.pooledKey(aKey: kf.Key, model: this.model, frame: kf.Value);
                        if (keyView != null)
                        {
                            ////Console.WriteLine("positionKeys 9b");
                            setupKey(keyView, kf.Key, kf.Value);
                        }
                    }
                }
            }

            ////Console.WriteLine("positionKeys 10");
            CATransaction.Commit();                
        }
    
        public void updateKeyAppearance()
        {
            CATransaction.Begin();
            CATransaction.DisableActions = (true);
            foreach(var kv in this.modelToView)
            {
                this.setAppearanceForKey(kv.Value, model: kv.Key, darkMode: this.darkMode, solidColorMode: this.solidColorMode);
            }
            CATransaction.Commit();
        }

        // on fullReset, we update the keys with shapes, images, etc. as if(from scratch; otherwise, just update the text;
        // WARNING: if key cache is disabled, DO NOT CALL WITH fullReset MORE THAN ONCE;
        public void updateKeyCaps(bool fullReset, bool uppercase, bool characterUppercase, ShiftState shiftState)
        {
            CATransaction.Begin();
            CATransaction.DisableActions = (true);
            if(fullReset )
            {
                foreach(var key in this.modelToView)
                {
                    key.Value.shape = null;

                    var imageKey = key.Value as ImageKey;
                    if (imageKey!=null)
                    { 
                        imageKey.image = null;
                    }
                }
            }
            foreach(var mk in this.modelToView)
            {
                this.updateKeyCap(mk.Value, model: mk.Key, fullReset: fullReset, uppercase: uppercase, characterUppercase: characterUppercase, shiftState: shiftState);

            }
            CATransaction.Commit();
        }

        void updateKeyCap(KeyboardKey key, Key model, bool fullReset, bool uppercase, bool characterUppercase, ShiftState shiftState)
        {
            if(fullReset )
            {
                // font size;

                switch (model.type)
                {
                    case Key.KeyType.ModeChange:
                    case Key.KeyType.Space:
                    case Key.KeyType.Return:
                        key.label.AdjustsFontSizeToFitWidth = true;
                        key.label.Font = key.label.Font.WithSize(16);
                        break;
                    default:
                        key.label.Font = key.label.Font.WithSize(22);
                        break;
                }
                // label inset;

                switch (model.type)
                {
                    case Key.KeyType.ModeChange:
                            key.labelInset = 3;
                            break;
                    default:
                            key.labelInset = 0;
                            break;
                }
                // shapes;

                switch (model.type)
                {
                    case Key.KeyType.Shift:
                        if(key.shape == null )
                        {
                            var shiftShape = this.getShape(typeof(ShiftShape));
                            key.shape = shiftShape;
                        }
                        break;
                    case Key.KeyType.Backspace:
                        if(key.shape == null )
                        {
                            //var backspaceShape = this.getShape(typeof(BackspaceShape));
                            //key.shape = backspaceShape;

                            var imageKey = key as ImageKey;
                            if (imageKey != null)
                            {
                                if (imageKey.image == null)
                                {
                                    var gearImage = new UIImage("delete");
                                    var settingsImageView = new UIImageView(image: gearImage);
                                    imageKey.image = settingsImageView;
                                }
                            }
                        }
                        break;
                    case Key.KeyType.KeyboardChange:
                         if(key.shape == null )
                        {
                            //var backspaceShape = this.getShape(typeof(BackspaceShape));
                            //var globeShape = this.getShape(typeof(GlobeShape));
                            // key.shape = globeShape;

                            var imageKey = key as ImageKey;
                            if (imageKey != null)
                            {
                                if (imageKey.image == null)
                                {
                                    var gearImage = new UIImage("globe");
                                    var settingsImageView = new UIImageView(image: gearImage);
                                    imageKey.image = settingsImageView;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
                // images;

                if(model.type == Key.KeyType.Settings )
                {
                    var imageKey = key as ImageKey;
                    if(imageKey!=null)
                    {
                        if(imageKey.image == null )
                        {
                            var gearImage = new UIImage("gear");
                            var settingsImageView = new UIImageView(image: gearImage);
                            imageKey.image = settingsImageView;
                        }
                    }
                }
            }
            if(model.type == Key.KeyType.Shift )
            {
                if(key.shape == null )
                {
                    var shiftShape = this.getShape(typeof(ShiftShape));
                    key.shape = shiftShape;

                }
                switch (shiftState) 
                {
                    case ShiftState.Disabled:
                        key.Highlighted = false;
                        break;
                    case ShiftState.Enabled:
                        key.Highlighted = true;
                        break;
                    case ShiftState.Locked:
                        key.Highlighted = true;
                        break;
                }
                var shp = (key.shape as ShiftShape);
                if (shp != null)
                {
                    shp.withLock = (shiftState == ShiftState.Locked);
                }

            }
            this.updateKeyCapText(key, model: model, uppercase: uppercase, characterUppercase: characterUppercase);
        }

        void updateKeyCapText(KeyboardKey key, Key model, bool uppercase, bool characterUppercase)
        {
            if(model.type == Key.KeyType.Character )
            {
                key.text = model.keyCapForCase(characterUppercase);
            }
            else
            {
                key.text = model.keyCapForCase(uppercase);
            }
        }
        ///////////////
        // END CALLS //
        ///////////////

        // TODO: avoid array copies;

        // TODO: sizes stored not rounded?
        ///////////////////////////
        // KEY POOLING FUNCTIONS //
        ///////////////////////////
        // if(pool is disabled, always returns a unique key view for the corresponding key model;
        KeyboardKey pooledKey(Key aKey, Keyboard model, CGRect frame)
        {
            ////Console.WriteLine("pooledKey 1");
            if (!this.shouldPoolKeys)
            {
                int p;
                int r;
                int k;

                // TODO: O(N^2) in terms of total # of keys since pooledKey is called for each key, but probably doesn't matter;
                bool foundKey = false;

                int p_ = 0, r_ = 0, k_ = 0;
                foreach (var pp in model.pages)
                {
                    foreach (var rr in pp.rows)
                    {
                        foreach (var kk in rr)
                        {
                            if (kk == aKey)
                            {
                                p = p_;
                                r = r_;
                                k = k_;
                                foundKey = true;
                            }
                            if (foundKey)
                            {
                                break;
                            }
                            k_++;
                        }
                        if (foundKey)
                        {
                            break;
                        }
                        r_++;
                    }
                    if (foundKey)
                    {
                        break;
                    }
                    p_++;
                }

                var id = String.Format("{0}\\{1}\\{2}", k_, r_, p_);

                if (this.nonPooledMap.ContainsKey(id))
                {
                    var key = this.nonPooledMap[id];
                    return key;
                }
                else
                {
                    var key1 = generateKey();
                    this.nonPooledMap[id] = key1;
                    return key1;
                }
            }
            else
            {
                ////Console.WriteLine("pooledKey 2");
                if (this.sizeToKeyMap.ContainsKey(frame.Size))
                {
                    var keyArray = this.sizeToKeyMap[frame.Size];
                    var key = keyArray.Last();
                    if (key!=null)
                    {
                        if(keyArray.Count == 1 )
                        {
                            this.sizeToKeyMap.Remove(frame.Size);
                        }
                        else
                        {
                            keyArray.Remove(key);
                            this.sizeToKeyMap[frame.Size] = keyArray;
                        }
                        return key;
                    }
                    else 
                    {
                        return null;
                    }                
                }
                else
                {
                    return null;
                }
            }
        }

        KeyboardKey createNewKey()
        {
            return new ImageKey();
        }

        // if(pool is disabled, always generates a new key;

        KeyboardKey generateKey()
        {
            Func<KeyboardKey> createAndSetupNewKey = () =>
            {
                var keyView = this.createNewKey();
                keyView.Enabled = true;
                keyView.delegate_ = this;
                this.superview.AddSubview(keyView);
                this.keyPool.Add(keyView);
                return keyView;
            };

            if (this.shouldPoolKeys)
            {
                if (this.sizeToKeyMap.Count > 0)
                {
                    var sk = this.sizeToKeyMap.First();//[this.sizeToKeyMap.startIndex];
                    var key = sk.Value.Last();
                    if (key!=null)
                    {
                        if (sk.Value.Count == 1)
                        {
                            this.sizeToKeyMap.Remove(sk.Key);
                        }
                        else
                        {
                            sk.Value.RemoveAt(sk.Value.Count-1);
                            this.sizeToKeyMap[sk.Key] = sk.Value;
                        }
                        return key;
                    }
                    else 
                    {
                            return createAndSetupNewKey();
                    }
                }
                else
                {
                    return createAndSetupNewKey();
                }
            }
            else
            {
                return createAndSetupNewKey();
            }
        }
        // if(pool is disabled, doesn't do anything;

        void resetKeyPool()
        {
            if (this.shouldPoolKeys)
            {
                this.sizeToKeyMap.Clear();


                foreach (var key in this.keyPool)
                {
                    if (this.sizeToKeyMap.ContainsKey(key.Frame.Size))
                    {
                        var keyArray = this.sizeToKeyMap[key.Frame.Size];
                        keyArray.Add(key);
                        this.sizeToKeyMap[key.Frame.Size] = keyArray;
                    }
                    else 
                    {
                        var keyArray = new List<KeyboardKey>();
                        keyArray.Add(key);
                        this.sizeToKeyMap[key.Frame.Size] = keyArray;
                    }
                    key.Hidden = true;
                }
            }
        }

        // TODO: no support for more than one of the same shape;

        // if(pool disabled, always returns new shape;

        Shape getShape(Type shapeClass)
        {
            var className = shapeClass.Name;// NSString.FromClass(shapeClass);        
            if(this.shouldPoolKeys)
            {
                if(this.shapePool.ContainsKey(className))
                {
                    var shape = this.shapePool[className];
                    return shape;
                }
                else 
                {
                    var shape = (Shape)Activator.CreateInstance(shapeClass, new object[] { CGRect.Empty});
                    this.shapePool[className] = shape;
                    return shape;
                }
            }
            else
            {
                return (Shape)Activator.CreateInstance(shapeClass, new object[] { CGRect.Empty });
            }
        }

        //////////////////////
        // LAYOUT FUNCTIONS //
        ////////////////////// 
        List<CGRect> layoutCharacterRow(Key [] row, CGFloat keyWidth, CGFloat gapWidth, CGRect frame)
        {
            List<CGRect> frames = new List<CGRect>();


            var keySpace = (CGFloat)(row.Length) * keyWidth + (CGFloat)(row.Length - 1) * gapWidth;
            var actualGapWidth = gapWidth;
            var sideSpace = (frame.Width - keySpace) / (CGFloat)(2);
        
            // TODO: port this to the other layout functions;
            // avoiding rounding errors;
            if(sideSpace< 0 )
            {
                sideSpace = 0;
                actualGapWidth = (frame.Width - ((CGFloat)(row.Length) * keyWidth)) / (CGFloat)(row.Length - 1);
            }
        
            var currentOrigin = frame.Location.X + sideSpace;
        
            foreach(var k in row) 
            {
                var roundedOrigin = rounded(currentOrigin);            
                // avoiding rounding errors;
                if(roundedOrigin + keyWidth > frame.Location.X + frame.Width )
                {
                    frames.Add(new CGRect(rounded(frame.Location.X + frame.Width - keyWidth), frame.Location.Y, keyWidth, frame.Height));
                }
                else
                {
                    frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, keyWidth, frame.Height));
                }
            
                currentOrigin += (keyWidth + actualGapWidth);
            }        
            return frames;
        }

        // TODO: pass in actual widths instead;
        List<CGRect> layoutCharacterWithSidesRow(Key[] row, CGRect frame, bool isLandscape, CGFloat keyWidth, CGFloat keyGap)
        {
            var frames = new List<CGRect>();

            var standardFullKeyCount = (int)(this.layoutConstants.keyCompressedThreshhold) - 1;
            var standardGap = (isLandscape ? 
                this.layoutConstants.keyGapLandscape(frame.Width, rowCharacterCount: standardFullKeyCount) : 
                this.layoutConstants.keyGapPortrait(frame.Width, rowCharacterCount: standardFullKeyCount));
            var sideEdges = (isLandscape ? this.layoutConstants.sideEdgesLandscape : this.layoutConstants.sideEdgesPortrait(frame.Width));
            var standardKeyWidth = (frame.Width - sideEdges - (standardGap * (CGFloat)(standardFullKeyCount - 1)) - sideEdges);
            standardKeyWidth /= (CGFloat)(standardFullKeyCount);
            var standardKeyCount = this.layoutConstants.flexibleEndRowMinimumStandardCharacterWidth;

            var standardWidth = (CGFloat)(standardKeyWidth * standardKeyCount + standardGap * (standardKeyCount - 1));
            var currentWidth = (CGFloat)(row.Length - 2) * keyWidth + (CGFloat)(row.Length - 3) * keyGap;

            var isStandardWidth = (currentWidth < standardWidth);
            var actualWidth = (isStandardWidth ? standardWidth : currentWidth);
            var actualGap = (isStandardWidth ? standardGap : keyGap);
            var actualKeyWidth = (actualWidth - (CGFloat)(row.Length - 3) * actualGap) / (CGFloat)(row.Length - 2);

            var sideSpace = (frame.Width - actualWidth) / (CGFloat)(2);

            var m = (isLandscape ? this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthMLandscape : this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthMPortrait);
            var c = (isLandscape ? this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthCLandscape : this.layoutConstants.flexibleEndRowTotalWidthToKeyWidthCPortrait);


            var specialCharacterWidth = sideSpace * m + c;
            specialCharacterWidth = (CGFloat)Math.Max(specialCharacterWidth, keyWidth);
            specialCharacterWidth = rounded(specialCharacterWidth);
            var specialCharacterGap = sideSpace - specialCharacterWidth;


            var currentOrigin = frame.Location.X;

            int k = 0;
            foreach(var kk in row)
            {
                if(k == 0)
                {
                    frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, specialCharacterWidth, frame.Height));
                    currentOrigin += (specialCharacterWidth + specialCharacterGap);
                }
                else 
                if(k == row.Length - 1)
                {
                    currentOrigin += specialCharacterGap;
                    frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, specialCharacterWidth, frame.Height));
                    currentOrigin += specialCharacterWidth;
                }
                else
                {
                    frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, actualKeyWidth, frame.Height));

                    if(k == row.Length - 2 )
                    {
                        currentOrigin += (actualKeyWidth);
                    }
                    else
                    {
                        currentOrigin += (actualKeyWidth + keyGap);
                    }
                }
                k++;
            }
            return frames;
        }

        List<CGRect> layoutSpecialKeysRow(Key[] row, CGFloat keyWidth, CGFloat gapWidth, CGFloat leftSideRatio, CGFloat rightSideRatio, CGFloat micButtonRatio, bool isLandscape, CGRect frame)
        {
            var frames = new List<CGRect>();


            var keysBeforeSpace = 0;
            var keysAfterSpace = 0;
            var reachedSpace = false;

            foreach(var kk in row) 
            {
                if(kk.type == Key.KeyType.Space )
                {
                    reachedSpace = true;
                }
                else
                {
                    if(!reachedSpace )
                    {
                        keysBeforeSpace += 1;
                    }
                    else
                    {
                        keysAfterSpace += 1;
                    }
                }
            }

            //assert(keysBeforeSpace <= 3, "invalid number of keys before space (only max 3 currently supported)");
            //assert(keysAfterSpace == 1, "invalid number of keys after space (only default 1 currently supported)");
            var hasButtonInMicButtonPosition = (keysBeforeSpace == 3);
            var leftSideAreaWidth = frame.Width * leftSideRatio;
            var rightSideAreaWidth = frame.Width * rightSideRatio;
            var leftButtonWidth = (leftSideAreaWidth - (gapWidth * (CGFloat)(2 - 1))) / (CGFloat)(2);
            leftButtonWidth = rounded(leftButtonWidth);
            var rightButtonWidth = (rightSideAreaWidth - (gapWidth * (CGFloat)(keysAfterSpace - 1))) / (CGFloat)(keysAfterSpace);
            rightButtonWidth = rounded(rightButtonWidth);

            var micButtonWidth = (isLandscape ? leftButtonWidth : leftButtonWidth * micButtonRatio);
        
            // special case for mic button;
            if(hasButtonInMicButtonPosition )
            {
                leftSideAreaWidth = leftSideAreaWidth + gapWidth + micButtonWidth;
            }
        
            var spaceWidth = frame.Width - leftSideAreaWidth - rightSideAreaWidth - gapWidth * (CGFloat)(2);
            spaceWidth = rounded(spaceWidth);
            var currentOrigin = frame.Location.X;

            bool beforeSpace = true;

            int k = 0;
            foreach(var kk in row)
            {
                if(kk.type == Key.KeyType.Space )
                {
                    frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, spaceWidth, frame.Height));
                    currentOrigin += (spaceWidth + gapWidth);
                    beforeSpace = false;
                }
                else 
                if(beforeSpace )
                {
                    if(hasButtonInMicButtonPosition && k == 2 )
                    { //mic button position;
                        frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, micButtonWidth, frame.Height));
                        currentOrigin += (micButtonWidth + gapWidth);
                    }
                    else
                    {
                        frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, leftButtonWidth, frame.Height));
                        currentOrigin += (leftButtonWidth + gapWidth);
                    }
                }
                else
                {
                    frames.Add(new CGRect(rounded(currentOrigin), frame.Location.Y, rightButtonWidth, frame.Height));
                    currentOrigin += (rightButtonWidth + gapWidth);
                }
                k++;
            }
            return frames;
        }

        ////////////////
        // END LAYOUT //
        ////////////////

        public CGRect frameForPopup(KeyboardKey key, Direction direction)
        {
            var actualScreenWidth = (UIScreen.MainScreen.NativeBounds.Size.Width / UIScreen.MainScreen.NativeScale);
            var totalHeight = this.layoutConstants.popupTotalHeight(actualScreenWidth);
            var popupWidth = key.Bounds.Width + this.layoutConstants.popupWidthIncrement;
            var popupHeight = totalHeight - this.layoutConstants.popupGap - key.Bounds.Height;
            var popupCenterY = 0;
        
            return new CGRect((key.Bounds.Width - popupWidth) / (CGFloat)(2), -popupHeight - this.layoutConstants.popupGap, popupWidth, popupHeight);
        }

        public void willShowPopup(KeyboardKey key, Direction direction)
        {
            // TODO: actual numbers, not standins;
            var popup = key.popup;
            if(popup!=null)
            {
                var actualSuperview = (this.superview.Superview != null ? this.superview.Superview : this.superview);            
                var localFrame = actualSuperview.ConvertRectFromView(popup.Frame, fromView: popup.Superview);
                if(localFrame.Location.Y < 3 )
                {
                    localFrame.Location = new CGPoint(localFrame.Location.X, 3);
                    key.background.attached = Direction.Down;
                    key.connector.startDir = Direction.Down;
                    key.background.hideDirectionIsOpposite = true;
                }
                else
                {
                    // TODO: this needs to be reset somewhere;
                    key.background.hideDirectionIsOpposite = false;
                }

                if (localFrame.Location.X < 3 )
                {
                    localFrame.Location = new CGPoint(key.Frame.Location.X, localFrame.Location.Y);
                }
                if(localFrame.Location.X + localFrame.Width > superview.Bounds.Width - 3 )
                {
                    localFrame.Location = new CGPoint(key.Frame.Location.X + key.Frame.Width - localFrame.Width, localFrame.Location.Y);
                }
                popup.Frame = actualSuperview.ConvertRectToView(localFrame, toView: popup.Superview);
            }
        }

        public void willHidePopup(KeyboardKey key)
        {
        }
    }
}