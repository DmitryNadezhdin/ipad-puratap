using MonoTouch.UIKit;
using System.Drawing;
using System;
using System.IO;
using MonoTouch.Foundation;

namespace Puratap
{
	public partial class TakePhotosViewController : UIViewController
	{
		private DetailedTabs _tabs;
		private int _photosCounter;
		private long _customerNumber;
		public int PhotosCounter { get { return _photosCounter; } set { _photosCounter = value; } }
		public long CustomerNumber { get { return _customerNumber; } set { _customerNumber = value; } }
		
		public TakePhotosViewController (DetailedTabs tabs) : base ("TakePhotosViewController", null)
		{
			this._tabs = tabs;
			this.Title = NSBundle.MainBundle.LocalizedString ("Take photos", "Take photos");
			using (var image = UIImage.FromBundle ("Images/86-camera") ) this.TabBarItem.Image = image;
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			
			UIImagePickerController ipc = new UIImagePickerController();
			ipc.SourceType = UIImagePickerControllerSourceType.Camera;
			ipc.Delegate = ipc;
			ipc.AllowsEditing = false;
			
			ipc.FinishedPickingMedia += delegate(object sender, UIImagePickerMediaPickedEventArgs e) 
			{
				NSString k = new NSString("UIImagePickerControllerOriginalImage");
				UIImage im = (UIImage)e.Info.ObjectForKey (k);
				NSError err;
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), 
				                              String.Format ("{0}_{1}_{2}.jpg", _customerNumber.ToString (), 
				               																	DateTime.Now.Date.ToString ("yyyy-MM-dd"),
				               																	_photosCounter.ToString ()));
				_photosCounter++;
				im.AsJPEG().Save(path, true, out err);
				im.SaveToPhotosAlbum (null);



				// error handling here according to NSError err
				if (err != null)
				{
					using(var alert = new UIAlertView("Error:" + err.LocalizedDescription, "Unable to save image to: "+path, null, "OK", null))
					{
						alert.Show();
					}
				}

				ipc.DismissViewController (true, null); // ipc.DismissModalViewControllerAnimated (true);
				im.Dispose (); k.Dispose (); ipc.Dispose ();
				im = null; k = null; ipc = null;
				if (err != null) { err.Dispose (); err = null; }
				_tabs.SelectedViewController = _tabs.ViewControllers[_tabs.LastSelectedTab];
				_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
			};
			ipc.Canceled += delegate {
				ipc.DismissViewController (true, null); // ipc.DismissModalViewControllerAnimated (true);
				_tabs.SelectedViewController = _tabs.ViewControllers[_tabs.LastSelectedTab];
				_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
				ipc.Dispose (); ipc = null;
			};

			this.PresentViewController (ipc, true, null); // this.PresentModalViewController (ipc, true);

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
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
}

