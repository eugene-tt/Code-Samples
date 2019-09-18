using System;

using ObjCRuntime;
using Foundation;
using UIKit;
using AudioToolbox;
using CoreGraphics;
using System.Collections.Generic;

namespace KeyboardExtension
{
    using NSTimeInterval = Double;
    using CGFloat = nfloat;
    using CoreAnimation;

    public class Metrics
    {
        public static nfloat metric(string name)
        {
            var metrics = new SafeDict<String, Double>()
            {
                { "topBanner", 40},
                { "topSpacer", 20}
            };
            if (!metrics.ContainsKey(name))
                return 0;

            return (nfloat)(metrics[name]);
        }
    }       

    public partial class KeyboardViewController : UIInputViewController
    {
        NSTimeInterval backspaceDelay = 0.5;
        NSTimeInterval backspaceRepeat = 0.07;

        Keyboard keyboard;
        ForwardingView forwardingView;
        KeyboardLayout layout;
        NSLayoutConstraint heightConstraint;

        ExtraView bannerView;
        ExtraView spacerView;
        UIToolbar toolbarView;
        ExtraView settingsView;

        int _currentMode;
        public int currentMode
        {
            get
            {
                return _currentMode;
            }
            set
            {
                if(_currentMode != value)
                {
                    _currentMode = value;
                    setMode(_currentMode);
                }
                else
                {
                    _currentMode = value;
                }
            }
        }

        void setMode(int mode)
        {
            this.forwardingView.resetTrackedViews();
            this.shiftStartingState = null;
            this.shiftWasMultitapped = false;


            var uppercase = this.shiftState.uppercase();
            var characterUppercase = uppercase;//  (NSUserDefaults.StandardUserDefaults.BoolForKey(kSmallLowercase) ? uppercase : true);
            this.layout?.layoutKeys(mode, uppercase: uppercase, characterUppercase: characterUppercase, shiftState: this.shiftState);
            this.setupKeys();
        }

        NSTimer backspaceDelayTimer;
        NSTimer backspaceRepeatTimer;
        bool backspaceActive
        {
            get
            {
                return (backspaceDelayTimer != null) || (backspaceRepeatTimer != null);
            }
        }
        enum AutoPeriodState
        {
            NoSpace,
            FirstSpace
        };

        AutoPeriodState autoPeriodState = AutoPeriodState.NoSpace;
        int lastCharCountInBeforeContext = 0;

        ShiftState _shiftState;
        public ShiftState shiftState
        {
            get
            {
                return _shiftState;
            }
            set
            {
                _shiftState = value;
                switch (_shiftState)
                {
                    case ShiftState.Disabled:
                        this.updateKeyCaps(false);
                        break;
                    case ShiftState.Enabled:
                        this.updateKeyCaps(true);
                        break;
                    case ShiftState.Locked:
                        this.updateKeyCaps(true);
                        break;
                };
            }
        }

        const string kAutoCapitalization = "kAutoCapitalization";
        const string kPeriodShortcut = "kPeriodShortcut";
        const string kKeyboardClicks = "kKeyboardClicks";
        const string kSmallLowercase = "kSmallLowercase";

        void updateKeyCaps(bool uppercase)
        {
            bool characterUppercase = uppercase;//(NSUserDefaults.StandardUserDefaults.BoolForKey(kSmallLowercase) ? uppercase : true);
            this.layout?.updateKeyCaps(false, uppercase: uppercase, characterUppercase: characterUppercase, shiftState: this.shiftState);
        }

        // state tracking during shift tap;
        bool shiftWasMultitapped = false;
        ShiftState? shiftStartingState;

        CGFloat _keyboardHeight;
        public CGFloat keyboardHeight
        {
            get
            {
                NSLayoutConstraint constraint = this.heightConstraint;
                if(constraint!=null)
                {
                    return constraint.Constant;
                }
                else 
                {
                    return _keyboardHeight;
                }
            }
            set
            {
                _keyboardHeight = value;
                this.setHeight(value);
            }
        }

        void setHeight(CGFloat height)
        {
            if (this.heightConstraint == null)
            {
                Console.WriteLine(String.Format("setHeight1: {0}", height));
                this.heightConstraint = NSLayoutConstraint.Create(
                    view1: this.View,
                    attribute1: NSLayoutAttribute.Height,
                    relation: NSLayoutRelation.Equal,
                    view2: null,
                    attribute2: NSLayoutAttribute.NoAttribute,
                    multiplier: 1,
                    constant: height);
                this.heightConstraint.SetIdentifier("idHC1");

                this.heightConstraint.Priority = (float)UILayoutPriority.Required;
                this.View.AddConstraint(this.heightConstraint); // TODO: what if(view already has constraint added?
            }
            else
            {
                Console.WriteLine(String.Format("setHeight2: {0}", height));
                this.heightConstraint.Constant = height;
            }
        }


