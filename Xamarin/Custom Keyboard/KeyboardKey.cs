using CoreGraphics;
using System;
using System.Collections.Generic;
using UIKit;
using CoreAnimation;
using Foundation;
using ObjCRuntime;

namespace KeyboardExtension
{
    using CGFloat = nfloat;
    public interface KeyboardKeyProtocol
    {
        CGRect frameForPopup(KeyboardKey key, Direction direction);
        void willShowPopup(KeyboardKey key, Direction direction); //may be called multiple times during layout;
        void willHidePopup(KeyboardKey key);
    }

    public enum VibrancyType
    {
        LightSpecial,
        DarkSpecial,
        DarkRegular,
    }

    /*
    PERFORMANCE NOTES;
    * CAShapeLayer: convenient and low memory usage, but chunky rotations;
    * drawRect: fast, but high memory usage (looks like there's a backing store for each of the 3 views);
    * if(I set CAShapeLayer to shouldRasterize, perf is *almost* the same as drawRect, while mem usage is the same as before;
    * oddly, 3 CAShapeLayers show the same memory usage as 1 CAShapeLayer — where is the backing store?
    * might want to move to drawRect with combined draw calls for performance reasons — not clear yet;
*/
    public class ShapeView : UIView
    {
        CAShapeLayer shapeLayer;

        [Export("layerClass")]
        public static Class LayerClass()
        {
            return new Class(typeof(CAShapeLayer));
        }

        UIBezierPath _curve;
        public UIBezierPath curve
        {
            get
            {
                return _curve;
            }
            set
            {
                _curve = value;
                var layer = this.shapeLayer;
                if (layer != null)
                {
                    layer.Path = _curve?.CGPath;
                }
                else
                {
                    this.SetNeedsDisplay();
                }
            }
        }

        UIColor _fillColor;
        public UIColor fillColor
        {
            get
            {
                return _fillColor;
            }
            set
            {
                _fillColor = value;
                var layer = this.shapeLayer;
                if (layer != null)
                {
                    layer.FillColor = _fillColor?.CGColor;
                }
                else
                {
                    this.SetNeedsDisplay();
                }
            }
        }

        UIColor _strokeColor;
        public UIColor strokeColor
        {
            get
            {
                return _strokeColor;
            }
            set
            {
                _strokeColor = value;
                var layer = this.shapeLayer;
                if (layer != null)
                {
                    layer.StrokeColor = _strokeColor?.CGColor;
                }
                else
                {
                    this.SetNeedsDisplay();
                }
            }
        }

        CGFloat? _lineWidth;
        public CGFloat? lineWidth
        {
            get
            {
                return _lineWidth;
            }
            set
            {
                _lineWidth = value;
                var layer = this.shapeLayer;
                if (layer != null)
                {
                    if(_lineWidth.HasValue)
                    {
                        layer.LineWidth = _lineWidth.Value;
                    }
                }
                else
                {
                    this.SetNeedsDisplay();
                }
            }
        }

        public ShapeView() : base(CGRect.Empty)
        {
            this.shapeLayer = this.Layer as CAShapeLayer;
        }

        public ShapeView(CGRect frame) : base(frame)
        {
            this.shapeLayer = this.Layer as CAShapeLayer;


            // optimization: off by default to ensure quick mode transitions; re-enable during rotations;

            //this.layer.shouldRasterize = true;

            //this.layer.rasterizationScale = UIScreen.mainScreen().scale;
        }


        public ShapeView(NSCoder coder)
        {
            throw new ApplicationException("NSCoding not supported");
        }

        public void drawCall(CGRect rect)
        {
            if (this.shapeLayer == null)
            {
                var curve = this.curve;
                if (curve!=null)
                {
                    if (this._lineWidth.HasValue)
                    {
                        curve.LineWidth = this.lineWidth.Value;
                    }

                    var _fillColor = this._fillColor;
                    if (_fillColor!=null)
                    {
                        _fillColor.SetFill();
                        curve.Fill();
                    }

                    var _strokeColor = this._strokeColor;
                    if (_strokeColor != null)
                    {
                        _fillColor.SetStroke();
                        curve.Stroke();
                    }
                }
            }
        }
        public override void Draw(CGRect rect)
        {
            if (this.shapeLayer == null)
            {
                this.drawCall(rect);
            }
        }
    }

