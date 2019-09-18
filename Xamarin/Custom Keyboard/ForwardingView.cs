using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace KeyboardExtension
{
    using CGFloat = nfloat;
    public class ForwardingView : UIView
    {
        SafeDict<UITouch, UIView> touchToView;
        public ForwardingView(CGRect frame) :
            base(frame)
        {
            this.touchToView = new SafeDict<UITouch, UIView>();
            this.ContentMode = UIViewContentMode.Redraw;
            this.MultipleTouchEnabled = true;
            this.UserInteractionEnabled = true;
            this.Opaque = false;
        }
        public ForwardingView(NSCoder coder)
        {
            throw new ApplicationException("NSCoding not supported");

        }

        // Why have this useless drawRect? Well, if(we just set the backgroundColor to clearColor,
        // then some weird optimization happens on UIKit's side where tapping down on a transparent pixel will
        // not actually recognize the touch. Having a manual drawRect fixes this behavior, even though it doesn't
        // actually do anything.
        public override void Draw(CGRect rect)
        { }

        public override UIView HitTest(CGPoint point, UIEvent uievent)
        {
            if (this.Hidden || this.Alpha == 0 || !this.UserInteractionEnabled)
            {
                return null;
            }
            else
            {
                return (this.Bounds.Contains(point) ? this : null);
            }
        }

        void handleControl(UIView view, UIControlEvent controlEvent)
        {
            var control = view as UIControl;

            if (control != null)
            {
                var targets = control.AllTargets;
                foreach (var target in targets)
                {
                    var actions = control.GetActions(target, controlEvent);
                    if ((actions != null)
                    ) {
                        foreach (var action in actions)
                        {
                            var selectorString = action;
                            var selector = new ObjCRuntime.Selector(selectorString);
                            control.SendAction(selector, target, null);
                        }
                    }
                }
            }
        }

        // TODO: there's a bit of "stickiness" to Apple's implementation
        UIView findNearestView(CGPoint position)
        {
            if (!this.Bounds.Contains(position))
            {
                return null;
            }

            UIView closest = null;
            CGFloat closestDist = nfloat.MaxValue;


            foreach (var anyView in this.Subviews)
            {
                var view = anyView;
                if (view.Hidden)
                {
                    continue;
                }

                view.Alpha = 1;

                var distance = distanceBetween(view.Frame, position);

                if (closest != null)
                {
                    if (distance < closestDist)
                    {
                        closest = view;
                        closestDist = distance;
                    }
                }
                else
                {
                    closest = view;
                    closestDist = distance;
                }
            }

            if (closest != null)
            {
                return closest;
            }
            else
            {
                return null;
            }
        }

        // http://stackoverflow.com/questions/3552108/finding-closest-object-to-cgpoint b/c I'm lazy
        CGFloat distanceBetween(CGRect rect, CGPoint point)
        {
            if (rect.Contains(point))
            {
                return 0;
            }

            var closest = rect.Location;

            if ((rect.Location.X + rect.Size.Width < point.X))
            {
                closest.X += rect.Size.Width;
            }
            else
            if ((point.X > rect.Location.X))
            {
                closest.X = point.X;
            }

            if ((rect.Location.Y + rect.Size.Height < point.Y))
            {
                closest.Y += rect.Size.Height;
            }
            else
            if ((point.Y > rect.Location.Y))
            {
                closest.Y = point.Y;
            }

            var a = Math.Pow((double)(closest.Y - point.Y), 2);
            var b = Math.Pow((double)(closest.X - point.X), 2);
            return (CGFloat)(Math.Sqrt(a + b));
        }

        // reset tracked views without cancelling current touch
        public void resetTrackedViews()
        {
            foreach (var view in this.touchToView.Values)
            {
                this.handleControl(view, UIControlEvent.TouchCancel);
            }
            this.touchToView.Clear();
        }

        bool ownView(UITouch newTouch, UIView viewToOwn)
        {
            var foundView = false;

            if (viewToOwn != null)
            {
                foreach (var ttv in this.touchToView)
                {
                    if (viewToOwn == ttv.Value)
                    {
                        if (ttv.Key == newTouch)
                        {
                            break;
                        }
                        else
                        {
                            this.touchToView[ttv.Key] = null;
                            foundView = true;
                        }
                        break;
                    }
                }
            }

            this.touchToView[newTouch] = viewToOwn;
            return foundView;
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            foreach (var _touch in touches)
            {
                UITouch touch = _touch as UITouch;
                if (touch == null)
                {
                    continue;
                }
                var position = touch.LocationInView(this);
                var view = findNearestView(position);

                var viewChangedOwnership = this.ownView(touch, viewToOwn: view);

                if (!viewChangedOwnership)
                {
                    this.handleControl(view, UIControlEvent.TouchDown);

                    if (touch.TapCount > 1)
                    {
                        // two events, I think this is the correct behavior but I have not tested with an actual UIControl
                        this.handleControl(view, controlEvent: UIControlEvent.TouchDownRepeat);
                    }
                }
            }
        }
        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            foreach (var _touch in touches)
            {
                UITouch touch = _touch as UITouch;
                if (touch == null)
                {
                    continue;
                }
                var position = touch.LocationInView(this);

                var oldView = this.touchToView[touch];
                var newView = findNearestView(position);

                if (oldView != newView)
                {
                    this.handleControl(oldView, controlEvent: UIControlEvent.TouchDragExit);

                    var viewChangedOwnership = this.ownView(touch, viewToOwn: newView);

                    if (!viewChangedOwnership)
                    {
                        this.handleControl(newView, controlEvent: UIControlEvent.TouchDragEnter);
                    }
                    else
                    {
                        this.handleControl(newView, controlEvent: UIControlEvent.TouchDragInside);
                    }
                }
                else
                {
                    this.handleControl(oldView, controlEvent: UIControlEvent.TouchDragInside);
                }
            }
        }
        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            foreach (var _touch in touches)
            {
                UITouch touch = _touch as UITouch;
                if (touch == null)
                {
                    continue;
                }
                var view = this.touchToView[touch];

                var touchPosition = touch.LocationInView(this);

                if (this.Bounds.Contains(touchPosition))
                {
                    this.handleControl(view, controlEvent: UIControlEvent.TouchUpInside);
                }
                else
                {
                    this.handleControl(view, controlEvent: UIControlEvent.TouchCancel);
                }

                this.touchToView[touch] = null;
            }
        }
        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            foreach (var _touch in touches)
            {
                UITouch touch = _touch as UITouch;
                if (touch == null)
                {
                    continue;
                }
                var view = this.touchToView[touch];


                this.handleControl(view, controlEvent: UIControlEvent.TouchCancel);


                this.touchToView[touch] = null;
            }
        }
    }
}
