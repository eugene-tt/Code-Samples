using CoreGraphics;
using System;
using System.Collections.Generic;
using UIKit;

namespace KeyboardExtension

{
    using Foundation;
    using System.Linq;
    using CGFloat = nfloat;
    public interface Connectable

    {
        Tuple<CGPoint, CGPoint> attachmentPoints(Direction direction);
        Direction? attachmentDirection();
        void attach(Direction? direction);// call with null to detach;

    }

    public class KeyboardKeyBackground : UIView, Connectable
    {
        public UIBezierPath fillPath;
        public UIBezierPath underPath;
        public UIBezierPath[] edgePaths;

        // do not set this manually;

        public CGFloat cornerRadius;
        public CGFloat underOffset;
        public List<CGPoint> startingPoints;
        public List<Tuple<CGPoint, CGPoint>> segmentPoints;
        public List<CGPoint> arcCenters;
        public List<CGFloat> arcStartingAngles;

        public bool dirty;

        Direction? _attached;
        public Direction? attached
        {
            get
            {
                return _attached;
            }
            set
            {
                _attached = value;
                this.dirty = true;
                this.SetNeedsLayout();
            }
        }

        bool _hideDirectionIsOpposite;
        public bool hideDirectionIsOpposite
        {
            get
            {
                return _hideDirectionIsOpposite;
            }
            set
            {
                this.dirty = true;

                this.SetNeedsLayout();

            }
        }


        public Tuple<CGPoint, CGPoint> attachmentPoints(Direction direction)
        { 
            var returnValue = new Tuple<CGPoint, CGPoint>(
                    this.segmentPoints[(int)direction.clockwise()].Item1,
                    this.segmentPoints[(int)direction.counterclockwise()].Item2);

        
                return returnValue;
        }

        public Direction? attachmentDirection()
        {
            return this.attached;
        }
    
        public void attach(Direction? direction)
        {
            this.attached = direction;
        }


        public KeyboardKeyBackground(CGFloat cornerRadius, CGFloat underOffset) :
            base(frame: CGRect.Empty)
        {
            attached = null;
            hideDirectionIsOpposite = false;
            dirty = false;

            startingPoints = new List<CGPoint>();
            segmentPoints = new List<Tuple<CGPoint, CGPoint>>();
            arcCenters = new List<CGPoint>();
            arcStartingAngles = new List<CGFloat>();

            startingPoints.Capacity = (4);
            segmentPoints.Capacity = (4);
            arcCenters.Capacity = (4);
            arcStartingAngles.Capacity = (4);

            for (int i=0; i<4; i++)
            { 
                startingPoints.Add(CGPoint.Empty);
                segmentPoints.Add(new Tuple<CGPoint, CGPoint>(CGPoint.Empty, CGPoint.Empty));
                arcCenters.Add(CGPoint.Empty);
                arcStartingAngles.Add(0);
            }

            this.cornerRadius = cornerRadius;
            this.underOffset = underOffset;

            this.UserInteractionEnabled = false;
        }

        public KeyboardKeyBackground(NSCoder coder)
        {
            throw new ApplicationException("NSCoding not supported");
        }

        CGRect? oldBounds;
        public override void LayoutSubviews()
        {
            if (!this.dirty)
            {
                if (this.Bounds.Width == 0 || this.Bounds.Height == 0)
                {
                    return;

                }
                if (oldBounds != null && this.Bounds.Equals(oldBounds))
                {
                    return;

                }
            }
            oldBounds = this.Bounds;
            base.LayoutSubviews();
            this.generatePointsForDrawing(this.Bounds);
            this.dirty = false;
        }

        CGFloat floatPi = (CGFloat)(Math.PI);
        CGFloat floatPiDiv2 = (CGFloat)(Math.PI / 2.0);
        CGFloat floatPiDivNeg2 = (CGFloat) (- (Math.PI / 2.0));