    public class KeyboardKey: UIControl
    {
        public KeyboardKeyProtocol delegate_;

        public VibrancyType vibrancy;

        public UILabel label;
        public UILabel popupLabel;

        public string _text;
        public string text
        {
            get

            {
                return _text;
            }
            set
            {
                _text = value;
                this.label.Text = value;
                this.label.Frame = new CGRect(this.labelInset, this.labelInset, this.Bounds.Width - this.labelInset * 2, this.Bounds.Height - this.labelInset * 2);
                this.redrawText();
            }
        }
        
        public UIColor _color            ;
        public UIColor _underColor       ;
        public UIColor _borderColor      ;
        public UIColor _popupColor       ;
        public bool    _drawUnder        ;
        public bool    _drawOver         ;
        public bool    _drawBorder       ;
        public CGFloat _underOffset      ;
        public UIColor _textColor        ;
        public UIColor _downColor        ;
        public UIColor _downUnderColor   ;
        public UIColor _downBorderColor  ;
        public UIColor _downTextColor;  
    
        public UIColor color           { get { return _color          ; } set { _color           = value; updateColors(); }}
        public UIColor underColor      { get { return _underColor     ; }
            set {
                _underColor      = value;
                updateColors(); }}
        public UIColor borderColor     { get { return _borderColor    ; } set { _borderColor     = value; updateColors(); }}
        public UIColor popupColor      { get { return _popupColor     ; } set { _popupColor      = value; updateColors(); }}
        public bool    drawUnder       { get { return _drawUnder      ; } set { _drawUnder       = value; updateColors(); }}
        public bool    drawOver        { get { return _drawOver       ; } set { _drawOver        = value; updateColors(); }}
        public bool    drawBorder      { get { return _drawBorder     ; } set { _drawBorder      = value; updateColors(); }}
        public CGFloat underOffset     { get { return _underOffset    ; } set { _underOffset     = value; updateColors(); }}                                                                                         
        public UIColor textColor       { get { return _textColor      ; } set { _textColor       = value; updateColors(); }}
        public UIColor downColor       { get { return _downColor      ; } set { _downColor       = value; updateColors(); }}
        public UIColor downUnderColor  { get { return _downUnderColor ; } set { _downUnderColor  = value; updateColors(); }}
        public UIColor downBorderColor { get { return _downBorderColor; } set { _downBorderColor = value; updateColors(); }}
        public UIColor downTextColor   { get { return _downTextColor  ; } set { _downTextColor   = value; updateColors(); }}


        public ShapeView displayView;
        public ShapeView borderView;
        public ShapeView underView;

        public CGFloat _labelInset = 0;
        public CGFloat labelInset
        {
            get { return _labelInset; }
            set

            {
                if (_labelInset != value );

                {
                    this.label.Frame = new CGRect(this.labelInset, this.labelInset, this.Bounds.Width - this.labelInset * 2, this.Bounds.Height - this.labelInset * 2);

                }
                _labelInset = value;
            }
        }

        public bool _shouldRasterize = false;
        public bool shouldRasterize
        {
            get { return _shouldRasterize; }
            set
            {
                foreach(var view in new UIView[] { this.displayView, this.borderView, this.underView })
                {
                    view.Layer.ShouldRasterize = shouldRasterize;
                    view.Layer.RasterizationScale = UIScreen.MainScreen.Scale;
                }
            }
        }

        public Direction? popupDirection;

        void redrawText()
        {
            //        this.keyView.Frame = this.Bounds;

            //        this.button.Frame = this.Bounds;

            //        
            //        this.button.setTitle(this.text, forState: UIControlState.Normal);

        }

        public override bool Enabled
        {
            get

            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;
                updateColors();
            }
        }

        public override bool Selected
        {
            get

            {
                return base.Selected;
            }
            set
            {
                base.Selected = value;
                updateColors();
            }
        }

        public override bool Highlighted
        {
            get

            {
                return base.Highlighted;
            }
            set
            {
                base.Highlighted = value;
                updateColors();
            }
        }
    
        public override CGRect Frame
        {
            get

            {
                return base.Frame;
            }
            set
            {
                base.Frame = value;
                redrawText();
            }
        }

