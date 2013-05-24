using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Drawing;
using System;
using System.IO;
using MonoTouch.Foundation;

namespace Application
{
	public class NewSignatureViewController : UIViewController
	{
		public DetailedTabs Tabs { get; set; }
		
		public bool hasBeenSigned;

		// private PointF lastPoint;
		// private PointF currentPoint;
		// private UITouch touch;
		// private RectangleF rect;

		public EventHandler ClearSignature { get; set; }
		
		private SignableDocuments _mode;
		public SignableDocuments Mode { 
			get { return _mode; } 
			set { 
				_mode = value;
				switch (_mode)
				{
				case SignableDocuments.PrePlumbingCheck: this.Title = "Sign pre-plumbing check sheet"; break;
				case SignableDocuments.ServiceReport: this.Title = "Sign service report form"; break;
				case SignableDocuments.Receipt: this.Title = "Sign receipt"; break;
				}
			} 
		}
		
		private bool _signingMode;
		public bool SigningMode { get { return _signingMode; } set { _signingMode = value; }	}
		
		private UIWebView pdfView;
		
		public SignatureView Signature; // {	get { return _sig; } set { _sig = value; } }
		public UIWebView PDFView { get { return pdfView; } set { pdfView = value; } }
		
		public NewSignatureViewController (DetailedTabs tabs) : base ()
		{
			this.Tabs = tabs;			
			PDFView = new UIWebView(new RectangleF(0,20,703,748));
			Signature = new SignatureView(new RectangleF(0,496,743,150), this);
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			pdfView = new UIWebView(new RectangleF(0,40,703,448)); //(20,40,663,448));
			this.View.Add (pdfView);
			this.View.Add (Signature);
			
			// any additional setup after loading the view, typically from a nib.
			// sigCanvas = new UIView(new RectangleF(0,496,743,150));
			// sigCanvas.BackgroundColor = UIColor.LightGray;
			// sigCanvas.Alpha = 0.25f;
			// sigCanvas.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			
			// pdfView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			
						
			// Signature = new UIImageView(new RectangleF(0,496,743,150));
			// Signature.BackgroundColor = UIColor.LightGray;
			// Signature.Image = new UIImage();
			// Signature.Frame = new RectangleF(0, 0, sigCanvas.Frame.Size.Width, sigCanvas.Frame.Size.Height);
			// Signature.AutoresizingMask = UIViewAutoresizing.FlexibleWidth; // resizing an image: concern!

			// sigCanvas.AddSubview (Signature);
			// sigCanvas.AutoresizingMask = UIViewAutoresizing.FlexibleWidth; // resizing an image: concern!
			// mouseMoved = 0;
			// rect = new RectangleF(0,496,743,150); // (0,0, 698, 150); // sigCanvas.Frame.Size.Width, sigCanvas.Frame.Size.Height);
		}

		/*
		public override void TouchesBegan (NSSet touches, UIEvent evt)
		{
			if (_signingMode) {
				mouseSwiped = false;
				touch = (UITouch)touches.AnyObject;
				lastPoint = touch.LocationInView (Signature); // (sigCanvas);

				if ((touch.GestureRecognizers != null) && (touch.GestureRecognizers.Length > 0))
				{
					foreach (var tmpG in touch.GestureRecognizers)
						tmpG.CancelsTouchesInView = false;
				}
			}
		}

		// this allowed to find out why the signatures were not captured correctly
		public override void TouchesCancelled (NSSet touches, UIEvent evt)
		{
			// Console.WriteLine(String.Format ("Event fired: TouchesCanceled: {0}", currentPoint.ToString ()));
			base.TouchesCancelled (touches, evt);
		}
		
		public override void TouchesMoved (NSSet touches, UIEvent evt)
		{
			// Console.WriteLine(String.Format ("Event fired: TouchesMoved: {0}, touches count = {1}, eventType: {2}, event subtype: {3}, evt.Description = {4}", 
			//                                  currentPoint.ToString (), touches.Count, evt.Type, evt.Subtype, evt.Description, evt.DebugDescription));	
			if (_signingMode) {
				mouseSwiped = true;
				
				touch = (UITouch) evt.AllTouches.AnyObject;
				currentPoint = touch.LocationInView (Signature); // (sigCanvas);

				if ((touch.GestureRecognizers != null) && (touch.GestureRecognizers.Length > 0))
				{
					foreach (var tmpG in touch.GestureRecognizers)
						tmpG.CancelsTouchesInView = false;
				}
		
				UIGraphics.BeginImageContext (Signature.Frame.Size);

				Signature.Image.Draw (rect); // ?
				using (CGContext _cgc = UIGraphics.GetCurrentContext () )
				{
					// _cgc.DrawImage (rect, Signature.Image.CGImage);


					_cgc.SetLineCap(CGLineCap.Round);
					_cgc.SetLineWidth (5);
					_cgc.SetStrokeColor (0,0,0,1);

					_cgc.BeginPath ();
					_cgc.MoveTo (lastPoint.X, lastPoint.Y);
					_cgc.AddLineToPoint (currentPoint.X, currentPoint.Y);
					_cgc.StrokePath ();

					Signature.Image = UIGraphics.GetImageFromCurrentImageContext ();
				}
				UIGraphics.EndImageContext ();

				lastPoint = currentPoint;
				// mouseMoved ++;
				// if (mouseMoved == 10) { mouseMoved = 0; }
			}
		}
		
		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			if (_signingMode) {
				// Console.WriteLine("Event fired: TouchesEnded: "+ lastPoint.ToString());


				touch = (UITouch) evt.AllTouches.AnyObject;	// (UITouch)touches.AnyObject;
				if (touch.TapCount > 3)
				{
//					// _sig.Image = new UIImage();
//					// Tabs._navWorkflow.RightButton.Enabled = false;
					return;
				}
				
				if (!mouseSwiped) 
				{
					UIGraphics.BeginImageContext (Signature.Frame.Size);
					
					Signature.Image.Draw (rect); // ?
					using (CGContext _cgc = UIGraphics.GetCurrentContext () )
					{
						// _cgc.DrawImage (rect, Signature.Image.CGImage);
						
						_cgc.SetLineCap(CGLineCap.Round);
						_cgc.SetLineWidth (5);
						_cgc.SetStrokeColor (0,0,0,1);
						
						_cgc.BeginPath ();
						_cgc.MoveTo (lastPoint.X, lastPoint.Y);
						_cgc.AddLineToPoint (currentPoint.X, currentPoint.Y);
						_cgc.StrokePath ();
						
						Signature.Image = UIGraphics.GetImageFromCurrentImageContext ();						
					}
					UIGraphics.EndImageContext ();
				}
			}
			hasBeenSigned = true;
		}
		*/

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);

