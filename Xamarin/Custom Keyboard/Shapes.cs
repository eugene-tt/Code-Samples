using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using UIKit;

namespace KeyboardExtension
{
    using CGFloat = nfloat;


    public class OverflowCanvas : UIView
    {
        public Shape shape;

        public OverflowCanvas(Shape shape) : base(CGRect.Empty)
        {
            this.shape = shape;
            this.Opaque = false;
        }


        public OverflowCanvas(NSCoder coder)
        {
            throw new ApplicationException("NSCoding not supported");
        }

        public override void Draw(CGRect rect)
        {
            var ctx = UIGraphics.GetCurrentContext();

            var cs = CGColorSpace.CreateDeviceRGB();

            ctx.SaveState();


            var xOffset = (this.Bounds.Width - this.shape.Bounds.Width) / (CGFloat)(2);
            var yOffset = (this.Bounds.Height - this.shape.Bounds.Height) / (CGFloat)(2);

            ctx.TranslateCTM(xOffset, yOffset);



            this.shape.drawCall(shape.color != null ? shape.color : UIColor.Black);

            ctx.RestoreState();
        }
    }

    public class BackspaceShape : Shape
    {
        public BackspaceShape(CGRect frame) :
            base(frame)
        {
        }

        public override void drawCall(UIColor color)
        {
            drawBackspace(this.Bounds, color);
        }
    }

    public class ShiftShape : Shape
    {
        public ShiftShape(CGRect frame) :
            base(frame)
        {
        }
        bool _withLock = false;
        public bool withLock
        {
            get
            {
                return _withLock;
            }
            set
            {
                _withLock = value;
                this.overflowCanvas.SetNeedsDisplay();
            }
        }

        public override void drawCall(UIColor color)
        { 
            drawShift(this.Bounds, color, withRect: this.withLock);
        }
    }

    public class GlobeShape : Shape
    {
        public GlobeShape(CGRect frame) :
            base(frame)
        {
        }
        public override void drawCall(UIColor color)
        {
            drawGlobe(this.Bounds, color);
            //drawBackspace(this.Bounds, color);

        }
    }

    public struct Factors
    {
        public CGFloat xScalingFactor;
        public CGFloat yScalingFactor;
        public CGFloat lineWidthScalingFactor;
        public bool fillIsHorizontal;
        public CGFloat offset;
    }

    public class Shape : UIView
    {
        public Shape()
        {
            this.init(frame: CGRect.Empty);
        }

        public Shape(CGRect frame) :
            base(frame)
        {
            this.init(frame);
        }

        public void init(CGRect frame)
        {
            this.Opaque = false;
            this.ClipsToBounds = false;

            this.overflowCanvas = new OverflowCanvas(shape: this);

            this.AddSubview(this.overflowCanvas);

        }

        public Shape(NSCoder coder)
        {
            throw new ApplicationException("NSCoding not supported");
        }

        CGRect? oldBounds;
        public override void LayoutSubviews()
        { 
            if(this.Bounds.Width == 0 || this.Bounds.Height == 0 )
            {
                return;
            }
            if(oldBounds != null && this.Bounds.Equals(oldBounds) )
            {
                return;
            }
            oldBounds = this.Bounds;

            base.LayoutSubviews();

            var overflowCanvasSizeRatio = (CGFloat)(1.25);
            var overflowCanvasSize = new CGSize(this.Bounds.Width * overflowCanvasSizeRatio, this.Bounds.Height * overflowCanvasSizeRatio);

            this.overflowCanvas.Frame = new CGRect(
                (CGFloat)((this.Bounds.Width - overflowCanvasSize.Width) / 2.0),
                (CGFloat)((this.Bounds.Height - overflowCanvasSize.Height) / 2.0),
                overflowCanvasSize.Width,
                overflowCanvasSize.Height);

            this.overflowCanvas.SetNeedsDisplay();

        }

        public virtual void drawCall(UIColor color) { /* override me! */ }


        UIColor _color;
        public UIColor color
        {
            get

            {
                return _color;
            }
            set
            {
                _color = value;
                if((_color != null))
                {
                    this.overflowCanvas.SetNeedsDisplay();
                }
            }
        }

