using System;
using System.Collections.Generic;
using UIKit;
using CoreGraphics;

namespace KeyboardExtension
{
    using Foundation;
    using ObjCRuntime;
    using System.Runtime.InteropServices;
    using CGFloat = nfloat;

    public class KeyboardConnector : KeyboardKeyBackground
    {
        UIView start;
        UIView end;
        public Direction startDir;
        public Direction endDir;

        Connectable startConnectable;
        Connectable endConnectable;
        Tuple<CGPoint, CGPoint> convertedStartPoints;
        Tuple<CGPoint, CGPoint> convertedEndPoints;
    
        CGPoint offset;

        // TODO: until bug is fixed, make sure start/end and startConnectable/endConnectable are the same object;
        public KeyboardConnector(CGFloat cornerRadius, CGFloat underOffset, 
            UIView s, UIView e,
            Connectable sC, Connectable eC,
            Direction startDirection, Direction endDirection) :
                base(cornerRadius: cornerRadius, underOffset: underOffset)
        {
                start = s;
                end = e;
                startDir = startDirection;
                endDir = endDirection;
                startConnectable = sC;
                endConnectable = eC;

                offset = CGPoint.Empty;
        }

        public KeyboardConnector(NSCoder coder) :
            base(coder)
        {
            throw new ApplicationException("NSCoding not supported");
        }

        public override void MovedToSuperview()
        {
            base.MovedToSuperview();
            this.SetNeedsLayout();
        }
        public override void LayoutSubviews()
        {
            this.resizeFrame();
            base.LayoutSubviews();
        }

        public void generateConvertedPoints()
        {
            if(this.startConnectable == null || this.endConnectable == null )
            {
                return;
            }
            var superview = this.Superview;
            if(superview!=null)
            {
                var startPoints = this.startConnectable.attachmentPoints(this.startDir);
                var endPoints = this.endConnectable.attachmentPoints(this.endDir);

                this.convertedStartPoints = new Tuple<CGPoint, CGPoint>(
                    superview.ConvertPointFromView(startPoints.Item1, fromView: this.start),
                    superview.ConvertPointFromView(startPoints.Item2, fromView: this.start));

                this.convertedEndPoints = new Tuple<CGPoint, CGPoint>(
                    superview.ConvertPointFromView(endPoints.Item1, fromView: this.end),
                    superview.ConvertPointFromView(endPoints.Item2, fromView: this.end));
                
            }
        }

        public void resizeFrame()
        {
            generateConvertedPoints();


            CGFloat buffer =  32;
            this.offset = new CGPoint(buffer / 2, buffer / 2);


            var minX = Math.Min(convertedStartPoints.Item1.X, Math.Min(convertedStartPoints.Item2.X, Math.Min(convertedEndPoints.Item1.X, convertedEndPoints.Item2.X)));                                                     
            var minY = Math.Min(convertedStartPoints.Item1.Y, Math.Min(convertedStartPoints.Item2.Y, Math.Min(convertedEndPoints.Item1.Y, convertedEndPoints.Item2.Y)));                                                     
            var maxX = Math.Max(convertedStartPoints.Item1.X, Math.Min(convertedStartPoints.Item2.X, Math.Min(convertedEndPoints.Item1.X, convertedEndPoints.Item2.X)));                                                     
            var maxY = Math.Max(convertedStartPoints.Item1.Y, Math.Min(convertedStartPoints.Item2.Y, Math.Min(convertedEndPoints.Item1.Y, convertedEndPoints.Item2.Y)));

            var width = maxX - minX;

            var height = maxY - minY;



            this.Frame = new CGRect(minX - buffer / 2, minY - buffer / 2 , width + buffer, height + buffer);

        }

        [DllImport(Constants.CoreGraphicsLibrary)]
        extern static IntPtr CGPathCreateMutable();
        private CGPath CreateMutablePath()
        {
            var pathHandle = CGPathCreateMutable();
            CGPath path = new CGPath(pathHandle);
            return path;
        }