			switch (_mode)
			{
			// case SignableDocuments.PrePlumbingCheck: { Tabs._navWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.SigningPrePlumbingCheck); break; }
			// case SignableDocuments.ServiceReport: { Tabs._navWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.SigningServiceCallReport); break; }
			default: return;
			}
			
			
			// _tabs._navWorkflow.Title = this.Title;
			// _tabs._navWorkflow.TabBarItem.Image = this.TabBarItem.Image;
			// _tabs._navWorkflow.SetToolbarHidden (true, animated);
		}
		
		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return true;
		}
	}

	public class SignatureView : UIView
	{		
		NewSignatureViewController nsvc;

		public void Clear ()
		{
			drawPath.Dispose ();
			drawPath = new CGPath ();
			fingerDraw = false;
			SetNeedsDisplay ();			
		}

		public SignatureView (RectangleF frame, NewSignatureViewController root) : base(frame)
		{
			nsvc = root;
			this.drawPath = new CGPath ();
			this.BackgroundColor = UIColor.Yellow;
			this.MultipleTouchEnabled = false;
		}
				
		private PointF touchLocation;
		private PointF prevTouchLocation;
		private CGPath drawPath;
		private bool fingerDraw;

		public override void TouchesBegan (MonoTouch.Foundation.NSSet touches, UIEvent evt)
		{
			if (nsvc.SigningMode == true)
			{
				base.TouchesBegan (touches, evt);
				
				UITouch touch = touches.AnyObject as UITouch;
				if ((touch.GestureRecognizers != null) && (touch.GestureRecognizers.Length > 0))
				{
					foreach (var tmpG in touch.GestureRecognizers)
						tmpG.CancelsTouchesInView = false;
				}

				this.fingerDraw = true;
				this.touchLocation = touch.LocationInView (this);
				this.prevTouchLocation = touch.PreviousLocationInView (this);
				this.SetNeedsDisplay ();	
			}
		}
		
		public override void TouchesMoved (MonoTouch.Foundation.NSSet touches, UIEvent evt)
		{
			if (nsvc.SigningMode == true)
			{
				base.TouchesMoved (touches, evt);
				
				UITouch touch = touches.AnyObject as UITouch;
				if ((touch.GestureRecognizers != null) && (touch.GestureRecognizers.Length > 0))
				{
					foreach (var tmpG in touch.GestureRecognizers)
						tmpG.CancelsTouchesInView = false;
				}

				this.touchLocation = touch.LocationInView (this);
				this.prevTouchLocation = touch.PreviousLocationInView (this);
				this.SetNeedsDisplay ();
				this.nsvc.hasBeenSigned = true;
			}
		}

		public override void TouchesCancelled (NSSet touches, UIEvent evt)
		{
			base.TouchesCancelled (touches, evt);
			// Console.WriteLine (String.Format ("Touches cancelled event fired."));
		}
		
		public UIImage GetDrawingImage ()
		{
			UIImage returnImg = null;

			UIGraphics.BeginImageContext (this.Bounds.Size);			
			using (CGContext context = UIGraphics.GetCurrentContext()) 
			{
				context.SetStrokeColor (UIColor.Black.CGColor);
				context.SetLineWidth (5f);
				context.SetLineJoin (CGLineJoin.Round);
				context.SetLineCap (CGLineCap.Round);
				context.AddPath (this.drawPath);
				context.DrawPath (CGPathDrawingMode.Stroke);
				returnImg = UIGraphics.GetImageFromCurrentImageContext ();
			}
			UIGraphics.EndImageContext ();
			return returnImg;
		}
		
		public override void Draw (RectangleF rect)
		{
			base.Draw (rect);
			
			if (this.fingerDraw) 
			{
				using (CGContext context = UIGraphics.GetCurrentContext()) 
				{
					context.SetStrokeColor (UIColor.Black.CGColor);
					context.SetLineWidth (5f);
					context.SetLineJoin (CGLineJoin.Round);
					context.SetLineCap (CGLineCap.Round);

					this.drawPath.MoveToPoint (this.prevTouchLocation);
					this.drawPath.AddLineToPoint (this.touchLocation);
					context.AddPath (this.drawPath);
					context.DrawPath (CGPathDrawingMode.Stroke);
				}
			}   
		}
	}
}

