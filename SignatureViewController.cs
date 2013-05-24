using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Drawing;
using System;
using System.IO;
using MonoTouch.Foundation;

namespace Application
{	
	public partial class SignatureViewController : UIViewController
	{
		DetailedTabs _tabs;
		
		public bool hasBeenSigned;
		
		private UIImageView _sig;
		private PointF lastPoint;
		private int mouseMoved;
		private bool mouseSwiped;
		
		
		private SignableDocuments _mode;
		public SignableDocuments Mode { 
			get { return _mode; } 
			set { 
				_mode = value;
				switch (_mode)
				{
				case SignableDocuments.PrePlumbingCheck: this.Title = "Sign pre-plumbing check sheet"; break;
				case SignableDocuments.ServiceReport: this.Title = "Sign service report form"; break;
				}
			} 
		}
		
		
		private bool _signingMode;
		public bool SigningMode { get { return _signingMode; } set { _signingMode = value; }	}
		
		public UIImageView Signature {	get { return _sig; } set { _sig = value; } }
		public UIWebView PDFView { get { return pdfView; } set { pdfView = value; } }
		
		public SignatureViewController (DetailedTabs tabs) : base ("SignatureViewController", null)
		{
			this._tabs = tabs;
			
			PDFView = new UIWebView(new RectangleF(0,20,703,748));
			
			ToolbarItems = new UIBarButtonItem[] {
				new UIBarButtonItem(UIBarButtonSystemItem.Reply),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning()),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem(UIBarButtonSystemItem.Action)			
			};
		}
		
		public EventHandler StartSigning()
		{
			return delegate {
			ToolbarItems[2] = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning());			
			
			Signature.Image = new UIImage();
			hasBeenSigned = false;
			SigningMode = true;
			};
		}
		
		public EventHandler FinishSigning()
		{
			return delegate {
				ToolbarItems[2] = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning());
			};
		}	
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			//any additional setup after loading the view, typically from a nib.
			
			_sig = new UIImageView();
			_sig.Image = new UIImage();
			_sig.Frame = new RectangleF(0, 0, sigCanvas.Frame.Size.Width, sigCanvas.Frame.Size.Height);
			_sig.AutoresizingMask = UIViewAutoresizing.FlexibleWidth; // resizing an image: concern!

			sigCanvas.AddSubview (_sig);
			sigCanvas.AutoresizingMask = UIViewAutoresizing.FlexibleWidth; // resizing an image: concern!
			mouseMoved = 0;
			
		}
		
		public override void TouchesBegan (NSSet touches, UIEvent evt)
		{
			if (_signingMode) {
				mouseSwiped = false;
				UITouch touch = (UITouch)touches.AnyObject;
				// Console.WriteLine ("Event fired: TouchesBegan"+ touch.LocationInView(sigCanvas).ToString () );
				if (touch.TapCount == 3)		// triple tap by user clears the signature field
				{
					_sig.Image = new UIImage();
					_tabs._navWorkflow.RightButton.Enabled = false;
					return;
				}
				lastPoint = touch.LocationInView (sigCanvas);
				// base.TouchesBegan (touches, evt);
			}
		}
		
		public override void TouchesMoved (NSSet touches, UIEvent evt)
		{
			if (_signingMode) {
				mouseSwiped = true;
				
				UITouch touch = (UITouch)touches.AnyObject;
				PointF currentPoint = touch.LocationInView (sigCanvas);
				// Console.WriteLine("Event fired: TouchesMoved: "+currentPoint.ToString ());			
				UIGraphics.BeginImageContext (sigCanvas.Frame.Size);			
				_sig.Image.Draw (new RectangleF(0,0, sigCanvas.Frame.Size.Width, sigCanvas.Frame.Size.Height));
				
				CGContext cgc = UIGraphics.GetCurrentContext ();
				cgc.SetLineCap(CGLineCap.Round);
				cgc.SetLineWidth (5);
				cgc.SetStrokeColor (0,0,0,1);
				cgc.BeginPath ();
				cgc.MoveTo (lastPoint.X, lastPoint.Y);
				cgc.AddLineToPoint (currentPoint.X, currentPoint.Y);
				cgc.StrokePath ();
				cgc.Flush ();
				
				_sig.Image = UIGraphics.GetImageFromCurrentImageContext ();
				UIGraphics.EndImageContext ();
				cgc.Dispose ();
				
				lastPoint = currentPoint;
				mouseMoved ++;
				if (mouseMoved == 10) { mouseMoved = 0; }
			}
		}
		
		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			if (_signingMode) {
				// Console.WriteLine("Event fired: TouchesEnded: "+ lastPoint.ToString());
				UITouch touch = (UITouch)touches.AnyObject;
				
				if (touch.TapCount == 3)		// triple tap by user clears the signature field
				{
					_sig.Image = new UIImage();
					_tabs._navWorkflow.RightButton.Enabled = false;
					return;
				}
				
				if (!mouseSwiped) {
					UIGraphics.BeginImageContext (sigCanvas.Frame.Size);			
					_sig.Image.Draw (new RectangleF(0,0, sigCanvas.Frame.Size.Width, sigCanvas.Frame.Size.Height));
					CGContext cgc = UIGraphics.GetCurrentContext ();
					cgc.SetLineCap(CGLineCap.Round);
					cgc.SetLineWidth (5);
					cgc.SetStrokeColor (0,0,0,1);
					cgc.BeginPath ();
					cgc.MoveTo (lastPoint.X, lastPoint.Y);
					cgc.AddLineToPoint (lastPoint.X, lastPoint.Y);
					cgc.StrokePath ();
					cgc.Flush ();
					_sig.Image = UIGraphics.GetImageFromCurrentImageContext ();
					UIGraphics.EndImageContext ();
					cgc.Dispose ();
				}
			}
			hasBeenSigned = true;
		}
		
		public override void ViewDidAppear (bool animated)
		{
			switch (_mode)
			{
			case SignableDocuments.PrePlumbingCheck: { _tabs._navWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.SigningPrePlumbingCheck); break; }
			case SignableDocuments.ServiceReport: { _tabs._navWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.SigningServiceCallReport); break; }
			default: return;
			}

			_tabs.SigningNav.SetToolbarHidden (false, true);
			base.ViewDidAppear (animated);
			
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
			// base.ViewDidUnload ();
			this.sigCanvas.Dispose ();
			// this.sigCanvas  = null;
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations

			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
}