        public override void generatePointsForDrawing(CGRect Bounds)
        {
            if(this.startConnectable == null || this.endConnectable == null )
            {
                return;
            }

            //////////////////
            // prepare data //
            //////////////////

            var startPoints = this.startConnectable.attachmentPoints(this.startDir);
            var endPoints = this.endConnectable.attachmentPoints(this.endDir);


            var myConvertedStartPoints = new Tuple<CGPoint, CGPoint>(
                this.ConvertPointFromView(startPoints.Item1, fromView: this.start),
                this.ConvertPointFromView(startPoints.Item2, fromView: this.start));

            var myConvertedEndPoints = new Tuple<CGPoint, CGPoint> (
                this.ConvertPointFromView(endPoints.Item1, fromView: this.end),
                this.ConvertPointFromView(endPoints.Item2, fromView: this.end));


            if(this.startDir == this.endDir )
            {
                var tempPoint = myConvertedStartPoints.Item1;
                myConvertedStartPoints =new Tuple<CGPoint, CGPoint>(
                    myConvertedStartPoints.Item2,
                    tempPoint); 

            }

            var path = CreateMutablePath();// new CGPath();

            path.MoveToPoint(myConvertedStartPoints.Item1.X, myConvertedStartPoints.Item1.Y);
            path.AddLineToPoint(myConvertedEndPoints.Item2.X, myConvertedEndPoints.Item2.Y);
            path.AddLineToPoint(myConvertedEndPoints.Item1.X, myConvertedEndPoints.Item1.Y);  
            path.AddLineToPoint(myConvertedStartPoints.Item2.X, myConvertedStartPoints.Item2.Y);
            path.CloseSubpath();


            // for now, assuming axis-aligned attachment points;
            var isVertical = (this.startDir == Direction.Up || this.startDir == Direction.Down) && 
                (this.endDir == Direction.Up || this.endDir == Direction.Down);

                
            CGFloat midpoint;
            if( isVertical )
            {
                midpoint = myConvertedStartPoints.Item1.Y + (myConvertedEndPoints.Item2.Y - myConvertedStartPoints.Item1.Y) / 2;
            }
            else
            {
                midpoint = myConvertedStartPoints.Item1.X + (myConvertedEndPoints.Item2.X - myConvertedStartPoints.Item1.X) / 2;
            }

            var bezierPath = new UIBezierPath();
            var fillPath = new UIBezierPath();

            var currentEdgePath = new UIBezierPath();

            List<UIBezierPath> edgePaths = new List<UIBezierPath>();

            bezierPath.MoveTo(myConvertedStartPoints.Item1);
            bezierPath.AddCurveToPoint(
                myConvertedEndPoints.Item2,
                controlPoint1: (isVertical ?
                    new CGPoint(myConvertedStartPoints.Item1.X, midpoint) :
                    new CGPoint(midpoint, myConvertedStartPoints.Item1.Y)),
                controlPoint2: (isVertical ?
                    new CGPoint(myConvertedEndPoints.Item2.X, midpoint) :
                    new CGPoint(midpoint, myConvertedEndPoints.Item2.Y)));
            //bezierPath.ApplyTransform(CGAffineTransform.MakeTranslation(0, -this.underOffset)); // <<<

            currentEdgePath = new UIBezierPath();
            currentEdgePath.MoveTo(myConvertedStartPoints.Item1);
            currentEdgePath.AddCurveToPoint(
                myConvertedEndPoints.Item2,
                controlPoint1: (isVertical ?
                    new CGPoint(myConvertedStartPoints.Item1.X, midpoint) :
                    new CGPoint(midpoint, myConvertedStartPoints.Item1.Y)),
                controlPoint2: (isVertical ?
                    new CGPoint(myConvertedEndPoints.Item2.X, midpoint) :
                    new CGPoint(midpoint, myConvertedEndPoints.Item2.Y)));
            currentEdgePath.ApplyTransform(CGAffineTransform.MakeTranslation(0, -this.underOffset));
            edgePaths.Add(currentEdgePath);
            fillPath.AppendPath(currentEdgePath);

            bezierPath.AddLineTo(myConvertedEndPoints.Item1);
            bezierPath.AddCurveToPoint(
                myConvertedStartPoints.Item2,
                controlPoint1: (isVertical ?
                    new CGPoint(myConvertedEndPoints.Item1.X, midpoint) :
                    new CGPoint(midpoint, myConvertedEndPoints.Item1.Y)),
                controlPoint2: (isVertical ?
                    new CGPoint(myConvertedStartPoints.Item2.X, midpoint) :
                    new CGPoint(midpoint, myConvertedStartPoints.Item2.Y)));
            //bezierPath.ApplyTransform(CGAffineTransform.MakeTranslation(0, -this.underOffset)); // <<<
            bezierPath.AddLineTo(myConvertedStartPoints.Item1);


            currentEdgePath = new UIBezierPath();
            currentEdgePath.MoveTo(myConvertedEndPoints.Item1);
            currentEdgePath.AddCurveToPoint(
                myConvertedStartPoints.Item2,
                controlPoint1: (isVertical ?
                    new CGPoint(myConvertedEndPoints.Item1.X, midpoint) :
                    new CGPoint(midpoint, myConvertedEndPoints.Item1.Y)),
                controlPoint2: (isVertical ?
                    new CGPoint(myConvertedStartPoints.Item2.X, midpoint) :
                    new CGPoint(midpoint, myConvertedStartPoints.Item2.Y)));
            currentEdgePath.ApplyTransform(CGAffineTransform.MakeTranslation(0, -this.underOffset));
            edgePaths.Add(currentEdgePath);
            fillPath.AppendPath(currentEdgePath);

            bezierPath.AddLineTo(myConvertedStartPoints.Item1);
            //fillPath.AddLineTo(myConvertedStartPoints.Item1);

            bezierPath.ClosePath();
            nfloat delta = 0;
            bezierPath.ApplyTransform(CGAffineTransform.MakeTranslation(0, -this.underOffset + delta));
            fillPath.ClosePath();

            this.fillPath = bezierPath;
            this.edgePaths = edgePaths.ToArray();
        }
    }
}