        public KeyboardViewController(IntPtr handle)
            : base(handle)
        {
            //NSDictionary dict = new NSDictionary();

            //dict[kAutoCapitalization] = new NSNumber(true);
            //dict[kPeriodShortcut] = new NSNumber(true);
            //dict[kKeyboardClicks] = new NSNumber(true);
            //dict[kSmallLowercase] = new NSNumber(true);

            //NSUserDefaults.StandardUserDefaults.RegisterDefaults(dict);


            this.keyboard = new DefaultKeyboard();


            this.shiftState = ShiftState.Disabled;  
            this.currentMode = 0;
             
            // Perform custom UI setup here
            this.forwardingView = new ForwardingView(frame: CGRect.Empty);
            this.View.AddSubview(this.forwardingView);


            NSNotificationCenter.DefaultCenter.AddObserver(NSUserDefaults.DidChangeNotification, defaultsChanged);
         }

        //public KeyboardViewController(string nibName, NSBundle bundle)
        //{

        //}

        protected override void Dispose(bool disposing)
        {
            backspaceDelayTimer.Invalidate();
            backspaceRepeatTimer.Invalidate();


            NSNotificationCenter.DefaultCenter.RemoveObserver(this);

            base.Dispose(disposing);
        }

        void defaultsChanged(NSNotification notification)
        {
            //var defaults = notification.object as? NSUserDefaults;
            this.updateKeyCaps(this.shiftState.uppercase());
        }

        /*
        BUG NOTE;

        For some strange reason, a layout pass of the entire keyboard is triggered 
        whenever a popup shows up, if(one of the following is done:

        a) The forwarding view uses an autoresizing mask.
        b) The forwarding view has constraints set anywhere other than init.

        On the other hand, setting (non-autoresizing) constraints or just setting the;
        frame in layoutSubviews works perfectly fine.

        I don't really know what to make of this. Am I doing Autolayout wrong, is it;
        a bug, or is it expected behavior? Perhaps this has to do with the fact that;
        the view's frame is only ever explicitly modified when set directly in layoutSubviews,
        and not implicitly modified by various Autolayout constraints;
        (even though it should really not be changing).
        */

        bool constraintsAdded = false;

        LayoutConstants _layoutConstants = null;
        LayoutConstants layoutConstants
        {
            get
            {
                if (_layoutConstants == null)
                {
                    _layoutConstants = new LayoutConstants();
                }
                return _layoutConstants;
            }
        }
        GlobalColors _globalColors = null;
        GlobalColors globalColors
        {
            get
            {
                if(_globalColors == null)
                {
                    _globalColors = new GlobalColors();
                }
                return _globalColors;
            }
        }

        void setupLayout()
        {
            if(!constraintsAdded )
            {
                this.layout = new KeyboardLayout(model: this.keyboard, superview: this.forwardingView, layoutConstants: this.layoutConstants, globalColors: this.globalColors, darkMode: this.darkMode(), solidColorMode: this.solidColorMode());


                this.layout.initialize();
                this.setMode(0);


                //this.setupKludge();


                this.updateKeyCaps(this.shiftState.uppercase());
                var capsWasSet = this.setCapsIfNeeded();
                shiftState = ShiftState.Enabled; // FIXX

                this.updateAppearances(this.darkMode());
                this.addInputTraitsObservers();


                this.constraintsAdded = true;
            }
            //Console.WriteLine("setupLayout done");

        }

        CADisplayLink traitPollingTimer;

        void addInputTraitsObservers()
        {
            // note that KVO doesn't work on textDocumentProxy, so we have to poll
            if (traitPollingTimer != null)
            {
                traitPollingTimer.Invalidate();
            }
            traitPollingTimer = UIScreen.MainScreen.CreateDisplayLink(() => pollTraits());
            if (traitPollingTimer != null)
            {
                traitPollingTimer.AddToRunLoop(NSRunLoop.Current, mode: NSRunLoopMode.Default);
            }
            //Console.WriteLine("addInputTraitsObservers done");
        }

        void pollTraits()
        {
            //Console.WriteLine("pollTraits loop");
            var proxy = this.TextDocumentProxy;
            var layout = this.layout;
            if (layout!=null)
            {
                var appearanceIsDark = (proxy.KeyboardAppearance == UIKeyboardAppearance.Dark);
                if (appearanceIsDark != layout.darkMode)
                {
                    this.updateAppearances(appearanceIsDark);
                }
            }
        }

        bool characterIsPunctuation(char character)
        {
            return (character == '.') || (character == '!') || (character == '?');
        }

        bool characterIsNewline(char character)
        {
            return (character == '\n') || (character == '\r');
        }

        bool characterIsWhitespace(char character)
        {
            // there are others, but who cares;
            return (character == ' ') || (character == '\n') || (character == '\r') || (character == '\t');
        }