        public Shape _shape;
        public Shape shape
        {
            get { return _shape; }
            set
            {
                if(_shape != null && value == null )
                {
                    _shape.RemoveFromSuperview();
                }
                this.redrawShape();
                updateColors();
                _shape = value;
            }
        }

        public KeyboardKeyBackground background;
        public KeyboardKeyBackground popup;
        public KeyboardConnector connector;
        public UIView shadowView;
        public CALayer shadowLayer;

        public KeyboardKey(VibrancyType optionalVibrancy) :
            base(CGRect.Empty)
        {
            this.vibrancy = optionalVibrancy;

            this.displayView = new ShapeView();
            this.underView = new ShapeView();
            this.borderView = new ShapeView();

            this.shadowLayer = new CAShapeLayer();
            this.shadowView = new UIView();

            this.label = new UILabel();
            this.text = "";
        
            this.color = UIColor.White;
            this.underColor = UIColor.Gray;
            this.borderColor = UIColor.Black;
            this.popupColor = UIColor.White;
            this.drawUnder = true;
            this.drawOver = true;
            this.drawBorder = false;
            this.underOffset = 1;
        
            this.background = new KeyboardKeyBackground(cornerRadius: 4, underOffset: this.underOffset);        
            this.textColor = UIColor.Black;
            this.popupDirection = null;       

        
            this.AddSubview(this.shadowView);
            this.shadowView.Layer.AddSublayer(this.shadowLayer);        
            this.AddSubview(this.displayView);

            var underView = this.underView;
            if(underView!=null)
            {
                this.AddSubview(underView);
            }

            var borderView = this.borderView;
            if(borderView!=null)
            {
                this.AddSubview(borderView);
            }
        
            this.AddSubview(this.background);

            this.background.AddSubview(this.label);

        
            //var setupViews: Void = {
            this.displayView.Opaque = false;
            this.underView.Opaque = false;
            this.borderView.Opaque = false;

            
            this.shadowLayer.ShadowOpacity = 0.2f;
            this.shadowLayer.ShadowRadius = 4;
            this.shadowLayer.ShadowOffset = new CGSize(0, 3);

            
            this.borderView.lineWidth = (CGFloat)(0.5);
            this.borderView.fillColor = UIColor.Clear;

            
            this.label.TextAlignment = UITextAlignment.Center;
            this.label.BaselineAdjustment = UIBaselineAdjustment.AlignCenters;
            this.label.Font = this.label.Font.WithSize(22);
            this.label.AdjustsFontSizeToFitWidth = true;
            this.label.MinimumScaleFactor = (CGFloat)(0.1);
            this.label.UserInteractionEnabled = false;
            this.label.Lines = 1;
        }

        public void redrawShape()
        {
            var shape = this.shape;
            if (shape!=null )
            {
                this.text = "";
                shape.RemoveFromSuperview();
                this.AddSubview(shape);


                CGFloat pointOffset = 4;
                var size = new CGSize(this.Bounds.Width - pointOffset - pointOffset, this.Bounds.Height - pointOffset - pointOffset);

                shape.Frame = new CGRect(
                    (CGFloat)((this.Bounds.Width - size.Width) / 2.0),
                    (CGFloat)((this.Bounds.Height - size.Height) / 2.0),
                    size.Width,
                    size.Height);


                shape.SetNeedsLayout();

            }
        }

        public virtual void updateColors()
        {
            CATransaction.Begin();
            CATransaction.DisableActions = (true);

            var switchColors = this.Highlighted || this.Selected;
            
            if (switchColors)
            {
                if (downColor != null)
                {
                    this.displayView.fillColor = downColor;
                }
                else 
                {
                    this.displayView.fillColor = this.color;
                }

                var downUnderColor = this.downUnderColor;
                if (downUnderColor!=null && this.underView!=null)
                {
                    this.underView.fillColor = downUnderColor;
                }
                else
                if (this.underView != null)
                {
                    this.underView.fillColor = this.underColor;
                }

                var downBorderColor = this.downBorderColor;
                if (downBorderColor!=null && this.borderView != null)
                {
                    this.borderView.strokeColor = downBorderColor;
                }
                else
                if (this.borderView != null)
                {
                    this.borderView.strokeColor = this.borderColor;
                }

                var downTextColor = this.downTextColor;
                if (downTextColor != null)
                { 
                    this.label.TextColor = downTextColor;
                    if (this.popupLabel != null) this.popupLabel.TextColor = downTextColor;
                    if (this.shape != null) this.shape.color = downTextColor;
                }
                else
                {
                    this.label.TextColor = this.textColor;
                    if (this.popupLabel != null) this.popupLabel.TextColor = this.textColor;
                    if (this.shape != null) this.shape.color = this.textColor;
                }
            }
            else
            {
                this.displayView.fillColor = this.color;
                this.displayView.strokeColor = this.color;
                this.underView.fillColor = this.underColor;
                this.borderView.strokeColor = this.borderColor;
                this.label.TextColor = this.textColor;
                if(this.popupLabel!=null)this.popupLabel.TextColor = this.textColor;
                if (this.shape != null) this.shape.color = this.textColor;
            }

            if (this.popup != null)
            {
                this.displayView.fillColor = this.popupColor;
            }

            CATransaction.Commit();
        }