        public virtual void generatePointsForDrawing(CGRect Bounds)
        {
            var segmentWidth = Bounds.Width;
            var segmentHeight = Bounds.Height - (CGFloat)(underOffset);

            // base, untranslated corner points;
            this.startingPoints[0] = new CGPoint(0, segmentHeight);
            this.startingPoints[1] = new CGPoint(0, 0);
            this.startingPoints[2] = new CGPoint(segmentWidth, 0);
            this.startingPoints[3] = new CGPoint(segmentWidth, segmentHeight);

            this.arcStartingAngles[0] = floatPiDiv2;
            this.arcStartingAngles[2] = floatPiDivNeg2;
            this.arcStartingAngles[1] = floatPi;

            this.arcStartingAngles[3] = 0;

            //// actual coordinates for each edge, including translation;
            //this.segmentPoints.removeAll(keepCapacity: true);
            //
            //// actual coordinates for arc centers for each corner;
            //this.arcCenters.removeAll(keepCapacity: true);
            //
            //this.arcStartingAngles.removeAll(keepCapacity: true);


            for (int i = 0; i < this.startingPoints.Count; i++)
            {
                var currentPoint = this.startingPoints[i];
                var nextPoint = this.startingPoints[(i + 1) % this.startingPoints.Count];

                CGFloat floatXCorner = 0;
                CGFloat floatYCorner = 0;

                if (i == 1)
                {
                    floatXCorner = cornerRadius;
                }
                else
                if (i == 3)
                {
                    floatXCorner = -cornerRadius;
                }


                if ((i == 0))
                {
                    floatYCorner = -cornerRadius;
                }
                else if ((i == 2))
                {
                    floatYCorner = cornerRadius;
                }

                var p0 = new CGPoint(
                    currentPoint.X + (floatXCorner),
                    currentPoint.Y + underOffset + (floatYCorner));

                var p1 = new CGPoint(
                    nextPoint.X - (floatXCorner),
                    nextPoint.Y + underOffset - (floatYCorner));


                this.segmentPoints[i] = new Tuple<CoreGraphics.CGPoint, CoreGraphics.CGPoint>(p0, p1);

                var c = new CGPoint(
                    p0.X - (floatYCorner),
                    p0.Y + (floatXCorner));


                this.arcCenters[i] = c;

            }

            // order of edge drawing: left edge, down edge, right edge, up edge;

            // We need to have separate paths for all the edges so we can toggle them as needed.
            // Unfortunately, it doesn't seem possible to assemble the connected fill path;
            // by simply using CGPathAddPath, since it closes all the subpaths, so we have to;
            // duplicate the code a little bit.

            var fillPath = new UIBezierPath();
            var edgePaths = new List<UIBezierPath>();
            CGPoint? prevPoint = null;
            
            for (int i = 0; i < 4; i++)
            {
                UIBezierPath edgePath = null;
                var segmentPoint = this.segmentPoints[i];
            
                if(this.attached != null && (this.hideDirectionIsOpposite? ((int)this.attached != i) : ((int)this.attached == i)))
                {
                    // do nothing;
                    // TODO: quick hack;
                    if(!this.hideDirectionIsOpposite )
                    {
                        continue;
                    }
                }
                else 
                {
                    edgePath = new UIBezierPath();

                
                    // TODO: figure out if(this is ncessary;
                    if (prevPoint == null || !prevPoint.HasValue)
                    {
                        prevPoint = segmentPoint.Item1;
                        fillPath.MoveTo(prevPoint.Value);
                    }

                    fillPath.AddLineTo(new CGPoint(segmentPoint.Item1.X, segmentPoint.Item1.Y));
                    fillPath.AddLineTo(segmentPoint.Item2);
                
                    edgePath.MoveTo(segmentPoint.Item1);
                    edgePath.AddLineTo(segmentPoint.Item2);

                    prevPoint = segmentPoint.Item2;
                }



                int attachedval = -1;
                if(this.attached.HasValue)
                {
                    attachedval = (int)this.attached.Value;
                }

                //let shouldDrawArcInOppositeMode = (self.attached != nil ? (self.attached!.rawValue == i) || (self.attached!.rawValue == ((i + 1) % 4)) : false)
                var shouldDrawArcInOppositeMode = (this.attached.HasValue ? ((int)this.attached == i) || (attachedval == ((i + 1) % 4)) : false);



                if (this.attached.HasValue && (this.hideDirectionIsOpposite ? !shouldDrawArcInOppositeMode : attachedval == ((i + 1) % 4)))
                {
                    // do nothing;

                }
                else
                {
                    edgePath = (edgePath == null? new UIBezierPath() : edgePath);

                
                    if(prevPoint == null || !prevPoint.HasValue)
                    {
                            prevPoint = segmentPoint.Item2;
                            fillPath.MoveTo(prevPoint.Value);
                    }

                    var startAngle1 = this.arcStartingAngles[(i + 1) % 4];
                    var endAngle1 = startAngle1 + floatPiDiv2;
                    var arcCenter = this.arcCenters[(i + 1) % 4];


                    fillPath.AddLineTo(prevPoint.Value);
                    fillPath.AddArc(arcCenter, radius: this.cornerRadius, startAngle: startAngle1, endAngle: endAngle1, clockWise: true); 
                    if(edgePath!=null)
                    {
                        edgePath.MoveTo(prevPoint.Value);
                        edgePath.AddArc(arcCenter, radius: this.cornerRadius, startAngle: startAngle1, endAngle: endAngle1, clockWise: true);
                    }

                    prevPoint = this.segmentPoints[(i + 1) % 4].Item1;
                }
                if (edgePath != null)
                {
                    edgePath.ApplyTransform(CGAffineTransform.MakeTranslation(0, -this.underOffset));
                    edgePaths.Add(edgePath);
                }            
            }
        
            fillPath.ClosePath();
            fillPath.ApplyTransform(CGAffineTransform.MakeTranslation(0, -this.underOffset));

            Func< UIBezierPath> underPathProc =  () =>
            {
                var underPath = new UIBezierPath();            
                underPath.MoveTo(this.segmentPoints[2].Item2);



                var startAngle = this.arcStartingAngles[3];
                var endAngle = startAngle + (CGFloat)(Math.PI/2.0);
                underPath.AddArc(this.arcCenters[3], radius: (CGFloat)(this.cornerRadius), startAngle: startAngle, endAngle: endAngle, clockWise: true);

                underPath.AddLineTo(this.segmentPoints[3].Item2);


                startAngle = this.arcStartingAngles[0];
                endAngle = startAngle + (CGFloat)(Math.PI/2.0);
                underPath.AddArc(this.arcCenters[0], radius: (CGFloat)(this.cornerRadius), startAngle: startAngle, endAngle: endAngle, clockWise: true);

                underPath.AddLineTo(new CGPoint(this.segmentPoints[0].Item1.X, this.segmentPoints[0].Item1.Y - this.underOffset));

                startAngle = this.arcStartingAngles[1];
                endAngle = startAngle - (CGFloat)(Math.PI/2.0);
                underPath.AddArc(new CGPoint(this.arcCenters[0].X, this.arcCenters[0].Y - this.underOffset), radius: (CGFloat)(this.cornerRadius), startAngle: startAngle, endAngle: endAngle, clockWise: false);

                underPath.AddLineTo(new CGPoint(this.segmentPoints[2].Item2.X - this.cornerRadius, this.segmentPoints[2].Item2.Y + this.cornerRadius - this.underOffset));

                startAngle = this.arcStartingAngles[0];
                endAngle = startAngle - (CGFloat)(Math.PI/2.0);
                underPath.AddArc(new CGPoint(this.arcCenters[3].X, this.arcCenters[3].Y - this.underOffset), radius: (CGFloat)(this.cornerRadius), startAngle: startAngle, endAngle: endAngle, clockWise: false);

                underPath.ClosePath();
            
                return underPath;
            };

        
            this.fillPath = fillPath;
            this.edgePaths = edgePaths.ToArray();
            this.underPath = underPathProc();
        }
    }
}
