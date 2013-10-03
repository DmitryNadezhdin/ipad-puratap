using System;
using System.IO;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using System.Drawing;

namespace Puratap
{
	public class SignServiceReportViewController : NewSignatureViewController
	{
		public EventHandler GoBack { get; set; }
		public EventHandler GoForward { get; set; }
		public EventHandler StartSigning { get; set; }
		public EventHandler FinishSigning { get; set; }
		
		UIBarButtonItem back;
		UIBarButtonItem forward;
		UIBarButtonItem signing;
		UIBarButtonItem clearSignature;
		
		public SignServiceReportViewController (DetailedTabs tabs) : base (tabs)
		{
			this.Title = "Sign service report";
			using (var image = UIImage.FromBundle ("Images/187-pencil") ) this.TabBarItem.Image = image;	
			this.NavigationItem.HidesBackButton = true;
			
			GoBack = delegate {
				if (SigningMode)
				{
					FinishSigning(null,null);
					hasBeenSigned = false;
				}
				if (! Tabs._prePlumbView.IsDefault () )
				{
					// push pre-plumb signing
					Tabs.SigningNav.PopToRootViewController (false);
					Tabs.SigningNav.PushViewController (Tabs.SignPre, true);
				}
				else {
					Tabs.SelectedViewController = Tabs.ViewControllers[Tabs.LastSelectedTab];
				}
			};
			
			GoForward = delegate {
				if (this.hasBeenSigned && !SigningMode)
				{
					if (Tabs.SignInvoice != null)
					{
						Tabs.SigningNav.PushViewController (Tabs.SignInvoice, true);
						// Tabs.SigningNav.InvoicePushed = true;
					}
					else Tabs._navWorkflow._finishWorkflow(null, null);
				}
				else
				{
					var alert = new UIAlertView("", "Please finish signing the document first", null, "OK");
					alert.Show();					
				}
			};
			
			ClearSignature = delegate {
				// Signature.Image = new UIImage();
				Signature.Clear ();
				hasBeenSigned = false;
			};
			
			StartSigning = delegate {
				signing = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning);
				this.SetToolbarItems (new UIBarButtonItem[] {
						back,				 	new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						clearSignature,	new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );
				
				// Signature.Image = new UIImage();
				Signature.Clear ();
				hasBeenSigned = false;
				SigningMode = true;

				this.NavigationController.SetNavigationBarHidden (false, true);
				this.NavigationItem.SetRightBarButtonItems (new UIBarButtonItem[] { 
					new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning), 
					new UIBarButtonItem("Clear Signature", UIBarButtonItemStyle.Bordered, ClearSignature) }, true);
				this.NavigationController.SetToolbarHidden (true, true);
			};
			
			FinishSigning = delegate {
				SigningMode = false;
				signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning);				

				this.SetToolbarItems (new UIBarButtonItem[] {
						back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );
				
				this.NavigationItem.SetRightBarButtonItems (new UIBarButtonItem[] { }, true );
				this.NavigationController.SetToolbarHidden (false, true);
				
				UIImageView iv = new UIImageView();
				iv = (UIImageView)Tabs._jobService.GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.Signature);
				iv.ContentMode = UIViewContentMode.ScaleAspectFit;
				UIImage im = this.Signature.GetDrawingImage (); // this.Signature.Image;
				iv.Image = im;
				
				if (hasBeenSigned)
				{
					Tabs._jobService.RedrawServiceCallPDF (true);
					
					PointF offset = new PointF(0, this.PDFView.ScrollView.ContentSize.Height - this.PDFView.ScrollView.Bounds.Height);
					PDFView.ScrollView.SetContentOffset (offset, true);
					// Signature.Image = new UIImage();
					Signature.Clear ();
				}
				
				iv.Dispose ();	im.Dispose ();
				im = null; iv = null;
			};			
			
			back = new UIBarButtonItem(UIBarButtonSystemItem.Reply);
			signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning);
			forward = new UIBarButtonItem(UIBarButtonSystemItem.Action);
			clearSignature = new UIBarButtonItem("Clear signature", UIBarButtonItemStyle.Bordered, ClearSignature);
			
			ToolbarItems = new UIBarButtonItem[] {
				back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				forward
			};
			
			back.Clicked += GoBack;
			forward.Clicked += GoForward;			
		}
		
		public override void ViewDidAppear (bool animated)
		{
			hasBeenSigned = false;
			string pdfFileName = Tabs._jobService.pdfServiceReportFileName; // Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_NotSigned.pdf");
			try
			{
				PDFView.LoadRequest (new NSUrlRequest( NSUrl.FromFilename (pdfFileName)));
			}
			catch (Exception e) {
				this.Tabs._scView.Log (e.Message);
			}
			
			Tabs.SigningNav.SetToolbarHidden (false, true);
			Tabs.SigningNav.SetToolbarItems (this.ToolbarItems, true);

			base.ViewDidAppear (animated);
		}	
	}
}

