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

		UIImagePickerController _ipc;
		Action<UIImagePickerController, NSDictionary> HandleFinishedPickingMedia;
		Action<UIImagePickerController> HandleCanceledPickingMedia;
		
		public TakePhotosViewController (DetailedTabs tabs) : base ("TakePhotosViewController", null)
		{
			this._tabs = tabs;
			this.Title = NSBundle.MainBundle.LocalizedString ("Take photos", "Take photos");
			using (var image = UIImage.FromBundle ("Images/86-camera") ) this.TabBarItem.Image = image;
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			HandleFinishedPickingMedia = new Action<UIImagePickerController, NSDictionary> (
				delegate(UIImagePickerController picker, NSDictionary info) {
					UIImage im = (UIImage)info.ObjectForKey(UIImagePickerController.OriginalImage);
					NSError err;
					string path = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), 
						String.Format ("{0}_{1}_{2}.jpg", _customerNumber.ToString (), 
							DateTime.Now.Date.ToString ("yyyy-MM-dd"),
							_photosCounter.ToString ()));
					_photosCounter++;
					im.AsJPEG ().Save (path, true, out err);
					im.SaveToPhotosAlbum (null);
					im.Dispose (); im = null;

					// error handling here according to NSError err
					if (err != null) {
						using (var alert = new UIAlertView ("Error:" + err.LocalizedDescription, "Unable to save image to: " + path, null, "OK", null)) {
							alert.Show ();
						}
						err.Dispose ();
						err = null;
					}

					picker.DismissViewController (true, null); // _ipc.DismissModalViewControllerAnimated (true);
					_tabs.SelectedViewController = _tabs.ViewControllers [_tabs.LastSelectedTab];
					_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
				});

			HandleCanceledPickingMedia = new Action<UIImagePickerController> (
				delegate(UIImagePickerController picker) {
					if (! (_tabs.ViewControllers[_tabs.LastSelectedTab] is TakePhotosViewController))
						_tabs.SelectedViewController = _tabs.ViewControllers [_tabs.LastSelectedTab];
					else 
						_tabs.SelectedViewController = _tabs.ViewControllers[0];

					_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
					picker.DismissViewController(true, null);
			});
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			 
			if (Runtime.Arch != Arch.SIMULATOR) {
				_ipc = new UIImagePickerController ();
				_ipc.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
				_ipc.SourceType = UIImagePickerControllerSourceType.Camera;
				_ipc.ShowsCameraControls = true;
				_ipc.AllowsEditing = false;
				_ipc.Delegate = new ImagePickerDelegate (HandleFinishedPickingMedia, HandleCanceledPickingMedia);

				this.PresentViewController (_ipc, animated, null);
			}
		}

		public override void ViewWillDisappear (bool animated)
		{
			if (Runtime.Arch != Arch.SIMULATOR) {

			}
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

	public class ImagePickerDelegate : UIImagePickerControllerDelegate {
		private readonly Action<UIImagePickerController, NSDictionary> _captureEvent;
		private readonly Action<UIImagePickerController> _cancelEvent;

		public ImagePickerDelegate(Action<UIImagePickerController, NSDictionary> captureEvent, 
									Action<UIImagePickerController>cancelEvent)
		{
			_captureEvent = captureEvent;
			_cancelEvent = cancelEvent;
		}

		public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
		{
			_captureEvent(picker, info);
		}

		public override void Canceled (UIImagePickerController picker)
		{
			_cancelEvent (picker);
		}
	}
}