        public KeyboardKey(NSCoder coder)
        {
            throw new ApplicationException("NSCoding not supported");
        }
    
        public override void SetNeedsLayout()
        {
            base.SetNeedsLayout();
        }

        CGRect? oldBounds;
        public override void LayoutSubviews()
        {
            this.layoutPopupIfNeeded();

        
            var boundingBox = (this.popup != null ? CGRect.Union(this.Bounds, this.popup.Frame) : this.Bounds);

        
            if(this.Bounds.Width == 0 || this.Bounds.Height == 0 ){
                return;

            }
            if(oldBounds != null && boundingBox.Size.Equals(oldBounds?.Size) )
            {
                return;
            }
            oldBounds = boundingBox;


            base.LayoutSubviews();

        
            CATransaction.Begin();

            CATransaction.DisableActions = (true);

        
            this.background.Frame = this.Bounds;

            this.label.Frame = new CGRect(this.labelInset, this.labelInset, this.Bounds.Width - this.labelInset * 2, this.Bounds.Height - this.labelInset * 2);
        
            this.displayView.Frame = boundingBox;
            this.shadowView.Frame = boundingBox;
            this.borderView.Frame = boundingBox;
            this.underView.Frame = boundingBox;
        
            CATransaction.Commit();        
            this.refreshViews();
        }

        public void refreshViews()
        {
            this.refreshShapes();
            this.redrawText();
            this.redrawShape();
            this.updateColors();
        }

        public virtual void refreshShapes()
        {
            // TODO: dunno why this is necessary;

            this.background.SetNeedsLayout();
            this.background.LayoutIfNeeded();
            if(popup!=null)this.popup.LayoutIfNeeded();
            if (connector != null) this.connector.LayoutIfNeeded();

        
            var testPath = new UIBezierPath();
            var edgePath = new UIBezierPath();
        
            var unitSquare = new CGRect(0, 0, 1, 1);

            // TODO: withUnder;
            Func<KeyboardKeyBackground, UIBezierPath, UIBezierPath, int> addCurves = (fromShape, toPath, toEdgePaths) =>
            {
                var shape = fromShape;
                if (shape != null)
                {
                    var path = shape.fillPath;

                    var translatedUnitSquare_ = this.displayView.ConvertRectFromView(unitSquare, fromView: shape);
                    var transformFromShapeToView_ = CGAffineTransform.MakeTranslation((CGFloat)translatedUnitSquare_.Location.X, (CGFloat)translatedUnitSquare_.Location.Y);

                    path.ApplyTransform(transformFromShapeToView_);

                    if (path != null)
                    {
                        toPath.AppendPath(path);
                    }

                    var edgePaths = shape.edgePaths;
                    if (edgePaths != null)
                    {
                        foreach(var anEdgePath in edgePaths)
                        {
                            var editablePath = anEdgePath;
                            editablePath.ApplyTransform(transformFromShapeToView_);
                            toEdgePaths.AppendPath(editablePath);
                        }
                    }
                }
                return 0;
            };
                    
            addCurves(this.popup, testPath, edgePath);
            addCurves(this.connector, testPath, edgePath);
        
            var shadowPath = UIBezierPath.FromPath(path: testPath.CGPath);
        
            addCurves(this.background, testPath, edgePath);
        
            var underPath = this.background.underPath;

            var translatedUnitSquare = this.displayView.ConvertRectFromView(unitSquare, fromView: this.background);
            var transformFromShapeToView = CGAffineTransform.MakeTranslation(translatedUnitSquare.Location.X, translatedUnitSquare.Location.Y);
            underPath.ApplyTransform(transformFromShapeToView);

        
            CATransaction.Begin();
            CATransaction.DisableActions=(true);

        
            if(this.popup!=null)
            {
                this.shadowLayer.ShadowPath = shadowPath.CGPath;
            }
        
            this.underView.curve = underPath;
            this.displayView.curve = testPath;
            this.borderView.curve = edgePath;

            var borderLayer = this.borderView.Layer as CAShapeLayer;
            if(borderLayer!=null)
            {
                borderLayer.StrokeColor = UIColor.Green.CGColor;
            }        
            CATransaction.Commit();
        }

