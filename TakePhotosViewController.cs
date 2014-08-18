using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;
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

		private UIImagePickerController _ipc;
		
		public TakePhotosViewController (DetailedTabs tabs) : base ("TakePhotosViewController", null)
		{
			this._tabs = tabs;
			this.Title = NSBundle.MainBundle.LocalizedString ("Take photos", "Take photos");
			using (var image = UIImage.FromBundle ("Images/86-camera") ) this.TabBarItem.Image = image;
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			if (Runtime.Arch != Arch.SIMULATOR) {
				this._ipc = new UIImagePickerController ();
				_ipc.SourceType = UIImagePickerControllerSourceType.Camera;
				_ipc.Delegate = _ipc;
				_ipc.AllowsEditing = false;

				_ipc.FinishedPickingMedia += delegate(object sender, UIImagePickerMediaPickedEventArgs e) {
					NSString k = new NSString ("UIImagePickerControllerOriginalImage");
					UIImage im = (UIImage)e.Info.ObjectForKey (k);
					NSError err;
					string path = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), 
						              String.Format ("{0}_{1}_{2}.jpg", _customerNumber.ToString (), 
							              DateTime.Now.Date.ToString ("yyyy-MM-dd"),
							              _photosCounter.ToString ()));
					_photosCounter++;
					im.AsJPEG ().Save (path, true, out err);
					im.SaveToPhotosAlbum (null);

					// error handling here according to NSError err
					if (err != null) {
						using (var alert = new UIAlertView ("Error:" + err.LocalizedDescription, "Unable to save image to: " + path, null, "OK", null)) {
							alert.Show ();
						}
					}

					_ipc.DismissViewController (true, null); // _ipc.DismissModalViewControllerAnimated (true);
					im.Dispose ();
					k.Dispose ();
					im = null;
					k = null; 
					// _ipc.Dispose (); _ipc = null;

					if (err != null) {
						err.Dispose ();
						err = null;
					}
					_tabs.SelectedViewController = _tabs.ViewControllers [_tabs.LastSelectedTab];
					_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
				};

				_ipc.Canceled += delegate {
					if (! (_tabs.ViewControllers[_tabs.LastSelectedTab] is TakePhotosViewController))
						_tabs.SelectedViewController = _tabs.ViewControllers [_tabs.LastSelectedTab];
					else 
						_tabs.SelectedViewController = _tabs.ViewControllers[0];

					_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
					_ipc.DismissViewController (true, null);

					// _ipc.Dispose (); _ipc = null;
				};
			}
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			if (Runtime.Arch != Arch.SIMULATOR) 
				this.PresentViewController (_ipc, true, null);
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