        // in case shapes draw out of.Bounds, we still want them to show;
        protected OverflowCanvas overflowCanvas;

        /////////////////////
        // SHAPE FUNCTIONS //
        /////////////////////

        public Factors getFactors(CGSize fromSize, CGRect toRect)
        {    
            Func<CGFloat> xSize = () =>
            { 
                var scaledSize_ = (fromSize.Width / (CGFloat)(2));

                if(scaledSize_ > toRect.Width )
                {
                    return (toRect.Width / scaledSize_) / (CGFloat)(2);
                }
                else
                {
                    return (CGFloat)(0.5);
                }
            };

            Func<CGFloat> ySize = () =>
            {
                var scaledSize = (fromSize.Height / (CGFloat)(2));

                if(scaledSize > toRect.Height )
                {
                    return (toRect.Height / scaledSize) / (CGFloat)(2);
                }
                else {
                    return (CGFloat)(0.5);
                }
            };
    
            var actualSize = (CGFloat)Math.Min(xSize(), ySize());

            return new Factors()
            {
                xScalingFactor = actualSize,
                yScalingFactor = actualSize,
                lineWidthScalingFactor = actualSize,
                fillIsHorizontal = false,
                offset = 0
            }; 
        }

        public void centerShape(CGSize fromSize, CGRect toRect)
        {
            var xOffset = (toRect.Width - fromSize.Width) / (CGFloat)(2);

            var yOffset = (toRect.Height - fromSize.Height) / (CGFloat)(2);



            var ctx = UIGraphics.GetCurrentContext();

            ctx.SaveState();

            ctx.TranslateCTM(xOffset, yOffset);

        }

        public void endCenter()
        {
            var ctx = UIGraphics.GetCurrentContext();

            ctx.RestoreState();
        }