        public void layoutPopupIfNeeded()
        {
            if(this.popup != null && this.popupDirection == null )
            {
                this.shadowView.Hidden = false;
                this.borderView.Hidden = false;
            
                this.popupDirection = Direction.Up;
            
                this.layoutPopup(this.popupDirection.Value);
                this.configurePopup(this.popupDirection.Value);
            
                this.delegate_.willShowPopup(this, direction: this.popupDirection.Value);

            }
            else
            {
                this.shadowView.Hidden = true;
                this.borderView.Hidden = true;
            }
        }

        public void layoutPopup(Direction dir)
        {
            //assert(this.popup != null, "popup not found");

            var popup = this.popup;
            if( popup!=null)
            {
                var delegate_ = this.delegate_;
                if(delegate_!=null)
                {
                    var frame = delegate_.frameForPopup(this, direction: dir);
                    popup.Frame = frame;
                    popupLabel.Frame = popup.Bounds;
                }
                else
                {
                    popup.Frame = CGRect.Empty;
                    popup.Center = this.Center;
                }
            }
        }

        public void configurePopup(Direction direction)
        {
            //assert(this.popup != null, "popup not found");
        
            this.background.attach(direction);
            this.popup.attach(direction.opposite());
        
            var kv = this.background;
            var p = this.popup;
        
            if(this.connector!=null)     this.connector.RemoveFromSuperview();

            this.connector = new KeyboardConnector(cornerRadius: 4, underOffset: this.underOffset, s: kv, e: p, sC: kv, eC: p, startDirection: direction, endDirection: direction.opposite());

            this.connector.Layer.ZPosition = -1;
            this.AddSubview(this.connector);        
            
            //        this.drawBorder = true;        
            if(direction == Direction.Up )
            {
            //            this.popup!.drawUnder = false;
            //            this.connector!.drawUnder = false;
            }
        }
    
        public void showPopup()
        {
            if(this.popup == null )
            {
                this.Layer.ZPosition = 1000;            
                var popup = new KeyboardKeyBackground(cornerRadius: 9.0f, underOffset: this.underOffset);
                this.popup = popup;
                this.AddSubview(popup);
            
                var _popupLabel = new UILabel();

                _popupLabel.TextAlignment = this.label.TextAlignment;
                _popupLabel.BaselineAdjustment = this.label.BaselineAdjustment;
                _popupLabel.Font = this.label.Font.WithSize(22 * 2);
                _popupLabel.AdjustsFontSizeToFitWidth = this.label.AdjustsFontSizeToFitWidth;
                _popupLabel.MinimumScaleFactor = (CGFloat)(0.1);
                _popupLabel.UserInteractionEnabled = false;
                _popupLabel.Lines = 1;
                _popupLabel.Frame = popup.Bounds;
                _popupLabel.Text = this.label.Text;
                popup.AddSubview(_popupLabel);
                this.popupLabel = _popupLabel;
            
                this.label.Hidden = true;
            }
        }
    
        public void hidePopup()
        {
            if(this.popup != null )
            {
                this.delegate_.willHidePopup(this);
                if (this.popupLabel != null) this.popupLabel.RemoveFromSuperview();
                if (this.popupLabel != null) this.popupLabel = null;            
                this.connector.RemoveFromSuperview();
                this.connector = null;            
                this.popup.RemoveFromSuperview();
                this.popup = null;
            
                this.label.Hidden = false;
                this.background.attach(null);
                this.Layer.ZPosition = 0;
            
                this.popupDirection = null;

            }
        }
    }
}
