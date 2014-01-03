using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.ComponentModel;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using MonoTouch.Foundation;

namespace Puratap
{
	public partial class RunRouteViewController : UIViewController
	{
		private RunRouteNavigationController Nav;
		private BackgroundWorker bw;
		public readonly DetailedTabs Tabs;

		public RunRouteViewController (DetailedTabs tabs) : base ("RunRouteViewController", null)
		{
			Tabs = tabs;
			this.Nav = Tabs.RunRouteNav; 
			this.Title = "Run route";
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
			this.btnFindRoute.TouchUpInside += HandleStartSearchTouchUpInside;
			// Perform any additional setup after loading the view, typically from a nib.
		}

		private void HandleStartSearchTouchUpInside (object sender, EventArgs e)
		{
			this.FindRoute();
		}

		private void HandleCancelSearchTouchUpInside (object sender, EventArgs e)
		{
			this.CancelRouteSearch();
		}

		private void SetButtonStateStart()
		{
			// return start button to its default state
			this.btnFindRoute.TouchUpInside -= HandleCancelSearchTouchUpInside;
			this.btnFindRoute.SetTitle("Start", UIControlState.Normal);
			this.btnFindRoute.TouchUpInside += HandleStartSearchTouchUpInside;
		}

		private void SetButtonStateCancel()
		{
			// start button turns into a cancel button
			this.btnFindRoute.TouchUpInside -= HandleStartSearchTouchUpInside;
			this.btnFindRoute.SetTitle("Cancel", UIControlState.Normal);
			this.btnFindRoute.TouchUpInside += HandleCancelSearchTouchUpInside;
		}

		private void CancelRouteSearch()
		{
			this.bw.CancelAsync();
			this.SetButtonStateStart ();
		}

		private void FindRoute() {
			RunRouteFinder rrf = null;
			RunRoute bestRoute = null;

			this.bw = new BackgroundWorker ();
			bw.WorkerSupportsCancellation = true;
			bw.WorkerReportsProgress = true;

			bw.DoWork += delegate(object sender, DoWorkEventArgs e) {
				rrf = new RunRouteFinder (this.Tabs);
				bestRoute = rrf.GetBestRoute (sender as BackgroundWorker);

				if (bw.CancellationPending)
					e.Cancel = true;

				e.Result = bestRoute;
			};

			this.SetButtonStateCancel ();
			bw.ProgressChanged += this.FindRouteProgressChanged;
			bw.RunWorkerCompleted += this.FindRouteCompleted;
			bw.RunWorkerAsync ();
		}

		private void FindRouteProgressChanged(object sender, ProgressChangedEventArgs e) 
		{
			RunRoute state = e.UserState as RunRoute;

			float percent = (float)state.SearchCallCounter / 1800.0f;
			string msg = String.Format ("FindRouteBackgroundWorker progress changed: {0}%, routes analyzed: {1}, run steps found: {2}, average speed: {3} kmph, time per job: {4} minutes", 
				percent*100, state.SearchCallCounter, state.RunRouteLegs.Count, state.AverageDrivingSpeed, state.AverageTimePerJob);
			Console.WriteLine(msg);

			this.pvRouteSearchProgress.SetProgress(percent, false);
			this.lbRouteSearchIterations.Text = String.Format ("Iterations: {0}/30", this.GetIterationFromRouteParams(state));
		}

		private void FindRouteCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null) {
				var errorAlert = new UIAlertView ("Error while calculating route", e.Error.Message, null, "OK");
				errorAlert.Show ();
			} else if (e.Cancelled) {
				var cancelledAlert = new UIAlertView ("Cancelled", "Route calculation cancelled by user", null, "OK");
				cancelledAlert.Show ();
			}
			else {
				RunRoute bestRoute = (RunRoute)e.Result;
				if (bestRoute != null) {
					DialogViewController results = CreateRouteDialogViewController (bestRoute);
					ShowRouteResults (results);
				} else {
					var noRoutesFound = new UIAlertView ("No routes found", "Please report this to the office", null, "OK");
					noRoutesFound.Show ();
				}
				this.SetButtonStateStart ();
			}
		}

		private void ShowRouteResults(DialogViewController results)
		{
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.3f);

			if (this.Nav.ViewControllers.Count () < 2)
				this.Nav.PushViewController (results, true);
			else {
				this.Nav.PopToRootViewController (false);
				this.Nav.PushViewController (results, true);
			}
			Tabs.MyNavigationBar.Hidden = true;
			Nav.NavigationBarHidden = false;

			UIView.CommitAnimations ();
		}

		private DialogViewController CreateRouteDialogViewController(RunRoute bestRoute)
		{
			string rootHeader = String.Format ("{0} jobs -- {1} km -- {2} mins/job -- {3} kmph", 
				bestRoute.RunRouteLegs.Count + 1, bestRoute.TotalDrivingDistance, 
				bestRoute.AverageTimePerJob.TotalMinutes, bestRoute.AverageDrivingSpeed);
			DialogViewController routeController = new DialogViewController (UITableViewStyle.Grouped, 
				new RootElement (rootHeader), true);
			Section legSection = new Section ("Route steps", "End of route");
			string startLine = String.Format ("Start from: {0}", bestRoute.StartingPoint.Address);
			string startValue = String.Format ("Assumed arrival: {0}", bestRoute.StartingPoint.Start.ToShortTimeString ()); //.TimeOfDay.ToString ("hh\\:mm\\:ss \\tt"));
			legSection.Add (new MultilineElement (startLine, startValue));

			int i = 0;
			foreach (RunRouteLeg leg in bestRoute.RunRouteLegs) {
				i++;
				string stepLine = String.Format ("Step {0} \r\n Drive ~{1} km ({2} min {3} sec) \r\n To: {4}", i, 
					Math.Round (leg.Costs.MoveCost.TotalHours * bestRoute.AverageDrivingSpeed, 2),
					leg.Costs.MoveCost.Minutes,
					leg.Costs.MoveCost.Seconds,
					leg.Destination.Address);
				string stepValue = String.Format ("Arrival: {0} \r\n\r\n {1} - {2}", 
					leg.Arrival.ToShortTimeString(),
					leg.Destination.Start.ToShortTimeString(), 
					leg.Destination.End.ToShortTimeString());
				legSection.Add (new StyledMultilineElement (stepLine, stepValue));
			}
			routeController.Root.Add (legSection);

			return routeController;
		}

		// this is bad, iteration info should be read from RunRoute object directly (in FindRouteProgressChanged)
		private int GetIterationFromRouteParams(RunRoute rr) {
			TimeSpan tpj = rr.AverageTimePerJob;
			double speed = rr.AverageDrivingSpeed;
			if (speed > 0) {
				int spdIteration = (int)(((speed - 30) / 2.5)) * 6;
				int tpjIteration = (int)(tpj.TotalMinutes - 15) * (-1) + 1;
				return (spdIteration + tpjIteration);
			} else
				return 1;
		}
	}
}