        public void drawBackspace(CGRect Bounds_, UIColor color_)
        {
            var factors = getFactors(new CGSize(44, 32), toRect: Bounds_);

            var xScalingFactor = factors.xScalingFactor;

            var yScalingFactor = factors.yScalingFactor;

            var lineWidthScalingFactor = factors.lineWidthScalingFactor;



            centerShape(new CGSize(44 * xScalingFactor, 32 * yScalingFactor), toRect: Bounds_);



            //// Color Declarations;

            var color = color_;

            var color2 = UIColor.Gray;// () // TODO:

            //// Bezier Drawing;

            var bezierPath = new UIBezierPath();

            bezierPath.MoveTo(new CGPoint(16 * xScalingFactor, 32 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(38 * xScalingFactor, 32 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(44 * xScalingFactor, 26 * yScalingFactor), controlPoint1: new CGPoint(38 * xScalingFactor, 32 * yScalingFactor), controlPoint2: new CGPoint(44 * xScalingFactor, 32 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(44 * xScalingFactor, 6 * yScalingFactor), controlPoint1: new CGPoint(44 * xScalingFactor, 22 * yScalingFactor), controlPoint2: new CGPoint(44 * xScalingFactor, 6 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(36 * xScalingFactor, 0 * yScalingFactor), controlPoint1: new CGPoint(44 * xScalingFactor, 6 * yScalingFactor), controlPoint2: new CGPoint(44 * xScalingFactor, 0 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(16 * xScalingFactor, 0 * yScalingFactor), controlPoint1: new CGPoint(32 * xScalingFactor, 0 * yScalingFactor), controlPoint2: new CGPoint(16 * xScalingFactor, 0 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(0 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(16 * xScalingFactor, 32 * yScalingFactor));

            bezierPath.ClosePath();

            color.SetFill();

            bezierPath.Fill();



            //// Bezier 2 Drawing;

            var bezier2Path = new UIBezierPath();

            bezier2Path.MoveTo(new CGPoint(20 * xScalingFactor, 10 * yScalingFactor));

            bezier2Path.AddLineTo(new CGPoint(34 * xScalingFactor, 22 * yScalingFactor));

            bezier2Path.AddLineTo(new CGPoint(20 * xScalingFactor, 10 * yScalingFactor));

            bezier2Path.ClosePath();

            UIColor.Gray.SetFill();

            bezier2Path.Fill();

            color2.SetStroke();

            bezier2Path.LineWidth = 2.5f * lineWidthScalingFactor;

            bezier2Path.Stroke();



            //// Bezier 3 Drawing;

            var bezier3Path = new UIBezierPath();

            bezier3Path.MoveTo(new CGPoint(20 * xScalingFactor, 22 * yScalingFactor));

            bezier3Path.AddLineTo(new CGPoint(34 * xScalingFactor, 10 * yScalingFactor));

            bezier3Path.AddLineTo(new CGPoint(20 * xScalingFactor, 22 * yScalingFactor));

            bezier3Path.ClosePath();

            UIColor.Red.SetFill();

            bezier3Path.Fill();

            color2.SetStroke();

            bezier3Path.LineWidth = 2.5f * lineWidthScalingFactor;

            bezier3Path.Stroke();

            endCenter();
        }

        public void drawShift(CGRect Bounds_, UIColor color_, bool withRect)
        {
            var factors = getFactors(new CGSize(38, (withRect ? 34 + 4 : 32)), toRect: Bounds_);

            var xScalingFactor = factors.xScalingFactor;

            var yScalingFactor = factors.yScalingFactor;

            var lineWidthScalingFactor = factors.lineWidthScalingFactor;



            centerShape(new CGSize(38 * xScalingFactor, (withRect ? 34 + 4 : 32) * yScalingFactor), toRect:Bounds_);



            //// Color Declarations;

            var color2 = color;


            //// Bezier Drawing;

            var bezierPath = new UIBezierPath();

            bezierPath.MoveTo(new CGPoint(28 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(38 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(38 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(19 * xScalingFactor, 0 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(0 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(0 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(10 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(10 * xScalingFactor, 28 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(14 * xScalingFactor, 32 * yScalingFactor), controlPoint1: new CGPoint(10 * xScalingFactor, 28 * yScalingFactor), controlPoint2: new CGPoint(10 * xScalingFactor, 32 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(24 * xScalingFactor, 32 * yScalingFactor), controlPoint1: new CGPoint(16 * xScalingFactor, 32 * yScalingFactor), controlPoint2: new CGPoint(24 * xScalingFactor, 32 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(28 * xScalingFactor, 28 * yScalingFactor), controlPoint1: new CGPoint(24 * xScalingFactor, 32 * yScalingFactor), controlPoint2: new CGPoint(28 * xScalingFactor, 32 * yScalingFactor));

            bezierPath.AddCurveToPoint(new CGPoint(28 * xScalingFactor, 18 * yScalingFactor), controlPoint1: new CGPoint(28 * xScalingFactor, 26 * yScalingFactor), controlPoint2: new CGPoint(28 * xScalingFactor, 18 * yScalingFactor));

            bezierPath.ClosePath();

            color2.SetFill();

            bezierPath.Fill();




            if(withRect ){
                //// Rectangle Drawing;

                var rectanglePath = UIBezierPath.FromRect(rect: new CGRect(10 * xScalingFactor, 34 * yScalingFactor, 18 * xScalingFactor, 4 * yScalingFactor));

                color2.SetFill();

                rectanglePath.Fill();

            }

            endCenter();

        }

        public void drawGlobe(CGRect Bounds_, UIColor color_)
        {
            var factors = getFactors(new CGSize(41, 40), toRect: Bounds_);

            var xScalingFactor = factors.xScalingFactor;

            var yScalingFactor = factors.yScalingFactor;

            var lineWidthScalingFactor = factors.lineWidthScalingFactor;



            centerShape(new CGSize(41 * xScalingFactor, 40 * yScalingFactor), toRect: Bounds_);



            //// Color Declarations;

            var color = color_;



            //var rectanglePath = UIBezierPath.FromRect(rect: new CGRect(10 * xScalingFactor, 34 * yScalingFactor, 18 * xScalingFactor, 4 * yScalingFactor));
            //color.SetFill();
            //rectanglePath.Fill();
            //endCenter();
            //return;


            //// Oval Drawing;

            var ovalPath = UIBezierPath.FromOval(inRect: new CGRect(0 * xScalingFactor, 0 * yScalingFactor, 40 * xScalingFactor, 40 * yScalingFactor));

            color.SetStroke();

            ovalPath.LineWidth = 1 * lineWidthScalingFactor;

            ovalPath.Stroke();



            //// Bezier Drawing;

            var bezierPath = new UIBezierPath();

            bezierPath.MoveTo(new CGPoint(20 * xScalingFactor, -0 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(20 * xScalingFactor, 40 * yScalingFactor));

            bezierPath.AddLineTo(new CGPoint(20 * xScalingFactor, -0 * yScalingFactor));

            bezierPath.ClosePath();

            color.SetStroke();

            bezierPath.LineWidth = 1 * lineWidthScalingFactor;

            bezierPath.Stroke();



            //// Bezier 2 Drawing;

            var bezier2Path = new UIBezierPath();

            bezier2Path.MoveTo(new CGPoint(0.5 * xScalingFactor, 19.5 * yScalingFactor));

            bezier2Path.AddLineTo(new CGPoint(39.5 * xScalingFactor, 19.5 * yScalingFactor));

            bezier2Path.AddLineTo(new CGPoint(0.5 * xScalingFactor, 19.5 * yScalingFactor));

            bezier2Path.ClosePath();

            color.SetStroke();

            bezier2Path.LineWidth = 1 * lineWidthScalingFactor;

            bezier2Path.Stroke();



            //// Bezier 3 Drawing;

            var bezier3Path = new UIBezierPath();

            bezier3Path.MoveTo(new CGPoint(21.63 * xScalingFactor, 0.42 * yScalingFactor));

            bezier3Path.AddCurveToPoint(new CGPoint(21.63 * xScalingFactor, 39.6 * yScalingFactor), controlPoint1: new CGPoint(21.63 * xScalingFactor, 0.42 * yScalingFactor), controlPoint2: new CGPoint(41 * xScalingFactor, 19 * yScalingFactor));

            bezier3Path.LineCapStyle = CGLineCap.Round;

            color.SetStroke();

            bezier3Path.LineWidth = 1 * lineWidthScalingFactor;

            bezier3Path.Stroke();



            //// Bezier 4 Drawing;

            var bezier4Path = new UIBezierPath();

            bezier4Path.MoveTo(new CGPoint(17.76 * xScalingFactor, 0.74 * yScalingFactor));

            bezier4Path.AddCurveToPoint(new CGPoint(18.72 * xScalingFactor, 39.6 * yScalingFactor), controlPoint1: new CGPoint(17.76 * xScalingFactor, 0.74 * yScalingFactor), controlPoint2: new CGPoint(-2.5 * xScalingFactor, 19.04 * yScalingFactor));

            bezier4Path.LineCapStyle = CGLineCap.Round;

            color.SetStroke();

            bezier4Path.LineWidth = 1 * lineWidthScalingFactor;

            bezier4Path.Stroke();



            //// Bezier 5 Drawing;

            var bezier5Path = new UIBezierPath();

            bezier5Path.MoveTo(new CGPoint(6 * xScalingFactor, 7 * yScalingFactor));

            bezier5Path.AddCurveToPoint(new CGPoint(34 * xScalingFactor, 7 * yScalingFactor), controlPoint1: new CGPoint(6 * xScalingFactor, 7 * yScalingFactor), controlPoint2: new CGPoint(19 * xScalingFactor, 21 * yScalingFactor));

            bezier5Path.LineCapStyle = CGLineCap.Round;

            color.SetStroke();

            bezier5Path.LineWidth = 1 * lineWidthScalingFactor;

            bezier5Path.Stroke();



            //// Bezier 6 Drawing;

            var bezier6Path = new UIBezierPath();

            bezier6Path.MoveTo(new CGPoint(6 * xScalingFactor, 33 * yScalingFactor));

            bezier6Path.AddCurveToPoint(new CGPoint(34 * xScalingFactor, 33 * yScalingFactor), controlPoint1: new CGPoint(6 * xScalingFactor, 33 * yScalingFactor), controlPoint2: new CGPoint(19 * xScalingFactor, 22 * yScalingFactor));

            bezier6Path.LineCapStyle = CGLineCap.Round;

            color.SetStroke();

            bezier6Path.LineWidth = 1 * lineWidthScalingFactor;

            bezier6Path.Stroke();



            endCenter();

        }   
    }
}
