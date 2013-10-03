using System;
using System.IO;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using System.Drawing;
using System.Collections.Generic;

namespace Puratap
{
	public class SignPrePlumbingViewController : NewSignatureViewController
	{
		public EventHandler GoBack { get; set; }
		public EventHandler GoForward { get; set; }
		public EventHandler StartSigning { get; set; }
		public EventHandler FinishSigning { get; set; }
		
		UIBarButtonItem back;
		UIBarButtonItem forward;
		UIBarButtonItem signing;
		UIBarButtonItem clearSignature;
		
		public SignPrePlumbingViewController (DetailedTabs tabs) : base (tabs)
		{			
			this.Title = "Sign pre-plumbing";
			using (var image = UIImage.FromBundle ("Images/187-pencil") ) this.TabBarItem.Image = image;			
			this.NavigationItem.HidesBackButton = true;
			
			GoBack = delegate {
				if (!SigningMode) {
					Tabs.SelectedViewController = Tabs.ViewControllers[Tabs.LastSelectedTab];
				}
				else {
					FinishSigning(null, null);
					hasBeenSigned = false;
					Tabs.SelectedViewController = Tabs.ViewControllers[Tabs.LastSelectedTab];
				}
			};
			
			GoForward = delegate {
				if (hasBeenSigned && !SigningMode) {
					bool ShouldSignService = false;
					Job main = (Tabs._jobRunTable.CurrentJob.HasParent ())?  Tabs._jobRunTable.FindParentJob (Tabs._jobRunTable.CurrentJob) : Tabs._jobRunTable.CurrentJob;
					if (main.Type.Code == "SER") ShouldSignService = true;
					foreach(Job child in main.ChildJobs)
						if (child.Type.Code == "SER") ShouldSignService = true;


					if ( (Tabs._prePlumbView.pr.UpgradeOffered == PrePlumbingCheckView.Choices.Option1 || 
					    Tabs._prePlumbView.pr.UpgradeOffered == PrePlumbingCheckView.Choices.Option2) &&
					    Tabs._prePlumbView.pr.CustomerAcceptedUpgrade == PrePlumbingCheckView.Choices.Yes)
					{
						// look for a tubing upgrade job
						bool foundUpgrade = false;
						if (main.Type.Code.Contains ("TUBING")) foundUpgrade = true;
						if (! foundUpgrade)
						{
							foreach(Job child in main.ChildJobs)
								if (child.Type.Code.Contains ("TUBING")) foundUpgrade = true;
						}
						if (! foundUpgrade) 
						{
							Dictionary<int, string> Reasons = MyConstants.GetFollowUpReasonsFromDB();
							string pickedReason = "Unable to do upgrade";
							int reasonID = Reasons.FindKeyByValue(pickedReason);
							Tabs._jobService.SaveFollowupToDatabase (main.JobBookingNumber, reasonID, "Upgrade was authorised and not done?");
						}
					}

					if (ShouldSignService)
					{
						Tabs.SigningNav.PopToRootViewController (false);
						Tabs.SigningNav.PushViewController (Tabs.SignService, true);
					}
					else
					{
						Tabs.SigningNav.PushViewController (Tabs.SignInvoice, true);
						//Tabs.SigningNav.InvoicePushed = true;
						// Tabs._navWorkflow._finishWorkflow(null, null);
					}
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
						back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
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
				iv = (UIImageView)Tabs._prePlumbView.GeneratedPDFView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.Signature);
				
				iv.ContentMode = UIViewContentMode.ScaleAspectFit;
				UIImage im = this.Signature.GetDrawingImage (); // this.Signature.Image;
				iv.Image = im;
				
				if (hasBeenSigned)
				{
					Tabs._prePlumbView.RedrawPrePlumbingPDF (false, true);
					
					PointF offset = new PointF(0, this.PDFView.ScrollView.ContentSize.Height - this.PDFView.ScrollView.Bounds.Height);
					PDFView.ScrollView.SetContentOffset (offset, true);
					Signature.Clear (); // Signature.Image = new UIImage();
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
			string pdfFileName = Tabs._prePlumbView.pdfPrePlumbingFileName; // Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_NotSigned.pdf");;
			PDFView.LoadRequest (new NSUrlRequest( NSUrl.FromFilename (pdfFileName)));
			
			Tabs.SigningNav.SetToolbarHidden (false, true);
			Tabs.SigningNav.SetToolbarItems (this.ToolbarItems, true);

			base.ViewDidAppear (animated);
		}
	}
}