        bool stringIsWhitespace(string str)
        {
            if (str != null )
            {
                foreach(var chr in str)
                {
                    if (!characterIsWhitespace(chr))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        bool shouldAutoCapitalize()
        {
            if (!NSUserDefaults.StandardUserDefaults.BoolForKey(kAutoCapitalization))
            {
                return false;
            }

            var traits = this.TextDocumentProxy;
            var autocapitalization = traits.AutocapitalizationType;

            var documentProxy = this.TextDocumentProxy;
            //var beforeContext = documentProxy.documentContextBeforeInput;
            string beforeContext;

            switch (autocapitalization)
            {
                case UITextAutocapitalizationType.None:
                    return false;
                case UITextAutocapitalizationType.Words:
                    beforeContext = documentProxy.DocumentContextBeforeInput;
                    if (beforeContext != null && beforeContext.Length > 1)
                    {
                        var previousCharacter = beforeContext[beforeContext.Length - 1];
                        return this.characterIsWhitespace(previousCharacter);
                    }
                    else
                    {
                        return true;
                    }
                case UITextAutocapitalizationType.Sentences:
                    beforeContext = documentProxy.DocumentContextBeforeInput;
                    if (beforeContext != null)
                    {
                        var offset = Math.Min(3, beforeContext.Length);
                        var index = beforeContext.Length - 1;

                        for (var i = 0; i < offset; i += 1)
                        {
                            index = index - 1;
                            var chr = beforeContext[index];

                            if (characterIsPunctuation(chr))
                            {
                                if (i == 0)
                                {
                                    return false; //not enough spaces after punctuation;
                                }
                                else
                                {
                                    return true; //punctuation with at least one space after it;
                                }
                            }
                            else
                            {
                                if (!characterIsWhitespace(chr))
                                {
                                    return false; //hit a foreign character before getting to 3 spaces;
                                }
                                else if (characterIsNewline(chr))
                                {
                                    return true; //hit start of line;
                                }
                            }
                        }
                    }
                    return true; //either got 3 spaces or hit start of line;                        
                case UITextAutocapitalizationType.AllCharacters:
                    return true;
            }
            return false;
        }

        bool setCapsIfNeeded()
        {
            if (this.shouldAutoCapitalize())
            {
                switch (this.shiftState)
                {
                    case ShiftState.Disabled:
                        this.shiftState = ShiftState.Enabled;
                        break;
                    case ShiftState.Enabled:
                        this.shiftState = ShiftState.Enabled;
                        break;
                    case ShiftState.Locked:
                        this.shiftState = ShiftState.Locked;
                        break;
                }

                return true;
            }
            else
            {
                switch (this.shiftState)
                {
                    case ShiftState.Disabled:
                        this.shiftState = ShiftState.Disabled;
                        break;
                    case ShiftState.Enabled:
                        this.shiftState = ShiftState.Disabled;
                        break;
                    case ShiftState.Locked:
                        this.shiftState = ShiftState.Locked;
                        break;
                }

                return false;
            }
        }

        // only available after frame becomes non-zero;
        bool darkMode()
        {
            bool darkMode = false;
            var proxy = this.TextDocumentProxy;
            darkMode = proxy.KeyboardAppearance == UIKeyboardAppearance.Dark;
            return darkMode;
        }

        bool solidColorMode()
        {
            return UIAccessibility.IsReduceTransparencyEnabled;
        }

        CGRect? lastLayoutBounds = null;


        CGFloat heightForOrientation(UIInterfaceOrientation orientation, bool withTopBanner)
        {
            var isPad = (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad);

            //TODO: hardcoded stuff;
            var actualScreenWidth = (UIScreen.MainScreen.NativeBounds.Size.Width / UIScreen.MainScreen.NativeScale);
            var canonicalPortraitHeight = (isPad ? (CGFloat)(264) : (CGFloat)(((orientation.IsPortrait() && actualScreenWidth >= 400) ? 226 : 216)));
            var canonicalLandscapeHeight = (isPad ? (CGFloat)(352) : (CGFloat)(162));
            //var canonicalPortraitHeight = (isPad ? (CGFloat)(264) : (CGFloat)(((orientation.IsPortrait() && actualScreenWidth >= 400) ? 226 : 216)));
            //var canonicalLandscapeHeight = (isPad ? (CGFloat)(352) : (CGFloat)(162));
            var topBannerHeight = (withTopBanner ? Metrics.metric("topBanner") : 0);
            var topSpacerrHeight = (withTopBanner ? Metrics.metric("topSpacer") : 0);

            return (CGFloat)(orientation.IsPortrait() ? 
                canonicalPortraitHeight + topBannerHeight + topSpacerrHeight : 
                canonicalLandscapeHeight + topBannerHeight + topSpacerrHeight);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.setupKludge();

            var aBanner = this.createBanner();
            if (aBanner != null)
            {
                toolbarView = new UIToolbar(new CGRect(0.0f, 0.0f, this.View.Frame.Size.Width, Metrics.metric("topBanner")));
                cleanToolbar();

                aBanner.Add(toolbarView);
                aBanner.Hidden = true;
                this.View.InsertSubviewBelow(aBanner, siblingSubview: this.forwardingView);
                this.bannerView = aBanner;
            }

            var aSpacer = this.createSpacer();
            if (aSpacer != null)
            {
                aSpacer.Hidden = true;
                this.View.InsertSubviewBelow(aSpacer, siblingSubview: this.forwardingView);
                this.spacerView = aSpacer;
            }
        }


        public override void ViewDidLayoutSubviews()
        {

            this.setupLayout();

            //Console.WriteLine("ViewDidLayoutSubviews 1");
            if (this.View.Bounds == CGRect.Empty)
            {
                return;
            }

            var orientationSavvyBounds = new CGRect(0, 0, this.View.Bounds.Width, this.heightForOrientation(this.InterfaceOrientation, withTopBanner: false));


            //Console.WriteLine("ViewDidLayoutSubviews 2");
            if (lastLayoutBounds != null && lastLayoutBounds == orientationSavvyBounds)
            {
                // do nothing;
            }
            else
            {

                //Console.WriteLine("ViewDidLayoutSubviews 2a");
                var uppercase = this.shiftState.uppercase();
                var characterUppercase = uppercase;// (NSUserDefaults.StandardUserDefaults.BoolForKey(kSmallLowercase) ? uppercase : true);


                //Console.WriteLine("ViewDidLayoutSubviews 2b");
                this.forwardingView.Frame = orientationSavvyBounds;

                //Console.WriteLine("ViewDidLayoutSubviews 2c");
                this.layout?.layoutKeys(this.currentMode, uppercase: uppercase, characterUppercase: characterUppercase, shiftState: this.shiftState);
                this.lastLayoutBounds = orientationSavvyBounds;

                //Console.WriteLine("ViewDidLayoutSubviews 2d");
                this.setupKeys();
            }

            if (bannerView != null)
            {
                this.bannerView.Frame = new CGRect(0, 0, this.View.Bounds.Width, Metrics.metric("topBanner"));
                this.toolbarView.Frame = new CGRect(0, 0, this.View.Bounds.Width, Metrics.metric("topBanner"));
                //this.bannerView.BackgroundColor = UIColor.Red;
            }

            if (spacerView != null)
            {
                this.spacerView.Frame = new CGRect(0, Metrics.metric("topBanner"), this.View.Bounds.Width, Metrics.metric("topSpacer"));
                //this.spacerView.BackgroundColor = UIColor.Yellow;
            }

            var newOrigin = new CGPoint(0, this.View.Bounds.Height - this.forwardingView.Bounds.Height);
            var fr = this.forwardingView.Frame;
            fr.Location = newOrigin;
            this.forwardingView.Frame = fr;
        }

        public override void ViewWillAppear(bool animated)
        {
            if(bannerView!=null)
            {
                this.bannerView.Hidden = false;
            }
            if (spacerView != null)
            {
                this.spacerView.Hidden = false;
            }
            this.keyboardHeight = this.heightForOrientation(this.InterfaceOrientation, withTopBanner: true);
        }

        public override void UpdateViewConstraints()
        {
            base.UpdateViewConstraints();

            // Add custom view sizing constraints here
            if (this.View.Frame.Size.Width == 0 || this.View.Frame.Size.Height == 0)
            {
                return;
            }
            
            setHeight(keyboardHeight);
        }

        public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            this.forwardingView.resetTrackedViews();
             this.shiftStartingState = null;
             this.shiftWasMultitapped = false;

            // optimization: ensures smooth animation;
            var keyPool = this.layout.keyPool; 
            if(keyPool!=null)
            {
                foreach(var view in keyPool )
                {
                    view.shouldRasterize = true;
                }
            }
            this.keyboardHeight = this.heightForOrientation(toInterfaceOrientation, withTopBanner: true);
        }

        public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            // optimization: ensures quick mode and shift transitions;
            var keyPool = this.layout.keyPool;
            if (keyPool != null)
            {
                foreach (var view in keyPool)
                {
                    view.shouldRasterize = false;
                }
            }
        }

        // without this here kludge, the height constraint for the keyboard does not work for some reason;
        UIView kludge;
        void setupKludge()
        {
            if (this.kludge == null)
            {
                UIView kludge1 = new UIView();
                kludge1.BackgroundColor = UIColor.Yellow;
                this.View.AddSubview(kludge1);
                kludge1.TranslatesAutoresizingMaskIntoConstraints = false;
                kludge1.Hidden = true;                

                NSLayoutConstraint a = NSLayoutConstraint.Create(view1: kludge1, attribute1: NSLayoutAttribute.Left, relation: NSLayoutRelation.Equal, view2: this.View, attribute2: NSLayoutAttribute.Left, multiplier: 1, constant: 0);
                NSLayoutConstraint b = NSLayoutConstraint.Create(view1: kludge1, attribute1: NSLayoutAttribute.Right, relation: NSLayoutRelation.Equal, view2: this.View, attribute2: NSLayoutAttribute.Left, multiplier: 1, constant: 0);
                NSLayoutConstraint c = NSLayoutConstraint.Create(view1: kludge1, attribute1: NSLayoutAttribute.Top, relation: NSLayoutRelation.Equal, view2: this.View, attribute2: NSLayoutAttribute.Top, multiplier: 1, constant: 0);
                NSLayoutConstraint d = NSLayoutConstraint.Create(view1: kludge1, attribute1: NSLayoutAttribute.Bottom, relation: NSLayoutRelation.Equal, view2: this.View, attribute2: NSLayoutAttribute.Top, multiplier: 1, constant: 0);


                a.SetIdentifier("idHCa");
                b.SetIdentifier("idHCa");
                c.SetIdentifier("idHCc");
                d.SetIdentifier("idHCd");

                this.View.AddConstraints(new NSLayoutConstraint[] { a, b, c, d });
                this.kludge = kludge1;    
            }
            //Console.WriteLine("Kludge is not created");
        }
        

        //BUG NOTE;

        //None of the UIContentContainer methods are called for this controller.
        //*/
        //override void viewWillTransitionToSize(size: CGSize, withTransitionCoordinator coordinator: UIViewControllerTransitionCoordinator) {
        //    super.viewWillTransitionToSize(size, withTransitionCoordinator: coordinator);
        //}

        void setupKeys()
        {
            if(this.layout == null )
            {
                return;
            }

            foreach(var page in keyboard.pages)
            {
                foreach (var rowKeys in page.rows)
                { // TODO: quick hack;
                    foreach (var key in rowKeys)
                    {
                        var keyView = this.layout?.viewForKey(key);
                        if(keyView!=null)
                        {
                            keyView.RemoveTarget(null, sel: null, events: UIControlEvent.AllEvents);

                            switch (key.type)
                            {
                                case Key.KeyType.KeyboardChange:
                                    keyView.TouchUpInside += (s, e) => { this.advanceTapped(keyView); };
                                        //.AddTarget(this, sel: new Selector("advanceTapped:"), events: UIControlEvent.TouchUpInside);
                                    break;
                                case Key.KeyType.Backspace:
                                    //var cancelEvents = UIControlEvent.TouchUpInside |UIControlEvent.TouchUpInside|UIControlEvent.TouchDragExit|
                                    //    UIControlEvent.TouchUpOutside | UIControlEvent.TouchCancel|
                                    //    UIControlEvent.TouchDragOutside ;

                                    keyView.TouchDown += (s, e) => { this.backspaceDown(keyView); };
                                    //.AddTarget(this, sel: new Selector("backspaceDown:"), events: UIControlEvent.TouchDown);

                                    keyView.TouchUpInside += (s, e) => { this.backspaceUp(keyView); };
                                    keyView.TouchDragExit += (s, e) => { this.backspaceUp(keyView); };
                                    keyView.TouchUpOutside += (s, e) => { this.backspaceUp(keyView); };
                                    keyView.TouchDragOutside += (s, e) => { this.backspaceUp(keyView); };
                                    //.AddTarget(this, sel: new Selector("backspaceUp:"), events: cancelEvents);

                                    break;
                                case Key.KeyType.Shift:
                                    keyView.TouchDown += (s, e) => { this.shiftDown(keyView); };
                                    //.AddTarget(this, sel: new Selector("shiftDown:"), events: UIControlEvent.TouchDown);

                                    keyView.TouchUpInside += (s, e) => { this.shiftUp(keyView); };
                                    //.AddTarget(this, sel: new Selector("shiftUp:"), events: UIControlEvent.TouchUpInside);

                                    keyView.TouchDownRepeat += (s, e) => { this.shiftDoubleTapped(keyView); };
                                    //.AddTarget(this, sel: new Selector("shiftDoubleTapped:"), events: UIControlEvent.TouchDownRepeat);

                                    break;
                                case Key.KeyType.ModeChange:

                                    keyView.TouchDown += (s, e) => { this.modeChangeTapped(keyView); };
                                    //.AddTarget(this, sel: new Selector("modeChangeTapped:"), events: UIControlEvent.TouchDown);

                                    break;
                                case Key.KeyType.Settings:
                                    // keyView.TouchDown += (s, e) => { this.toggleSettings(keyView); };
                                    //.AddTarget(this, sel: new Selector("toggleSettings"), events: UIControlEvent.TouchUpInside);
                                    break;
                                default:
                                    break;
                            }

                            if (key.isCharacter )
                            {
                                if (UIDevice.CurrentDevice.UserInterfaceIdiom != UIUserInterfaceIdiom.Pad )
                                {
                                    keyView.TouchDown += (s, e) => { this.showPopup(keyView); };
                                    keyView.TouchDragInside += (s, e) => { this.showPopup(keyView); };
                                    keyView.TouchDragEnter += (s, e) => { this.showPopup(keyView); };
                                    //.AddTarget(this, sel: new Selector("showPopup:"), events: UIControlEvent.TouchDown|UIControlEvent.TouchDragInside| UIControlEvent.TouchDragEnter);


                                    keyView.TouchDragExit += (s, e) => { keyView.hidePopup(); };
                                    keyView.TouchCancel += (s, e) => { keyView.hidePopup(); };
                                    //.AddTarget(keyView, sel: new Selector("hidePopup"), events: UIControlEvent.TouchDragExit|UIControlEvent.TouchCancel);

                                    keyView.TouchUpInside += (s, e) => { this.hidePopupDelay(keyView); };
                                    keyView.TouchUpOutside += (s, e) => { this.hidePopupDelay(keyView); };
                                    keyView.TouchDragOutside += (s, e) => { this.hidePopupDelay(keyView); };
                                    //.AddTarget(this, sel: new Selector("hidePopupDelay:"), events: UIControlEvent.TouchUpInside| UIControlEvent.TouchUpOutside| UIControlEvent.TouchDragOutside);
                                }
                            }

                            if (key.hasOutput)
                            {
                                keyView.TouchUpInside += (s, e) => { this.keyPressedHelper(keyView); };
                                //.AddTarget(this, sel: new Selector("keyPressedHelper:"), events: UIControlEvent.TouchUpInside);
                            }

                            if (key.type != Key.KeyType.Shift && key.type != Key.KeyType.ModeChange )
                            {
                                keyView.TouchDown += (s, e) => { this.highlightKey(keyView); };
                                keyView.TouchDragInside += (s, e) => { this.highlightKey(keyView); };
                                keyView.TouchDragEnter += (s, e) => { this.highlightKey(keyView); };
                                //.AddTarget(this, sel: new Selector("highlightKey:"), events: UIControlEvent.TouchDown|UIControlEvent.TouchDragInside| UIControlEvent.TouchDragEnter);

                                keyView.TouchUpInside += (s, e) => { this.unHighlightKey(keyView); };
                                keyView.TouchUpOutside += (s, e) => { this.unHighlightKey(keyView); };
                                keyView.TouchDragOutside += (s, e) => { this.unHighlightKey(keyView); };
                                keyView.TouchDragExit += (s, e) => { this.unHighlightKey(keyView); };
                                keyView.TouchCancel += (s, e) => { this.unHighlightKey(keyView); };
                                //.AddTarget(this, sel: new Selector("unHighlightKey:"), events: UIControlEvent.TouchUpInside| UIControlEvent.TouchUpOutside| UIControlEvent.TouchDragOutside|
                                //UIControlEvent.TouchDragExit| UIControlEvent.TouchCancel);
                            }

                            keyView.TouchDown += (s, e) => { this.playKeySound(); };
                            //.AddTarget(this, sel: new Selector("playKeySound"), events: UIControlEvent.TouchDown);
                        }
                    }
                }
            }
        }

        /////////////////
        // POPUP DELAY //
        /////////////////

        KeyboardKey keyWithDelayedPopup;
        NSTimer popupDelayTimer;

        void showPopup(KeyboardKey sender)
        {
            if (sender == this.keyWithDelayedPopup )
            {
                this.popupDelayTimer.Invalidate();
            }
            sender.showPopup();
        }

        void hidePopupDelay(KeyboardKey sender)
        {
            this.popupDelayTimer?.Invalidate();


            if (sender != this.keyWithDelayedPopup)
            {
                this.keyWithDelayedPopup?.hidePopup();
                this.keyWithDelayedPopup = sender;
            }

            if (sender.popup != null)
            {
                this.popupDelayTimer = NSTimer.CreateScheduledTimer((double)0.05, (t) => hidePopupCallback());
            }
        }

        void hidePopupCallback()
        {
            this.keyWithDelayedPopup?.hidePopup();
            this.keyWithDelayedPopup = null;
            this.popupDelayTimer = null;
        }

        /////////////////////
        // POPUP DELAY END //
        /////////////////////

        // TODO: this is currently not working as intended; only called when selection changed -- iOS bug;
        public override void TextDidChange(IUITextInput textInput)
        {
            this.contextChanged();
        }

        void contextChanged()
        {
            this.setCapsIfNeeded();
            this.autoPeriodState = AutoPeriodState.NoSpace;
        }

        void updateAppearances(bool appearanceIsDark)
        {
            this.layout.solidColorMode = this.solidColorMode();
            this.layout.darkMode = appearanceIsDark;
            this.layout.updateKeyAppearance();

            this.bannerView._darkMode = appearanceIsDark;
            this.spacerView._darkMode = appearanceIsDark;
            //this.settingsView._darkMode = appearanceIsDark;
        }

        void highlightKey(KeyboardKey sender)
        {
            sender.Highlighted = true;
        }

        void unHighlightKey(KeyboardKey sender)
        {
            sender.Highlighted = false;
        }

        void keyPressedHelper(KeyboardKey sender)
        {
            var model = this.layout?.keyForView(sender);
            if (model!=null)
            {
                this.keyPressed(model);

                // auto exit from special char subkeyboard;
                if (model.type == Key.KeyType.Space || model.type == Key.KeyType.Return )
                {
                    this.currentMode = 0;
                }
                else if(model.lowercaseOutput == "'" )
                {
                    this.currentMode = 0;
                }
                else if(model.type == Key.KeyType.Character )
                {
                    this.currentMode = 0;
                }

                // auto period on double space;
                // TODO: timeout;

                this.handleAutoPeriod(model);
                // TODO: reset context;
            }

            this.setCapsIfNeeded();
        }

        void handleAutoPeriod(Key key)
        {
            if (!NSUserDefaults.StandardUserDefaults.BoolForKey(kPeriodShortcut))
            {
                return;
            }

            if (this.autoPeriodState == AutoPeriodState.FirstSpace)
            {
                if (key.type != Key.KeyType.Space)
                {
                    this.autoPeriodState = AutoPeriodState.NoSpace;
                    return;
                }

                Func<bool> charactersAreInCorrectState = () =>
                {
                    var previousContext = this.TextDocumentProxy.DocumentContextBeforeInput;
                    if (previousContext == null || previousContext.Length < 3)
                    {
                        return false;
                    }

                    var index = previousContext.Length - 1;

                    index = index - 1;
                    if (previousContext[index] != ' ')
                    {
                        return false;
                    }

                    index = index - 1;
                    if (previousContext[index] != ' ' )
                    {
                        return false;
                    }

                    index = index - 1;
                    var chr = previousContext[index];
                    if (this.characterIsWhitespace(chr) || this.characterIsPunctuation(chr) || chr == ',')
                    {
                        return false;
                    }
                    return true;
                };

                if(charactersAreInCorrectState())
                {
                    this.TextDocumentProxy.DeleteBackward();
                    this.TextDocumentProxy.DeleteBackward();
                    this.TextDocumentProxy.InsertText(".");
                    this.TextDocumentProxy.InsertText(" ");
                }

                this.autoPeriodState = AutoPeriodState.NoSpace;
            }
            else
            {
                if (key.type == Key.KeyType.Space )
                {
                    this.autoPeriodState = AutoPeriodState.FirstSpace;
                }
            }
        }

        void cancelBackspaceTimers()
        {
            this.backspaceDelayTimer?.Invalidate();
            this.backspaceRepeatTimer?.Invalidate();
            this.backspaceDelayTimer = null;
            this.backspaceRepeatTimer = null;
        }

        void backspaceDown(KeyboardKey sender)
        {
            this.cancelBackspaceTimers();


            this.TextDocumentProxy.DeleteBackward();
            this.setCapsIfNeeded();

            // trigger for subsequent deletes;
            this.backspaceDelayTimer = NSTimer.CreateScheduledTimer(backspaceDelay - backspaceRepeat, (t) => backspaceDelayCallback());
        }

        void backspaceUp(KeyboardKey sender)
        {
            this.cancelBackspaceTimers();
        }

        void backspaceDelayCallback()
        {
            this.backspaceDelayTimer = null;
            this.backspaceRepeatTimer = NSTimer.CreateScheduledTimer(backspaceRepeat, (t) => backspaceRepeatCallback());
        }

        void backspaceRepeatCallback()
        {
            this.playKeySound();

            this.TextDocumentProxy.DeleteBackward();
            this.setCapsIfNeeded();
        }

        // this only works if(full access is enabled;
        void playKeySound()
        {
            if (!NSUserDefaults.StandardUserDefaults.BoolForKey(kKeyboardClicks))
            {
                return;
            }
            SystemSound clickSound = new SystemSound(1104);
            clickSound.PlaySystemSound();
        }

        void shiftDown(KeyboardKey sender)
        {
            this.shiftStartingState = this.shiftState;
            var shiftStartingState = this.shiftStartingState;

            if(shiftStartingState!=null)
            {
                if (shiftStartingState.Value.uppercase() )
                {
                    // handled by shiftUp;
                    return;
                }
                else
                {
                    switch (this.shiftState)
                    {
                        case ShiftState.Disabled:
                            this.shiftState = ShiftState.Enabled;
                            break;
                        case ShiftState.Enabled:
                            this.shiftState = ShiftState.Disabled;
                            break;
                        case ShiftState.Locked:
                            this.shiftState = ShiftState.Disabled;
                            break;
                    }
                    var btn = sender.shape as ShiftShape;
                    if(btn!=null)
                    {
                        btn.withLock = false;
                    }
                }
            }
        }

        void shiftUp(KeyboardKey sender)
        {
            if (this.shiftWasMultitapped )
            {
                // do nothing;
            }
            else
            {
                var shiftStartingState = this.shiftStartingState;
                if (shiftStartingState != null)
                {
                    if (!shiftStartingState.Value.uppercase() )
                    {
                        // handled by shiftDown;
                    }
                    else
                    {
                        switch (this.shiftState)
                        {
                            case ShiftState.Disabled:
                                this.shiftState = ShiftState.Enabled;
                                break;
                            case ShiftState.Enabled:
                                this.shiftState = ShiftState.Disabled;
                                break;
                            case ShiftState.Locked:
                                this.shiftState = ShiftState.Disabled;
                                break;
                        }
                        var btn = sender.shape as ShiftShape;
                        if (btn != null)
                        {
                            btn.withLock = false;
                        }
                    }
                }
            }

            this.shiftStartingState = null;
            this.shiftWasMultitapped = false;
        }

        void shiftDoubleTapped(KeyboardKey sender)
        {
            this.shiftWasMultitapped = true;


            switch (this.shiftState)
            {
                case ShiftState.Disabled:
                    this.shiftState = ShiftState.Locked;
                    break;
                case ShiftState.Enabled:
                    this.shiftState = ShiftState.Locked;
                    break;
                case ShiftState.Locked:
                    this.shiftState = ShiftState.Disabled;
                    break;
            }
        }

        void modeChangeTapped(KeyboardKey sender)
        {
            if(layout.viewToModel.ContainsKey(sender))
            {
                var toMode = this.layout.viewToModel[sender].toMode;
                this.currentMode = toMode;
            }
        }

        void advanceTapped(KeyboardKey sender)
        {
            this.forwardingView.resetTrackedViews();
            this.shiftStartingState = null;
            this.shiftWasMultitapped = false;

            this.AdvanceToNextInputMode();
        }

        //@IBAction void toggleSettings();
        //{
        //    // lazy load settings;
        //    if(this.settingsView == null )
        //    {
        //        if(var aSettings = this.createSettings() )
        //        {
        //            aSettings.darkMode = this.darkMode();


        //            aSettings.hidden = true;
        //            this.View.AddSubview(aSettings);
        //            this.settingsView = aSettings;


        //            aSettings.translatesAutoresizingMaskIntoConstraints = false;


        //            var widthConstraint = NSLayoutConstraint(item: aSettings, attribute: NSLayoutAttribute.Width, relatedBy: NSLayoutRelation.Equal, toItem: this.View, attribute: NSLayoutAttribute.Width, multiplier: 1, constant: 0);
        //            var heightConstraint = NSLayoutConstraint(item: aSettings, attribute: NSLayoutAttribute.Height, relatedBy: NSLayoutRelation.Equal, toItem: this.View, attribute: NSLayoutAttribute.Height, multiplier: 1, constant: 0);
        //            var centerXConstraint = NSLayoutConstraint(item: aSettings, attribute: NSLayoutAttribute.CenterX, relatedBy: NSLayoutRelation.Equal, toItem: this.View, attribute: NSLayoutAttribute.CenterX, multiplier: 1, constant: 0);
        //            var centerYConstraint = NSLayoutConstraint(item: aSettings, attribute: NSLayoutAttribute.CenterY, relatedBy: NSLayoutRelation.Equal, toItem: this.View, attribute: NSLayoutAttribute.CenterY, multiplier: 1, constant: 0);


        //            this.View.addConstraint(widthConstraint);
        //            this.View.addConstraint(heightConstraint);
        //            this.View.addConstraint(centerXConstraint);
        //            this.View.addConstraint(centerYConstraint);
        //                    }
        //    }
        //    if(var settings = this.settingsView ){
        //        var hidden = settings.hidden;
        //                    settings.hidden = !hidden;
        //                    this.forwardingView.hidden = hidden;
        //                    this.forwardingView.userInteractionEnabled = !hidden;
        //                    this.bannerView?.hidden = hidden;
        //                }
        //}









        ////////////////////////////////////
        //MOST COMMONLY EXTENDABLE METHODS //
        ////////////////////////////////////

        void keyPressed(Key key)
        {
            var text = key.outputForCase(this.shiftState.uppercase());
            if (text.ToUpper() == "A")
            {
                text = "AAAA";
            }
            else
            if (text.ToUpper() == "B")
            {
                toolbarView.Items = new[]
                {
                    new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                    new UIBarButtonItem("XXXX", UIBarButtonItemStyle.Plain, delegate
                    {
                        text = "XXXX"; this.TextDocumentProxy.InsertText(text); cleanToolbar();
                    }),
                    new UIBarButtonItem("YYYY", UIBarButtonItemStyle.Plain, delegate
                    {
                        text = "YYYY"; this.TextDocumentProxy.InsertText(text); cleanToolbar();
                    }),
                    new UIBarButtonItem("ZZZZ", UIBarButtonItemStyle.Plain, delegate
                    {
                        text = "ZZZZ"; this.TextDocumentProxy.InsertText(text); cleanToolbar();
                    })
                };
                return;
            }
            else
            {
                cleanToolbar();
            }

            this.TextDocumentProxy.InsertText(text);
        }

        void cleanToolbar()
        {
            toolbarView.Items = new[]
            {
                    new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
            };
        }

        // a banner that sits in the empty space on top of the keyboard;
        ExtraView createBanner()
        {
            // note that dark mode is not yet valid here, so we just put false for clarity;
            return new ExtraView(globalColors: this.globalColors, darkMode: false, solidColorMode: this.solidColorMode());
        }
        ExtraView createSpacer()
        {
            // note that dark mode is not yet valid here, so we just put false for clarity;
            return new ExtraView(globalColors: this.globalColors, darkMode: false, solidColorMode: this.solidColorMode());
        }

        // a settings view that replaces the keyboard when the settings button is pressed;
        ExtraView createSettings()
        {
            return new ExtraView(globalColors: this.globalColors, darkMode: false, solidColorMode: this.solidColorMode());
            // note that dark mode is not yet valid here, so we just put false for clarity;
            //var settingsView = new DefaultSettings(globalColors: this.globalColors, darkMode: false, solidColorMode: this.solidColorMode());
            //settingsView.backButton?.addTarget(this, action: Selector("toggleSettings"), forControlEvents: UIControlEvents.TouchUpInside);
            //return settingsView;
        }
    }
}
