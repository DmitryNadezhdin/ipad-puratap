using System;
using System.Linq;
using System.Collections.Generic;
using MonoTouch.UIKit;

namespace Puratap
{
	public class RunRouteFinder
	{
		private DetailedTabs tabsHost;

		public double AverageDrivingSpeed { get; set; }
		double MaximumDrivingSpeed { get; set; }
		double DrivingSpeedIncreaseStep { get; set; }

		public TimeSpan AverageTimePerJob { get; set; }
		TimeSpan InitialTimePerJob { get; set; }
		TimeSpan MinimumTimePerJob { get; set; }
		TimeSpan TimePerJobDecreaseStep { get; set; }

		int StartingPointsSearchDepth { get; set; } // this should really not matter, I'd rather have all possible starting points considered
		int BookingsSearchDepth { get; set; }
		int StartingPointNumber { get; set; }

		double ShortDistanceMultiplier { get; set; }
		double LongDistanceMultiplier { get; set; }

		List<RunRoute> PossibleRoutes { get; set; }
		RunRoute CurrentRoute { get; set; }
		bool FoundAnyRoute { get; set; }

		RunBooking StartingPoint { get; set; }
		List<RunBooking> RunJobs { get; set; }
		double [,] CostMatrix { get; set; }
		int CostMatrixSize { get; set; }
		int[] OptimumCounters { get; set; }

		int stepSearchResult { get; set; }
		int nStep { get; set; }
		int optIndex { get; set; }

		public RunRouteFinder (DetailedTabs tabs)
		{
			this.tabsHost = tabs;

			// define run route search parameters
			this.AverageDrivingSpeed = 30;
			this.MaximumDrivingSpeed = 40;
			this.DrivingSpeedIncreaseStep = 2.5; 

			this.ShortDistanceMultiplier = 2;
			this.LongDistanceMultiplier = Math.Sqrt (2); // 1.5;

			this.AverageTimePerJob = new TimeSpan (0, 15, 0);
			this.InitialTimePerJob = new TimeSpan (0, 15, 0);
			this.MinimumTimePerJob = new TimeSpan (0, 10, 0);
			this.TimePerJobDecreaseStep = new TimeSpan (0, 1, 0);

			this.StartingPointsSearchDepth = 6;
			this.BookingsSearchDepth = 300;
			this.StartingPointNumber = 0;

			PossibleRoutes = new List<RunRoute> ();
			RunJobs = new List<RunBooking> ();
		}

		public RunRoute GetBestRoute(System.ComponentModel.BackgroundWorker bw)
		{
			this.Initialize ();
			bool searchParametersLocked = false;
			do { 		// while (this.FoundAnyRoute == false || StartingPointNumber < StartingPointsSearchDepth);
				do { 	// while ( nStep != this.CostMatrixSize+1 && ((this.CurrentRoute.SearchCallCounter / StartingPointsSearchDepth) < BookingsSearchDepth) );
					if (stepSearchResult == 0) {
						// dead end -- impossible booking exists, step back and look for an alternative
						nStep --;
						optIndex = this.OptimumCounters[nStep-1];
						stepSearchResult = this.GetNextRunStep(bw);

					} else if (stepSearchResult == 1) {
						// found a valid step, search for next step
						nStep ++;
						optIndex = 0;
						stepSearchResult = this.GetNextRunStep(bw);

					} else if (stepSearchResult == 2) {
						// search ended, check FoundAnyRoute to determine if a route was found
						break; 
					}
				} while ( nStep != this.CostMatrixSize+1 && ((this.CurrentRoute.SearchCallCounter / StartingPointsSearchDepth) < BookingsSearchDepth) );

				if (bw.CancellationPending)
					return null;

				if (this.FoundAnyRoute) {
					// lock parameter set, continue searching for other routes
//					Console.WriteLine(" ------- RUN ROUTE ------- ");
//					foreach (var leg in this.CurrentRoute.RunRouteLegs) {
//						Console.WriteLine (String.Format("Index = {0}, Drive time = {1}, " +
//						                                 "Destination: {2}, " +
//						                                 "Arrival time: {3}, Departure time: {4}", leg.BookingIndex, leg.Costs.MoveCost, leg.Destination.Address, leg.Arrival, leg.Departure));
//					}
//					Console.WriteLine(String.Format("Total driving cost: {0}", CurrentRoute.TotalDrivingCost));

					searchParametersLocked = true;

					if (StartingPointNumber < StartingPointsSearchDepth) {
						StartingPointNumber ++;
						this.StartingPoint = this.RunJobs[StartingPointNumber];
						this.ResetCurrentRoute();
						this.ResetOptimumCounters();
						nStep = 0;
						optIndex = 0;
						stepSearchResult = 1;
						this.FoundAnyRoute = false;
					}
				} else {
					// adjust parameters if possible

					// adjust starting point
					if (StartingPointNumber < StartingPointsSearchDepth) {
						StartingPointNumber ++;
					} else {
						if ( !searchParametersLocked ) {
							// adjust time per job if possible
							if (AverageTimePerJob > MinimumTimePerJob) {
								AverageTimePerJob = AverageTimePerJob - TimePerJobDecreaseStep;
								// reset other parameters
								this.CurrentRoute.SearchCallCounter = 0;
								StartingPointNumber = 0;
							} else {
								// adjust driving speed if possible
								if (AverageDrivingSpeed < MaximumDrivingSpeed) {
									AverageDrivingSpeed = AverageDrivingSpeed + DrivingSpeedIncreaseStep;

									// reset other parameters
									// cost matrix has to be re-initialized since it contains cost in minutes calculated using previous value of AverageDrivingSpeed
									this.InitCostMatrix();
									this.CurrentRoute.SearchCallCounter = 0;
									AverageTimePerJob = InitialTimePerJob;
									StartingPointNumber = 0;
								}
								else {
									// drive speed at max --> exit
									break;
								}
							}
						} else {
							// search parameters have been locked, starting points exhausted --> exit
							break;
						}
					}

					// reset route properties since search parameters have been altered
					this.StartingPoint = this.RunJobs[this.StartingPointNumber];
					this.ResetCurrentRoute();
					this.ResetOptimumCounters();
					stepSearchResult = 1;
					nStep = 0;
					optIndex = 0;
				}

			} while (this.FoundAnyRoute == false || StartingPointNumber < StartingPointsSearchDepth);

			return this.SelectBestOfPossibleRoutes();
		}

		public int GetNextRunStep(System.ComponentModel.BackgroundWorker bw)
		{
			// if step index exceeds the matrix size, we have found a route
			if (nStep == this.CostMatrixSize) {
				this.FoundAnyRoute = true;
				return 2;
			}

			this.CurrentRoute.SearchCallCounter ++;

			// report progress to the background worker thread
			if (this.CurrentRoute.SearchCallCounter % 10 == 0) {
				int prcnt = (int) this.CurrentRoute.SearchCallCounter / (this.StartingPointsSearchDepth*this.BookingsSearchDepth);
				if (!bw.CancellationPending) 
					bw.ReportProgress (prcnt, this.CurrentRoute);
			}

			var costMatrixForStep = new double [CostMatrixSize, CostMatrixSize];
			RunRouteLeg lastLeg = (this.CurrentRoute.RunRouteLegs.Count > 0) ? this.CurrentRoute.RunRouteLegs [this.CurrentRoute.RunRouteLegs.Count - 1] : 
																				new RunRouteLeg();
			if (nStep > 1) {
				// adjust the cost matrix considering previous steps -- copy initial matrix, then remove rows and columns for jobs already done

				// copy the initial matrix
				for (int i = 0; i < this.CostMatrixSize; i++) {
					for (int j = 0; j < this.CostMatrixSize; j++) {
						// "cross out" rows and columns for customers already visited (including last destination)
						if (this.CurrentRoute.RunRouteLegs.Exists (leg => leg.Origin.Index == i) ||
						    this.CurrentRoute.RunRouteLegs.Exists (leg => leg.Origin.Index == j) ||
						    this.StartingPoint.Index == i || this.StartingPoint.Index == j ||
						    lastLeg.Origin.Index == i || lastLeg.Origin.Index == j)

							costMatrixForStep [i, j] = -1;
						else
							costMatrixForStep [i, j] = this.CostMatrix [i, j];
					}
				}
			} else { // nStep == 1
				// copy the initial matrix
				for (int i = 0; i < this.CostMatrixSize; i++) {
					for (int j = 0; j < this.CostMatrixSize; j++) {
						costMatrixForStep [i, j] = this.CostMatrix [i, j];
					}
				}
			}

			// get raw costs (distance) for current step index (list of RunRouteStepCost)
			RunBooking currentLocation = (nStep == 1) ? this.StartingPoint : this.RunJobs[lastLeg.Destination.Index];
			var StepCosts = new List<RunRouteStepCost> ();
			for (int i = 0; i < this.CostMatrixSize; i++) {
				if (currentLocation.Index != i) {
					if (costMatrixForStep [currentLocation.Index, i] > 0 && costMatrixForStep [currentLocation.Index, i] < 9999) {
						StepCosts.Add (new RunRouteStepCost () { 
							RowIndex = i, 
							MoveCost = TimeSpan.FromMinutes (costMatrixForStep [currentLocation.Index, i])
						});
					}
				}
			}

			// adjust the costs considering current time and job time frames

			// additional considerations here: 
			// 1. if run is a country run and distance exceeds "country distance threshold" value,
			// 		increase driving speed to "country limit"
			// 2. if run is city and travel would be made around peak hour time, decrease driving speed
			// 3. with 9:00 - 17:00 bookings, adding time until expires to the total cost seems to be problematic
			//		if there are several of these on the run (thus making the run "easier"), they are all delayed until last possible moment
			//		since "until expires" value is overshadowing everything else. However, when the "expiry" time closes in, sometimes
			//		it's impossible to do these along with "regular" evening bookings (16:30 - 17:30 and 17:00 - 17:30)

			//	foreach (RunRouteStepCost stepCost in StepCosts) { -- would not work since collection is modified inside enumeration
			for (int t = 0; t < StepCosts.Count; t++) {
				RunRouteStepCost stepCost = StepCosts [t];
				RunBooking booking = this.RunJobs [stepCost.RowIndex];
				// calculate time until job "expires"
				// FIXME :: if time until expires == 0 here, is there a point in going further?
				DateTime dtArrival = new DateTime( (long) this.CurrentRoute.CurrentRunRouteTime.Ticks + stepCost.MoveCost.Ticks);
				stepCost.TimeUntilExpires = ( DateTime.Compare(booking.End, dtArrival) > 0) ? booking.End - dtArrival : TimeSpan.FromMinutes(0);

				// calculate waiting costs and adjusted total costs
				if (DateTime.Compare(dtArrival, booking.Start) < 0) {
					TimeSpan waitCost = booking.Start - dtArrival;
					stepCost.WaitCost = waitCost;
					double waitPenaltyMultiplier = (waitCost > AverageTimePerJob) ? 3 : 1.5;
					stepCost.AdjustedTotalCost = TimeSpan.FromSeconds(stepCost.MoveCost.TotalSeconds + stepCost.TimeUntilExpires.TotalSeconds + 
																	 (waitCost.TotalSeconds * waitPenaltyMultiplier) );
				} else {
					stepCost.WaitCost = TimeSpan.FromMinutes (0);
					stepCost.AdjustedTotalCost = TimeSpan.FromSeconds(stepCost.MoveCost.TotalSeconds + stepCost.TimeUntilExpires.TotalSeconds);
				}
				StepCosts [t] = stepCost;
			}
			// dump StepCosts to console
//			foreach (RunRouteStepCost sc in StepCosts)
//				Console.WriteLine (String.Format ("Index = {0}, MoveCost = {1}, WaitCost = {2}, Until Expires = {3}, Total cost = {4}", 
//				                                  sc.RowIndex, sc.MoveCost, sc.WaitCost, sc.TimeUntilExpires, sc.AdjustedTotalCost ));

			// order step costs by AdjustedTotalCost
			StepCosts = StepCosts.OrderBy (cost => cost.AdjustedTotalCost).ThenBy (cost => cost.MoveCost).ToList ();

			// dump StepCosts to console again after the sorting
//			foreach (RunRouteStepCost sc in StepCosts)
//				Console.WriteLine (String.Format ("Index = {0}, MoveCost = {1}, WaitCost = {2}, Until Expires = {3}, Total cost = {4}", 
//				                                  sc.RowIndex, sc.MoveCost, sc.WaitCost, sc.TimeUntilExpires, sc.AdjustedTotalCost ));

			// check if can select the proposed step that corresponds to optimumIndex value
			if (StepCosts.Count <= optIndex) {
				// cannot select step: step back
				// check if stepIndex = 1
				if (nStep == 1)
					// stepIndex = 1 -- failed to find the first step => return 2
					return 2;
				else {		
					// stepIndex > 1
					// set route current time back to what it was before last step was made
					DateTime lastTime = new DateTime(this.CurrentRoute.CurrentRunRouteTime.Ticks - AverageTimePerJob.Ticks - lastLeg.Costs.MoveCost.Ticks - lastLeg.Costs.WaitCost.Ticks);
					this.CurrentRoute.CurrentRunRouteTime = lastTime;
					// remove last leg from route
					this.CurrentRoute.RunRouteLegs.Remove (lastLeg);
					// increase optimum counter for previous step
					this.OptimumCounters [nStep - 1] ++;
					// reset optimum counters for all subsequent steps (including current index)
					for (int i = nStep-1; i < this.OptimumCounters.Length; i++)
						this.OptimumCounters [i] = 0;
					// return 0 -- step back
					return 0;
				}
			} else {
				// selected step
				RunRouteStepCost selectedStepCost = StepCosts [optIndex];
				RunBooking thisStepDestination = this.RunJobs[selectedStepCost.RowIndex];
				DateTime arrivalTime = new DateTime(this.CurrentRoute.CurrentRunRouteTime.Ticks + selectedStepCost.MoveCost.Ticks + selectedStepCost.WaitCost.Ticks);
				// check if selected step satisfies conditions:
				// 1. Check total run time if the step is performed
				bool TotalRunTimeCheckFailed = (arrivalTime > this.StartingPoint.Start.AddHours(12)) ? true : false;

				// 2. Check if we'll be late for the job on selected step
					// * * * REMOVED :: REDUNDANT, check (3) already covers this condition * * * 
					// bool LateForJobCheckFailed = (arrivalTime > checkDestination.End) ? true : false;

				// 3. Check if (arrival time + time spent on job) will exceed end time of any job that has not been done yet
				bool ImpossibleJobExists = false;
				foreach (RunBooking booking in this.RunJobs) {
					// check if booking was visited
					if (booking.Index != this.StartingPoint.Index) {
						if (!this.CurrentRoute.RunRouteLegs.Exists (leg => leg.Destination.Index == booking.Index)) {
							if (arrivalTime > booking.End) {
								ImpossibleJobExists = true;
								break;
							}
						}
					}
				}
				// if any of (1), (2) or (3) checks failed, then there is no point in iterating over Costs list, as everything else will be worse
				if (TotalRunTimeCheckFailed || ImpossibleJobExists) {
					// if stepIndex = 1 -- failed to find a valid first step => return 2
					if (nStep == 1)
						return 2;
					else { // if stepIndex > 1 =>
						// set route current time back to what it was before last step was made
						DateTime lastTime = new DateTime(this.CurrentRoute.CurrentRunRouteTime.Ticks - AverageTimePerJob.Ticks - lastLeg.Costs.MoveCost.Ticks - lastLeg.Costs.WaitCost.Ticks);
						this.CurrentRoute.CurrentRunRouteTime = lastTime;
						// remove last leg from route
						this.CurrentRoute.RunRouteLegs.Remove (lastLeg);
						// increase optimum counter for previous step
						this.OptimumCounters [nStep - 1] ++;
						// reset optimum counters for all subsequent steps (including current index)
						for (int i = nStep-1; i < this.OptimumCounters.Length; i++)
							this.OptimumCounters [i] = 1;
						// return 0 -- step back
						return 0;
					}
				} else {
					// all checks passed => add selected step to route
					RunRouteLeg thisLeg = new RunRouteLeg ();
					thisLeg.Origin = (nStep == 1) ? this.StartingPoint : lastLeg.Destination;
					thisLeg.Destination = thisStepDestination;
					thisLeg.Costs = selectedStepCost;
					thisLeg.Arrival = new DateTime(this.CurrentRoute.CurrentRunRouteTime.Ticks + thisLeg.Costs.MoveCost.Ticks + thisLeg.Costs.WaitCost.Ticks);
					thisLeg.Departure = new DateTime(thisLeg.Arrival.Ticks + AverageTimePerJob.Ticks);
					thisLeg.OptimumIndex = optIndex;
					thisLeg.BookingIndex = selectedStepCost.RowIndex; // thisStepDestination.Index;
					// add leg to CurrentRoute
					this.CurrentRoute.RunRouteLegs.Add (thisLeg);
					// increase CurrentRunRouteTime
					this.CurrentRoute.CurrentRunRouteTime = thisLeg.Departure;
					// add driving cost to CurrentRoute.TotalDrivingCost
						// UNNECESSARY -- getter for TotalDrivingCost does the calculation -- this.CurrentRoute.TotalDrivingCost += thisLeg.Costs.MoveCost;
					// return 1 -- found valid step
					return 1;
				}				 
			}
		}

		public void Initialize()
		{
			this.FoundAnyRoute = false;

			this.PossibleRoutes.Add (new RunRoute (this));
			this.CurrentRoute = this.PossibleRoutes.ElementAt (0);

			this.InitRunData ();
			this.InitMatrixSize ();
			this.InitOptimumCounters ();
			this.InitCostMatrix ();
			this.InitStartingPoint ();

			this.stepSearchResult = 1;
			this.nStep = 0;
			this.optIndex = 0;
		}

		public void InitRunData()
		{
			bool foundExpiredJobs = false;
			// load run jobs into RunJobs list here
			int i = 0;
			foreach (Job job in tabsHost._jobRunTable.MainJobList) {
				if (job.Started == MyConstants.JobStarted.None) {
					// TODO :: check if job time frame has expired already
					var booking = new RunBooking () { 
						Index = i,
						Start = job.JobTimeStart, 
						End = job.JobTimeEnd 
					};

					Customer c = this.tabsHost._jobRunTable.Customers.Find (customer => customer.CustomerNumber == job.CustomerNumber);
					booking.Address = c.Address + ' ' + c.Suburb;
					booking.Lat = c.Lat;
					booking.Lng = c.Lng;
					
					this.RunJobs.Add (booking);
					i++;
				}
			}
			this.RunJobs = this.RunJobs.OrderBy (b => b.End).ThenBy (b => b.Start).ToList ();
			i = 0;
			foreach (RunBooking b in this.RunJobs) {
				b.Index = i;
				i ++;
			}

			if (foundExpiredJobs) {
				var ignoredExpiredJobs = new UIAlertView ("Jobs with expired time frames have been found", 
				                                          "These jobs are ignored in route calculation", null, "OK");
				ignoredExpiredJobs.Show ();
			}
		}

		public void InitMatrixSize()
		{
			this.CostMatrixSize = this.RunJobs.Count;
			this.CostMatrix = new double[CostMatrixSize, CostMatrixSize];
		}

		public void InitOptimumCounters()
		{
			this.OptimumCounters = new int[this.CostMatrixSize];
			for (int i = 0; i < this.CostMatrixSize; i++)
				this.OptimumCounters [i] = 0;
		}

		public void InitCostMatrix()
		{
			RunBooking origin, destination;
			for (int i = 0; i < this.CostMatrixSize; i++) {
				for (int j = 0; j < this.CostMatrixSize; j++) {
					if (i != j) {
						origin = new RunBooking() { 
							Lat = this.RunJobs[i].Lat,
							Lng = this.RunJobs[i].Lng,
							Start = this.RunJobs[i].Start,
							End = this.RunJobs[i].End 
						};
						destination = new RunBooking () { 
							Lat = this.RunJobs[j].Lat,
							Lng = this.RunJobs[j].Lng,
							Start = this.RunJobs[j].Start,
							End = this.RunJobs[j].End
						};
						// calculate cost
						double distanceCost = this.DirectDistance (origin.Lat, origin.Lng, destination.Lat, destination.Lng);
						// adjust cost using distance multipliers
						double adjustedDistanceCost = (distanceCost < 3) ? distanceCost * ShortDistanceMultiplier : distanceCost * LongDistanceMultiplier;
						// calculate cost in seconds
						double costInSeconds = (adjustedDistanceCost / AverageDrivingSpeed) * 3600;
						// transform into TimeSpan
						TimeSpan timeCost = TimeSpan.FromSeconds (costInSeconds);
						// check if the destination could possibly be done after the origin
						DateTime afterDone = new DateTime (origin.Start.Ticks + AverageTimePerJob.Ticks + timeCost.Ticks);
						if (afterDone > destination.End) {
							this.CostMatrix [i, j] = 999999;
						} else {
							this.CostMatrix [i, j] = timeCost.TotalMinutes;
						}
					} else {
						this.CostMatrix [i, j] = 0;
					}
				}
			}
			Console.WriteLine ("Run route finder: Cost matrix has been initialized.");
		}

		public void InitStartingPoint()
		{
			if (this.RunJobs != null) {
				List<RunBooking> sortedRunJobs = this.RunJobs.OrderBy (booking => booking.Start).ThenBy (booking => booking.End).ToList ();
				RunBooking startingPoint = sortedRunJobs [0];
				this.StartingPoint = startingPoint;
				this.StartingPointNumber = 0; // startingPoint.Index;
				this.CurrentRoute.CurrentRunRouteTime = new DateTime(startingPoint.Start.Ticks + AverageTimePerJob.Ticks);
				this.CurrentRoute.StartingPoint = new RunBooking () { 
					Lat = this.StartingPoint.Lat,
					Lng = this.StartingPoint.Lng,
					Index = this.StartingPoint.Index,
					Address = this.StartingPoint.Address,
					Start = this.StartingPoint.Start,
					End = this.StartingPoint.End
				};
			} else {
				return;
			}
		}

		public void ResetCurrentRoute()
		{
			if (this.FoundAnyRoute) {
				this.PossibleRoutes.Add (new RunRoute (this));
				this.CurrentRoute = this.PossibleRoutes.Last ();
			} else {
				this.CurrentRoute.ClearRouteLegs ();
			}
			this.CurrentRoute.CurrentRunRouteTime = new DateTime(this.StartingPoint.Start.Ticks + AverageTimePerJob.Ticks);
			this.CurrentRoute.StartingPoint = new RunBooking() {
				Lat = this.StartingPoint.Lat,
				Lng = this.StartingPoint.Lng,
				Index = this.StartingPoint.Index,
				Address = this.StartingPoint.Address,
				Start = this.StartingPoint.Start,
				End = this.StartingPoint.End
			};
			this.CurrentRoute.AverageTimePerJob = this.AverageTimePerJob;
			this.CurrentRoute.AverageDrivingSpeed = this.AverageDrivingSpeed;
		}

		public void ResetOptimumCounters()
		{
			for (int i = 0; i < this.OptimumCounters.Length; i++)
				this.OptimumCounters [i] = 1;
		}

		public RunRoute SelectBestOfPossibleRoutes()
		{
			if (this.PossibleRoutes != null) {
				this.PossibleRoutes.RemoveAll (route => route.RunRouteLegs.Count < this.CostMatrixSize - 1);
				if (this.PossibleRoutes.Count > 0) {
					List<RunRoute> SortedPossibleRoutes = this.PossibleRoutes.OrderBy (route => route.TotalDrivingCost).ToList ();
					RunRoute bestRoute = SortedPossibleRoutes [0];
					bestRoute.AverageDrivingSpeed = this.AverageDrivingSpeed;
					bestRoute.AverageTimePerJob = this.AverageTimePerJob;
					return bestRoute;
				} else {
					return null;
				}
			} else {
				return null;
			}
		}

		public double DirectDistance (double lat1, double lng1, double lat2, double lng2)
		{
			double rLat1, rLat2, rLng1, rLng2;
			rLat1 = DegreesToRadians (lat1);
			rLat2 = DegreesToRadians (lat2);
			rLng1 = DegreesToRadians (lng1);
			rLng2 = DegreesToRadians (lng2);
			if (rLat1 == rLat2 && rLng1 == rLng2)
				return 0;
			else
				return 6371.00 * Math.Acos (Math.Sin (rLat1) * Math.Sin (rLat2) + Math.Cos (rLat1) * Math.Cos (rLat2) * Math.Cos (rLng1 - rLng2));
		}

		public double DegreesToRadians(double degree)
		{
			return (degree * Math.PI) / 180;
		}
	}

	public class RunRoute 
	{
		RunRouteFinder _routeFinder;

		public int SearchCallCounter { get; set; }
		public TimeSpan TotalDrivingCost { 
			get {
				double minutes = 0;
				foreach (var leg in RunRouteLegs) {
					minutes += leg.Costs.MoveCost.TotalMinutes;
				}
				return TimeSpan.FromMinutes (minutes);
			} 
			// set; 
		}

		public double TotalDrivingDistance {
			get { return Math.Round(this.TotalDrivingCost.TotalHours * _routeFinder.AverageDrivingSpeed, 1); }
		}

		public DateTime CurrentRunRouteTime { get; set; }

		int StepCounter { get; set; }
		int StartingPointIndex { get; set; }
		public TimeSpan AverageTimePerJob { get; set; }
		public double AverageDrivingSpeed { get; set; }

		public RunBooking StartingPoint { get; set; }
		public List<RunRouteLeg> RunRouteLegs { get; set; }

		public RunRoute(RunRouteFinder rrf) {
			_routeFinder = rrf;
			RunRouteLegs = new List<RunRouteLeg> ();
			// TotalDrivingCost = TimeSpan.FromMinutes (0);
		}

		public void ClearRouteLegs()
		{
			this.RunRouteLegs.Clear ();
		}
	}

	public class RunRouteLeg
	{
		public RunBooking Origin { get; set; }
		public RunBooking Destination { get; set; }
		public RunRouteStepCost Costs { get; set; }
		public DateTime Arrival { get; set; }
		public DateTime Departure { get; set; }
		public int OptimumIndex { get; set; }
		public int BookingIndex { get; set; }
	}

	public class RunBooking
	{
		public double Lat { get; set; }
		public double Lng { get; set; }
		public int Index { get; set; }
		public string Address { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}

	public class RunRouteStepCost {
		public int RowIndex { get; set; }
		public TimeSpan MoveCost { get; set; }
		public TimeSpan TimeUntilExpires { get; set; }
		public TimeSpan WaitCost { get; set; }
		public TimeSpan AdjustedTotalCost { get; set; }
	}
}

/*

* * * * * * * * * * * * * * * * *
* * * FoxPro implementation * * * 
* * * * * * * * * * * * * * * * *

* * * DEFINE CONSTANTS
	PUBLIC avgDrivingSpeed 				&& in kmph
	PUBLIC averageTimeSpentOnOneJob		&& in seconds
	PUBLIC searchDepth					&& perform this many calls to GetNextStep before altering route search parameters
	PUBLIC bookingsSearchDepth			&& on each iteration, consider this many possible steps [with lowest cost among all]
	PUBLIC startingPointAsCursorIndex	&& set initial starting point of the run (first job location)
	PUBLIC initialTimeOnJob				&& default average time spent on a job (on site)
	PUBLIC shortDistanceMultiplier		&& to be used when transforming short direct geometric distance into real-world driving distance
	PUBLIC longDistanceMultiplier		&& to be used when transforming longer direct geometric distance into real-world driving distance
	PUBLIC canBeLate							&& reserved for future use
	PUBLIC arriveAtFirstJobAtExactStartTime		&& reserved for future use
	shortDistanceMultiplier = 2
	longDistanceMultiplier = SQRT(2)
	avgDrivingSpeed = 35
	initialTimeOnJob = 15*60
	averageTimeSpentOnOneJob = initialTimeOnJob
	startingPointAsCursorIndex = 1
	bookingsSearchDepth = 10				
	searchDepth = 200
	canBeLate = .F.
	arriveAtFirstJobAtExactStartTime = .T. && if false, the first job is started at MAX(8:00 AM, MIN(starting time between all jobs on the run))
	
	PUBLIC startingPointSearchDepth		&& if route cannot be found given the conditions, starting point will change from 1 to this
	PUBLIC timeOnJobDecreaseStep		&& if route cannot be found given the conditions, average time on job will be gradually decreased
	PUBLIC drvSpeedIncreaseStep			&& if route cannot be found given the conditions, driving speed will be gradually increased
	startingPointSearchDepth = 5
	timeOnJobDecreaseStep = 60
	drvSpeedIncreaseStep = 2.5
	
	PUBLIC maxDrivingSpeed
	PUBLIC minTimeOnJob
	minTimeOnJob = 10*60		&& if time on job has been decreased to 12 and route still cannot be found, route search will stop
	maxDrivingSpeed = 40		&& if average driving speed has been increased to 40 kmph and route still cannot be found, route search will stop
* * * END DEFINE CONSTANTS

* * * DEFINE INITIAL RUN BY COORDINATES AND TIME FRAMES
	LOCAL loUtils, runDate, runID_1, runID_2, runGeoData, numberOfJobsOnRun
	runDate = m.passedRunDate
	runID_1 = m.PassedRunID_1	
	runID_2 = m.PassedRunID_2

	loUtils = NEWOBJECT("Utils", "C:\backup of m\m (puratap-server)\ccontrol.vcx")
	loUtils.Run_Distance (runDate, runID_1, runID_2)

	runGeoData = "Run_Geodata_" + TRANSFORM(runID_1) + "_" + TRANSFORM(runID_2)
	SELECT (runGeoData)
	numberOfJobsOnRun = RECCOUNT()
* * * END DEFINE INITIAL RUN BY COORDINATES AND TIME FRAMES

* * * SET UP UTILITY VARIABLES
	PUBLIC myRoute
	myRoute = NEWOBJECT("Route")
	LOCAL endLine, logFileName, _date
	_date = TRANSFORM(TtoC(runDate, 1), '@R 9999-99-99')
	logFileName = "\\PURATAP-SERVER\M\IPAD DATA\DATA.OUT\" + _date + "\Run routes\Run_Route_" + ;
				DTOC(m.RunDate,2) + "_" + ;
				TRANSFORM(m.runID_1) + "_" + ;
				myRoute.GetRunName(m.runID_1) + ".txt"
	endLine = CHR(13) + CHR(10)
				
	PUBLIC lfmProgress
	lfmProgress = CREATEOBJECT("RouteSearchProgressForm", "Route search in progress...")
	lfmProgress.UpdateMinutesPerJobCaption(ROUND(averageTimeSpentOnOneJob/60, 1))
	lfmProgress.UpdateDrivingSpeedCaption(avgDrivingSpeed)
	lfmProgress.Show()
* * * END SET UP UTILITY VARIABLES

* * * Start run route calculation
	LOCAL bestRoute

	myRoute.RunDate = DTOC(m.RunDate,2)
	myRoute.RunID = TRANSFORM(m.runID_1)
	myRoute.RunName = myRoute.GetRunName(m.runID_1)
	bestRoute = myRoute.GetBestRoute(runGeoData)
* * * End run route calculation

	* check if run route lookup succeeded
	IF VARTYPE(bestRoute) = 'O'
		* * * Display results or write route to file
		IF VARTYPE(gridToPostCursorTo) = 'O'
			bestRoute.AddProperty("ResultingCursorName", "")
			bestRoute.ResultingCursorName = bestRoute.SaveAsCursor()
			SELECT (bestRoute.ResultingCursorName)
			GO TOP
			* * * return the resulting cursor to the grid
			gridToPostCursorTo.RecordSource = bestRoute.ResultingCursorName
			gridToPostCursorTo.Refresh()
			gridToPostCursorTo.AutoFit()
			
			* * * DEBUG :: return the conditions under which the route was found to the form
			gridToPostCursorTo.Parent.txtAvgDrivingSpeed.Value = avgDrivingSpeed
			gridToPostCursorTo.Parent.txtaverageTimeSpentOnOneJob.Value = averageTimeSpentOnOneJob / 60
			* * * END DEBUG ::
		ELSE
			* there is no form to post results to, create a text log file
			STRTOFILE(endLine + "Run start time: " + TRANSFORM(bestRoute.StartingPoint.Start) + endLine, logFileName, 1)
			STRTOFILE("Run start address: " + bestRoute.StartingPoint.Address + endLine, logFileName, 1)
			STRTOFILE("Run average driving speed: " + TRANSFORM(avgDrivingSpeed) + " kmph" + endLine, logFileName, 1)
			STRTOFILE("Run average time spent on a single job: " + TRANSFORM(averageTimeSpentOnOneJob/60) + " minutes" + endLine + endLine, logFileName, 1)
			
			* starting point
			LOCAL nextLine
			nextLine = "Step " + TRANSFORM(0) + ": " + ;
					TRANSFORM(0) + " ---> " + ;
					TRANSFORM(bestRoute.StartingPoint.Index) + ;
					" || Arrival time: " + TTOC(bestRoute.StartingPoint.Start, 2) + ;
					" || Departure time: " + TTOC(bestRoute.StartingPoint.Start + averageTimeSpentOnOneJob, 2) + ;
					" || Cost: Unknown (" + TRANSFORM(0) + " mins)" + ;
					" || Until expires: " + TRANSFORM((bestRoute.StartingPoint.End - bestRoute.StartingPoint.Start) / 60) + "mins" + ;
					" || From: Puratap office" + ;
					" --> to: " + myRoute.StartingPoint.Address + ;
					" || Drive: Unknown (" + TRANSFORM(0) + " km)"
					
			STRTOFILE(nextLine + endLine, logFileName, 1)

			LOCAL totalDistance, totalDrivingTime, totalWaitTime
			totalDistance = 0
			totalDrivingTime = 0
			totalWaitTime = 0
			i = 0
			FOR EACH leg as RouteLeg IN bestRoute
				i = i + 1
				LOCAL legDriveDistance
				legDriveDistance = TRANSFORM(ROUND(leg.DrivingCost * avgDrivingSpeed / 60, 2)) + " km"
				totalDistance = totalDistance + ROUND(leg.DrivingCost * avgDrivingSpeed / 60, 2)
				totalDrivingTime = totalDrivingTime + ROUND(leg.DrivingCost, 2)
				totalWaitTime = totalWaitTime + ROUND(leg.WaitingCost, 2)
				nextLine = "Step " + TRANSFORM(i) + ": " + ;
					TRANSFORM(leg.Origin.Index) + " ---> " + ;
					TRANSFORM(leg.Destination.Index) + ;
					" || Arrival time: " + TTOC(leg.ArrivalTime - (leg.WaitingCost * 60), 2) + ;
					" || Departure time: " + TTOC(leg.DepartureTime, 2) + ;
					" || Cost: " + TRANSFORM(ROUND(leg.Cost, 2)) + " mins" + ;
					" || Until expires: " + TRANSFORM(ROUND(leg.TimeUntilExpires, 2)) + " mins" + ;
					" || From: " + leg.Origin.Address + ;
					" --> to: " + leg.Destination.Address + ;
					" || Drive: " + legDriveDistance + ;
					IIF(leg.WaitingCost > 0, " || Wait: " + TRANSFORM(CEILING(leg.WaitingCost)) + " minutes", "") + ;
					" || Destination start: " + TTOC(leg.Destination.Start, 2) + ;
					" || Destination end: " + TTOC(leg.Destination.End, 2)
				* ? nextLine
				
				STRTOFILE(nextLine + endLine, logFileName, 1)
			ENDFOR
			
			STRTOFILE(endLine + "Total jobs: " + TRANSFORM(numberOfJobsOnRun) + endLine, logFileName, 1)
			STRTOFILE("Total driving distance: " + TRANSFORM(totalDistance) + " km" + endLine, logFileName, 1)
			STRTOFILE("Total driving time: " + TRANSFORM(CEILING(totalDrivingTime)) + " minutes" + endLine, logFileName, 1)
			STRTOFILE("Total time spent on jobs: " + TRANSFORM( numberOfJobsOnRun*averageTimeSpentOnOneJob/60) + " minutes" + endLine, logFileName, 1)
			STRTOFILE("Total idle time: " + TRANSFORM(CEILING(totalWaitTime)) + " minutes" + endLine, logFileName, 1)
			STRTOFILE("Total run time: " + TRANSFORM(CEILING(totalWaitTime + totalDrivingTime) + ;
							numberOfJobsOnRun*averageTimeSpentOnOneJob/60) + " minutes" + endLine, logFileName, 1)
		
			* * * End write log to file			
		ENDIF
		
		* * * Release public objects
		ReleasePublicObjects()
		IF VARTYPE(lfmProgress) != 'U'
			lfmProgress.Release()
		ENDIF
		RETURN .T.
	ELSE

		* failed to find route
		IF VARTYPE(lfmProgress) != 'U'
			IF lfmProgress.ButtonCancel
				*!* MESSAGEBOX("Route search cancelled.")
			ELSE
				* progress form has not been released, no routes found
				logFileName = "\\PURATAP-SERVER\M\IPAD DATA\DATA.OUT\" + _date + "\Run routes\NO_ROUTE_FOUND_" + ;
					DTOC(m.RunDate,2) + "_" + ;
					TRANSFORM(m.runID_1) + "_" + ;
					myRoute.GetRunName(m.runID_1) + "_" + ;
					"NO_ROUTE_FOUND" + ".txt"
				
				LOCAL quotedLogFileName
				quotedLogFileName = ["] + logFileName + ["]
				SELECT (myRoute.RunDataCursor)
				COPY TO &quotedlogFileName DELIMITED WITH TAB
				
				STRTOFILE(endLine + endLine + "Could not find a route within given parameter thresholds..." + endLine, logFileName, 1)
				STRTOFILE("Max driving speed (average during the day) = " + TRANSFORM(maxDrivingSpeed) + " kmph" + endLine, logFileName, 1)
				STRTOFILE("Min time on job (average during the day) = " + TRANSFORM(minTimeOnJob/60) + " minutes" + endLine, logFileName, 1)
			ENDIF
		ELSE
*!*				MESSAGEBOX("Could not find a single possible route" + CHR(13) + ;
*!*							"within given parameter thresholds... ")		
		ENDIF
	ENDIF && myRoute.HasFoundRoute

* * * Release public objects
	ReleasePublicObjects()
	IF VARTYPE(lfmProgress) != 'U'
		lfmProgress.Release()
	ENDIF

	RETURN .F.
	
* ====================================================================================== *
* ==================================== END MAIN PROGRAM ================================ *
* ====================================================================================== *

   
	PROCEDURE INIT
		PARAMETER cCaption, cBarCaption
		IF EMPTY(cCaption) THEN
		  cCaption = ""
		ENDIF
		IF EMPTY(cBarCaption) THEN
		  cBarCaption = ""
		ENDIF
		ThisForm.oShp.Left=ThisForm.oTxt.Left + 1
		ThisForm.oShp.Top=ThisForm.oTxt.Top + 1
		ThisForm.oShp.Height=ThisForm.oTxt.Height-2
		ThisForm.oShp.Visible = .T.
		ThisForm.BorderStyle = 2
		ThisForm.Caption = cCaption
		ThisForm.BarCaption = cBarCaption
	ENDPROC
	
	PROCEDURE Counter_Assign
		LPARAMETERS vNewVal
		IF vNewVal != ThisForm.Counter THEN
			IF vNewVal > 100 THEN
				vNewVal = 100
			ENDIF

			ThisForm.counter = vNewVal
			ThisForm.oShp.Width = vNewVal * ThisForm.oTxt.width/100
			ThisForm.oTxt.Value = ThisForm.BarCaption+ALLTRIM(STR(vNewVal))+"% Complete"
			ThisForm.Refresh()
			* Check if cancel button has been pushed Alt+C or clicked
			IF MDOWN() OR CHRSAW() THEN 
				DOEVENTS
			ENDIF
		ENDIF
	ENDPROC
	
	PROCEDURE UpdateMinutesPerJobCaption
		LPARAMETERS nMins
		This.olbMinutesOnJob.Caption = "Avg. minutes per job: "	+ TRANSFORM(nMins)
	ENDPROC

	PROCEDURE UpdateDrivingSpeedCaption
		LPARAMETERS nDrivingSpeed		
		This.olbDrivingSpeed.Caption = "Avg. driving speed: " + TRANSFORM(nDrivingSpeed)
	ENDPROC
	
	PROCEDURE UpdateRoutesAnalyzedCaption
		LPARAMETERS nRoutesCount
		This.olbRoutesAnalyzed.Caption = "Routes analyzed: " + TRANSFORM(nRoutesCount)
	ENDPROC
	
	PROCEDURE UpdateCurrentRunStepCaption
		LPARAMETERS nStep
		This.olbCurrentRunStep.Caption = "Run steps found: " + TRANSFORM(nStep)
	ENDPROC
ENDDEFINE

DEFINE CLASS Route as Collection
	Utils = NULL
	CallCounter = 0
	matrixSize = 0
	TotalDrivingCost = 0
	RunDate = ""
	RunID = ""
	RunName = ""
	
	PROCEDURE InitStartingPoint
		LPARAMETERS recordNo
		
		SELECT (This.RunDataCursor)
		GO RECORD recordNo
		This.AddProperty("currentTime", Start_Time + averageTimeSpentOnOneJob)
		This.AddProperty("startingPointIndex", RECNO())
		This.AddProperty("startingPoint", This.GetBookingDataByIndex(This.startingPointIndex))		
		This.AddProperty("stepCounter", 1)
	ENDPROC
	
	FUNCTION GetBestRoute
		LPARAMETERS cursorName
		
		This.InitUtils()						&& creates Utils object that is used to calculate the distance between ponits
		This.InitMatrixSize(CursorName)			&& add MatrixSize property (equals to RecCount() in data cursor
		This.InitOptimumCounters() 				&& add OptimumCounters array property and fills it with '1's
		This.InitCostMatrix(This.RunDataCursor)	&& add two-dimensional array property CostMatrix_0 of size (MatrixSize x MatrixSize)
												&& and fill it with move costs considering the booking timeframes
		This.InitStartingPoint(startingPointAsCursorIndex)
												&& add starting point property (set to booking no [startingPointAsCursorIndex] in the data cursor)
		
		LOCAL nStep, optIndex, stepSearchResult
		stepSearchResult = 1
		nStep = 0
		optIndex = 0
		
		LOCAL PossibleRoutes
		PossibleRoutes = NEWOBJECT("Collection")
		LOCAL ParametersLocked
		ParametersLocked = .F.
				
		DO WHILE (This.HasFoundRoute = .F.) ;
			OR (startingPointAsCursorIndex < startingPointSearchDepth)
			
			* search for route with current parameter values
			DO WHILE nStep != This.matrixSize+1 .AND. ;
					(This.CallCounter / startingPointSearchDepth) < m.SearchDepth
				DO CASE 
					CASE stepSearchResult = 0
						* dead end, look up previous step again with a different optimum parameter
						nStep = nStep - 1
						optIndex = This.OptimumCounters[nStep]
						stepSearchResult = This.GetNextStep(nStep, optIndex, This.matrixSize)
						
					CASE stepSearchResult = 1
						* valid step, look for next step
						nStep = nStep + 1
						optIndex = 1
						stepSearchResult = This.GetNextStep(nStep, optIndex, This.matrixSize)

					CASE stepSearchResult = 2
						* search ended: either last step was found OR search depth exceeded and no route found
						EXIT
				ENDCASE
			ENDDO && while not nStep = This.matrixSize+1 and search depth not exceeded

			
			IF This.HasFoundRoute
				* route has been found --> save it and try other starting points and/or other optimum combinations
				
				* save found route
				LOCAL foundRoute
				foundRoute = NEWOBJECT("Route")
				FOR EACH leg as RouteLeg IN This
					foundRoute.Add(leg)
				ENDFOR
				foundRoute.TotalDrivingCost = This.TotalDrivingCost
				foundRoute.AddProperty("StartingPoint", This.StartingPoint)
				foundRoute.RunDate = This.RunDate
				foundRoute.RunID = This.RunID
				foundRoute.RunName = This.RunName
				
				PossibleRoutes.Add(foundRoute)
				
				* continue search until all starting points are exhausted
				* while locking in other parameters
				IF startingPointAsCursorIndex < startingPointSearchDepth 
					* increase starting point and try again
					startingPointAsCursorIndex = startingPointAsCursorIndex + 1
					This.CallCounter = 0
					This.StartingPointIndex = startingPointAsCursorIndex
					This.StartingPoint = This.GetBookingDataByIndex(This.startingPointIndex)
					This.ClearRouteLegs()
					This.ResetOptimumCounters()
					stepSearchResult = 1
					nStep = 0
					optIndex = 0
					This.HasFoundRoute = .F.
					ParametersLocked = .T.
				ELSE
					* startingPointSearchDepth reached, end search
					EXIT
				ENDIF
			ELSE
				* route has not been found --> adjust parameters if possible
				IF startingPointAsCursorIndex < startingPointSearchDepth 
					* increase starting point and try again
					startingPointAsCursorIndex = startingPointAsCursorIndex + 1
				ELSE
					* if no routes have been found yet, try to weaken parameter restrictions
					IF ParametersLocked = .F.
						IF averageTimeSpentOnOneJob > minTimeOnJob
							* decrease time on job, reset other params and try again
							This.CallCounter = 0
							averageTimeSpentOnOneJob = averageTimeSpentOnOneJob - timeOnJobDecreaseStep
							startingPointAsCursorIndex = 1
							lfmProgress.UpdateMinutesPerJobCaption(ROUND(averageTimeSpentOnOneJob/60, 1))
						ELSE
							* if cannot decrease job average time, try increase the driving speed
							IF avgDrivingSpeed < maxDrivingSpeed
								* increase driving speed, reset other params and try again					
								This.CallCounter = 0
								avgDrivingSpeed = avgDrivingSpeed + drvSpeedIncreaseStep
								averageTimeSpentOnOneJob = initialTimeOnJob
								startingPointAsCursorIndex = 1
								lfmProgress.UpdateMinutesPerJobCaption(ROUND(averageTimeSpentOnOneJob/60, 1))
								lfmProgress.UpdateDrivingSpeedCaption(avgDrivingSpeed)						
							ELSE
								* if speed at max, exit the loop
								EXIT
							ENDIF && avgDrivingSpeed < maxDrivingSpeed
						ENDIF && averageTimeSpentOnOneJob > minTimeOnJob
					ELSE
						EXIT
					ENDIF && parameters locked
				ENDIF
				
				* reset other route properties since search parameters have been altered				
				This.StartingPointIndex = startingPointAsCursorIndex
				This.StartingPoint = This.GetBookingDataByIndex(This.startingPointIndex)
				This.ClearRouteLegs()
				This.ResetOptimumCounters()
				stepSearchResult = 1
				nStep = 0
				optIndex = 0
			ENDIF
		ENDDO && while not This.HasFoundRoute
		
		IF PossibleRoutes.Count > 1
			RETURN This.SelectBestRoute(PossibleRoutes) && This
		ELSE
			IF PossibleRoutes.Count = 1
				RETURN PossibleRoutes.Item[1]
			ELSE
				RETURN .F.
			ENDIF
		ENDIF
	ENDFUNC && GetBestRoute
	
	FUNCTION SelectBestRoute
		* returns the lowest driving time route from a collection of routes
		LPARAMETERS Routes as Collection
		LOCAL Result as Route
		
		Result = Routes.Item[1]
		LOCAL i
		FOR i=2 TO Routes.Count
			IF Result.TotalDrivingCost > Routes.Item[i].TotalDrivingCost
				Result = Routes.Item[i]
			ENDIF
		ENDFOR
		
		RETURN Result
	ENDFUNC
	
	FUNCTION GetNextStep
		LPARAMETERS stepIndex, optimumIndex, matrixSize
		
		* if we have found the last step, return
		IF stepIndex = matrixSize
			This.AddProperty("HasFoundRoute", .T.)
			IF VARTYPE(lfmProgress) != 'U'
				lfmProgress.Counter = 100
			ENDIF
			RETURN 2
		ENDIF
			
		IF VARTYPE(lfmProgress.ButtonCancel) != 'U'
			IF lfmProgress.ButtonCancel
				RETURN 2
			ENDIF
		ENDIF
		
		This.CallCounter = This.CallCounter + 1
		lfmProgress.Counter = (This.CallCounter / (startingPointSearchDepth*searchDepth) ) * 100
		lfmProgress.UpdateRoutesAnalyzedCaption(This.CallCounter)
		lfmProgress.UpdateCurrentRunStepCaption(stepIndex)
*!*			IF This.CallCounter % 50 = 0
*!*				? "Routes analyzed: " + TRANSFORM(This.CallCounter) + " || Jobs done on current route: " + TRANSFORM(stepIndex)
*!*			ENDIF
		
		* * * generate the cost matrix considering previous steps
		IF stepIndex != 1 && initial matrix is generated before the first step search is called
			* copy the initial cost matrix and then nullify all rows and columns for booking indexes that have already been used
			LOCAL newCostMatrixName
			newCostMatrixName = "CostMatrix_"+TRANSFORM(stepIndex-1)
			
			This.AddProperty(newCostMatrixName+"("+TRANSFORM(matrixSize)+","+TRANSFORM(matrixSize)+")")		
			ACOPY(This.CostMatrix_0, This.&newCostMatrixName)
			
			FOR EACH existingLeg as RouteLeg IN This
				LOCAL i
				FOR i = 1 TO matrixSize
					 This.&newCostMatrixName[i, existingLeg.Origin.Index] = NULL
					 This.&newCostMatrixName[existingLeg.Origin.Index, i] = NULL
				ENDFOR
			ENDFOR
		ENDIF
		
		* * * Get costs for step index
		LOCAL currentPoint
		currentPoint = IIF(stepIndex = 1, ;
								This.StartingPoint, ;
								This.GetBookingDataByIndex( This.Item[stepIndex-1].Destination.Index ))		
		LOCAL curName
		curName = "Costs_For_Step_" + TRANSFORM(stepIndex)
		CREATE CURSOR &CurName ( ColumnIndex INTEGER, MoveCost NUMERIC(10,4), TimeUntilExpires NUMERIC (10,4), ;
								WaitCost NUMERIC (10,4), WaitPenalty NUMERIC(10,4), TotalCost NUMERIC(10,4))
		FOR i = 1 TO matrixSize
			IF i != currentPoint.Index
				LOCAL matrixIndex, matrixName
				matrixIndex = stepIndex-1
				matrixName = "CostMatrix_"+TRANSFORM(matrixIndex)
				
				IF !ISNULL(This.&matrixName[currentPoint.Index, i])
					INSERT INTO &CurName (ColumnIndex, MoveCost) VALUES (i, This.&matrixName[currentPoint.Index, i] )
				ELSE
					* ? "NULL encountered"					
				ENDIF
			ENDIF
		ENDFOR
		
		SELECT * FROM &CurName ORDER BY MoveCost ASC INTO CURSOR &CurName READWRITE
		* adjust the costs based on current time and job time frames
		SELECT (CurName)
		SCAN
			LOCAL booking
			booking = This.GetBookingDataByIndex( &CurName..ColumnIndex )
			* we add time until the allocated time frame "expires" to the total cost, so that urgent jobs are rated higher
			LOCAL timeUntilExpires
			m.timeUntilExpires = MAX( ( booking.End - (This.CurrentTime + (&CurName..MoveCost)*60) ) / 60, 0)
			REPLACE &CurName..TimeUntilExpires WITH m.timeUntilExpires
			* determine if there would be waiting involved
			IF This.CurrentTime + (&CurName..MoveCost)*60 < booking.Start
				LOCAL waitingPeriod, newCost

				* if we arrive too early for the job, we have to include the waiting time in the cost
				m.waitingPeriod = ( booking.Start - (This.CurrentTime + (&CurName..MoveCost)*60) ) / 60
				REPLACE &CurName..WaitCost WITH m.waitingPeriod
				* calculate wait penalty multiplier (if waiting for a time that exceeds average time spent on job, it is 3, otherwise 1.5)				
				LOCAL WaitPenaltyMultiplier
				WaitPenaltyMultiplier = IIF(m.WaitingPeriod*60 > averageTimeSpentOnOneJob, 3, 1.5)
				* calculate total cost by summing move cost, "urgency cost", and wait cost multiplied by penalty multiplier
				newCost = &CurName..MoveCost + m.TimeUntilExpires + (m.WaitingPeriod * WaitPenaltyMultiplier)
				REPLACE &CurName..TotalCost WITH m.newCost
			ELSE
				REPLACE &CurName..TotalCost WITH &CurName..MoveCost + m.TimeUntilExpires
			ENDIF
		ENDSCAN
		SELECT * FROM &CurName ORDER BY TotalCost ASC, MoveCost ASC INTO CURSOR &CurName READWRITE
		
		SELECT (CurName)
		IF optimumIndex <= RECCOUNT() .AND. optimumIndex < m.bookingsSearchDepth
			GO RECORD optimumIndex
		ELSE 
			* too early to stop the search
			*!*	This.AddProperty("HasFoundRoute", .F.)
			*!*	RETURN 2
			
			IF stepIndex > 1
				* set the time property to CurrentTime - (TravelTimeToLastDestination + WaitingTimeAtDestination)
				This.CurrentTime = IIF(stepIndex = 1, This.StartingPoint.Start + averageTimeSpentOnOneJob, ;
													 This.CurrentTime - averageTimeSpentOnOneJob - This.Item[stepIndex-1].Cost*60)
				
				* remove the last leg from the route
				This.Remove(stepIndex - 1)
				
				* increase the optimum index for the previous step
				This.OptimumCounters[stepIndex - 1] = This.OptimumCounters[stepIndex - 1] + 1
				* reset the optimums for the current and all subsequent steps
				FOR i = stepIndex TO matrixSize
					This.OptimumCounters[i] = 1
				ENDFOR
			ELSE
				* stepIndex = 1, there are no legs to remove and optimumIndex is way too high for valid steps to be found
				This.HasFoundRoute = .F.
				RETURN 2
			ENDIF
			* go back one step and look for another line
*!*				? "Dead end: went back one step: called GetNextStep(" + ;
*!*						TRANSFORM(stepIndex-1) + "," + ;
*!*						TRANSFORM(This.OptimumCounters[stepIndex - 1]) + ", "+ ;
*!*						TRANSFORM(matrixSize)+")"
*!*				? "Time: " + TRANSFORM(This.CurrentTime)
*!*				? "StepIndex = " + TRANSFORM(stepIndex)
			* INKEY(0)
			RETURN 0
		ENDIF
		
		LOCAL thisLegIndex, thisLegCost, thisRouteLeg, checkDestination
		checkDestination = This.GetBookingDataByIndex(&CurName..ColumnIndex)
		
		* check if current record in the costs cursor cannot satisfy out conditions (start and end times, total run cost)
		LOCAL TotalRunTimeCheckFailed, LateForNextJobCheckFailed, ImpossibleJobExists
		TotalRunTimeCheckFailed = IIF(This.CurrentTime + (&CurName..MoveCost+&CurName..WaitCost)*60 > This.StartingPoint.Start + (11*60*60), .T., .F.)
		LateForNextJobCheckFailed = IIF(This.CurrentTime + (&CurName..MoveCost+&CurName..WaitCost)*60 > checkDestination.End, .T., .F.)
		
		LOCAL arrivalTime
		arrivalTime = This.CurrentTime + (&CurName..MoveCost+&CurName..WaitCost)*60 && + averageTimeSpentOnOneJob 
		* IF arrivalTime > endTime of any job that is not included in the route yet
		SELECT (This.RunDataCursor)
		GO TOP
		* SKIP 1 -- was applicable when the first booking in data cursor always served as starting point for the run
		SCAN REST
			* skip check for the run starting point
			IF RECNO() != This.StartingPointIndex
				* check IF RECNO() is not in This collection
				LOCAL RecNoExistsInCollection
				RecNoExistsInCollection = .F.
				FOR EACH leg as RouteLeg IN This
					RecNoExistsInCollection = RecNoExistsInCollection .OR. (leg.BookingIndex=RECNO())
				ENDFOR
				IF !RecNoExistsInCollection
					LOCAL checkIfImpossible
					checkIfImpossible = This.GetBookingDataByIndex(RECNO())
					ImpossibleJobExists = ImpossibleJobExists .OR. IIF(arrivalTime > checkIfImpossible.End, .T., .F.)
					IF ImpossibleJobExists 
						* ? "Impossible job exists: " + checkIfImpossible.Address
						EXIT
					ENDIF
				ENDIF
			ENDIF
		ENDSCAN

		IF LateForNextJobCheckFailed .OR. TotalRunTimeCheckFailed .OR. ImpossibleJobExists
			
			* we are in a dead end (there is no point in iterating over Costs cursor since all records below
			* current would be even worse, so we roll back the last step and look for alternative there
			IF (stepIndex = 1)
				* ? "Impossible job found when looking for the first step. Check starting point and geocoding of customers on the run..."
				This.HasFoundRoute = .F.
				RETURN 2
			ENDIF
			
			* set the time property to CurrentTime - (TravelTimeToLastDestination + WaitingTimeAtDestination)
			This.CurrentTime = IIF(stepIndex = 1, This.StartingPoint.Start + averageTimeSpentOnOneJob, ;
												 This.CurrentTime - averageTimeSpentOnOneJob - This.Item[stepIndex-1].Cost*60)
			
			* remove the last leg from the route
			This.TotalDrivingCost = This.TotalDrivingCost - This.Item[stepIndex - 1].DrivingCost
			This.Remove(stepIndex - 1)
			
			* increase the optimum index for the previous step
			This.OptimumCounters[stepIndex - 1] = This.OptimumCounters[stepIndex - 1] + 1
			* reset the optimums for the current and all subsequent steps
			FOR i = stepIndex TO matrixSize
				This.OptimumCounters[i] = 1
			ENDFOR
			
			* go back one step and look for another line
*!*				? "Dead end: went back one step: called GetNextStep(" + ;
*!*						TRANSFORM(stepIndex-1) + "," + ;
*!*						TRANSFORM(This.OptimumCounters[stepIndex - 1]) + ", "+ ;
*!*						TRANSFORM(matrixSize)+")"
*!*				? "Time: " + TRANSFORM(This.CurrentTime)
*!*				? "StepIndex = " + TRANSFORM(stepIndex)
			* INKEY(0)
			RETURN 0
			* This call has to be made outside, otherwise restrictions are exceeded for nested calls
			* This.GetNextStep(stepIndex - 1, This.OptimumCounters[stepIndex - 1], matrixSize)
		ELSE
			* step satisfies the conditions, add it to the route and look for the next step
		
			* add this leg to route
			thisRouteLeg = NEWOBJECT("RouteLeg")
			
			* origin = run starting point on the first step, previous destination on all other steps
			thisRouteLeg.Origin = IIF(stepIndex = 1, ;
									This.StartingPoint, ;
									This.GetBookingDataByIndex( This.Item[stepIndex-1].Destination.Index ))
			* destination has just been found
			thisRouteLeg.Destination = This.GetBookingDataByIndex(&CurName..ColumnIndex)
			* save total cost
			thisRouteLeg.Cost = (&CurName..MoveCost+&CurName..WaitCost)
			* save driving cost
			thisRouteLeg.DrivingCost = &CurName..MoveCost
			* save waiting cost
			thisRouteLeg.WaitingCost = &CurName..WaitCost			
			* save time until job would "expire"
			thisRouteLeg.TimeUntilExpires = &CurName..TimeUntilExpires
			* save optimum index
			thisRouteLeg.OptimumIndex = optimumIndex
			* save booking index in the run data cursor
			thisRouteLeg.BookingIndex = checkDestination.Index
			* calculate arrival and departure times
			thisRouteLeg.ArrivalTime = This.CurrentTime + thisRouteLeg.Cost*60
			thisRouteLeg.DepartureTime = thisRouteLeg.ArrivalTime + averageTimeSpentOnOneJob
			* add leg to route
			This.Add(thisRouteLeg) && , TRANSFORM(checkDestination.Index))
			* adjust the current time (if we arrived before the job start as defined by job time frame, in is accounted for in the cost)
			This.CurrentTime = thisRouteLeg.DepartureTime
			This.TotalDrivingCost = This.TotalDrivingCost + thisRouteLeg.DrivingCost
			
*!*				? "Added leg to route: " + TRANSFORM(thisRouteLeg.Origin.Index) + " --> " + ;
*!*											TRANSFORM(thisRouteLeg.Destination.Index) + " || " + ;
*!*											thisRouteLeg.Origin.Address + " --> " + thisRouteLeg.Destination.Address
*!*				? "Arrival time: " + TRANSFORM(thisRouteLeg.ArrivalTime) + "  ||  " + ;
*!*					"Departure time: " + TRANSFORM(thisRouteLeg.DepartureTime)
*!*				* search for next step
*!*				? "Searching for next step: called GetNextStep(" + ;
*!*						TRANSFORM(stepIndex+1) + "," + ;
*!*						TRANSFORM(1) + ", "+ ;
*!*						TRANSFORM(matrixSize)+")"
			
			* This call has to be made outside, otherwise nesting restrictions are exceeded
			* This.GetNextStep(stepIndex + 1, 1, matrixSize)
			RETURN 1
		ENDIF
	ENDFUNC && GetNextStep
	
	FUNCTION GetBookingDataByIndex
		LPARAMETERS cursorIndex
		
		LOCAL oldAlias
		IF !EMPTY(ALIAS())
			oldAlias = ALIAS()
		ENDIF
		
		SELECT (This.RunDataCursor)
		GO RECORD cursorIndex
		
		LOCAL curName
		curName = This.RunDataCursor
		result = NEWOBJECT("Booking")
		WITH result
			.Lat = &curName..Lat
			.Lng = &curName..Lng
			.Index = cursorIndex
			.Start = &curName..Start_Time
			.End = &curName..End_Time
			.Address = ALLTRIM(&curName..CustomerAddress)
		ENDWITH
		
		IF !EMPTY(oldAlias) .AND. oldAlias != This.RunDataCursor
			SELECT (oldAlias)
		ENDIF
		
		RETURN result
	ENDFUNC && GetBookingDataByIndex
	
	PROCEDURE ClearRouteLegs
		LOCAL j
		FOR j = This.Count TO 1 STEP -1
			This.Remove(j)
		ENDFOR
		This.CurrentTime = This.StartingPoint.Start + averageTimeSpentOnOneJob
		This.TotalDrivingCost = 0
	ENDPROC
	
	PROCEDURE ResetOptimumCounters
		LOCAL j
		FOR j=1 TO ALEN(This.OptimumCounters)
			This.OptimumCounters = 1
		ENDFOR
	ENDPROC
	
	FUNCTION GetRunName
		LPARAMETERS runID
		LOCAL result, oldAlias
		
		IF !EMPTY(ALIAS())
			oldAlias = ALIAS()
		ENDIF
		
		SELECT RunNames
		SET ORDER TO RunNumber
		SEEK m.runID
		IF FOUND()
			result = ALLTRIM(RunNames.RunName)
		ELSE
			result = "Not found"
		ENDIF
		
		IF !EMPTY(oldAlias)
			SELECT (oldAlias)
		ENDIF
		
		RETURN result
	ENDFUNC
	
	PROCEDURE SaveAsCursor
		LOCAL cursorName, j
		j = 0
		cursorName = STRTRAN(This.RunName, " ", "_") + "_" + ;
				This.RunID + "_" + ;
				 This.RunDate
		
		CREATE CURSOR &cursorName (Step INTEGER										;
										, Arrival VARCHAR(12)						;
										, Departure VARCHAR(12)						;
										, Mins_Until_Booking_Expires NUMERIC(8,2)	;
										, Address_From VARCHAR(100)					;
										, Address_To VARCHAR(100)					;
										, Drive_Distance_km NUMERIC(8,2)			;
										, Drive_Cost_mins NUMERIC(8,2)				;
										, Wait_Cost_mins NUMERIC(8,2)				;
										, Total_Cost_mins NUMERIC(8,2)				;
										, Destination_Time_Frame_Start DATETIME		;
										, Destination_Time_Frame_End DATETIME)
										
		* * * insert info about starting point										
		INSERT INTO &cursorName VALUES ;
								(0													;
								, TTOC(This.StartingPoint.Start, 2)					;
								, TTOC(This.StartingPoint.Start + averageTimeSpentOnOneJob, 2)	;
								, (This.StartingPoint.End - This.StartingPoint.Start) / 60	;
								, "Puratap Office"	;
								, This.StartingPoint.Address					;
								, 0	;
								, 0	;
								, 0	;
								, 0	;
								, This.StartingPoint.Start	;
								, This.StartingPoint.End)
										
		FOR EACH leg as RouteLeg IN This
			j = j + 1
			* * * insert info about each of the route leg
			INSERT INTO &cursorName VALUES ;
									(j													;
									, TTOC(leg.ArrivalTime - (leg.WaitingCost * 60), 2)	;
									, TTOC(leg.DepartureTime, 2)						;
									, leg.TimeUntilExpires	;
									, leg.Origin.Address	;
									, leg.Destination.Address					;
									, leg.DrivingCost * avgDrivingSpeed / 60	;
									, leg.DrivingCost		;
									, leg.WaitingCost		;
									, leg.Cost				;
									, leg.Destination.Start	;
									, leg.Destination.End)
		ENDFOR
		
		RETURN CursorName
	ENDPROC

	PROCEDURE InitMatrixSize
		LPARAMETERS CursorName

		IF !USED(CursorName)
			? "Cursor containing the run data does not exist"
		ELSE
			SELECT (CursorName)
			This.AddProperty("RunDataCursor", CursorName)
		ENDIF
		
		This.AddProperty("HasFoundRoute", .F.)
		This.matrixSize = RECCOUNT()
		GO TOP
	ENDPROC
	
	PROCEDURE InitOptimumCounters
		This.AddProperty("OptimumCounters("+TRANSFORM(This.matrixSize)+")")
		LOCAL i
		FOR i = 1 TO This.matrixSize
			This.OptimumCounters[i] = 1
		ENDFOR		
	ENDPROC
	
	PROCEDURE InitUtils
		This.Utils = NEWOBJECT("Utils", "C:\backup of m\m (puratap-server)\ccontrol.vcx")
	ENDPROC
	
	PROCEDURE InitCostMatrix
		LPARAMETERS CursorName
		
		* * * Calculating initial cost matrix * * *
		This.AddProperty("CostMatrix_0("+TRANSFORM(This.matrixSize)+","+TRANSFORM(This.matrixSize)+")")
		LOCAL Origin
		Origin = NEWOBJECT("Booking")
		LOCAL Destination
		Destination = NEWOBJECT("Booking")

		LOCAL i, j
		FOR i = 1 TO This.matrixSize
			FOR j = 1 TO This.matrixSize
				
				IF j=i
					This.CostMatrix_0(i,j) = 0
				ELSE
					LOCAL Cost, AdjustedCost
					SELECT (CursorName)
					GO RECORD i
					Origin.Lat = &CursorName..Lat
					Origin.Lng = &CursorName..Lng
					Origin.Start = &CursorName..Start_Time
					Origin.End = &CursorName..End_Time
					GO RECORD j
					Destination.Lat = &CursorName..Lat
					Destination.Lng = &CursorName..Lng
					Destination.Start = &CursorName..Start_Time
					Destination.End = &CursorName..End_Time
					
					m.Cost = This.Utils.GetDistance(Origin.Lat, Origin.Lng, Destination.Lat, Destination.Lng)
					&& adjust cost by increasing the direct geometric distance
					m.AdjustedCost = IIF(m.Cost < 3, m.Cost * shortDistanceMultiplier, ;
													 m.Cost * longDistanceMultiplier )	
					m.AdjustedCost = (m.AdjustedCost / avgDrivingSpeed) * 60 	&& calculate adjusted cost in minutes
					
					* * * if driving there means we arrive after the end of booked time frame -- do not consider

					* * * in other words, if Origin.Start + averageTimeSpentOnOneJob + TimeCostToDestination > Destination.EndTime -- do not consider
					IF Origin.Start + averageTimeSpentOnOneJob + m.AdjustedCost*60 > Destination.End
						* ? "Do not want: " + TRANSFORM(Origin.Start) + " + averageTimeSpentOnOneJob + " + TRANSFORM(m.AdjustedCost) + " = " + ;
							TRANSFORM(Origin.Start + averageTimeSpentOnOneJob + m.AdjustedCost*60)
						* ? "    Destination.End = " + TRANSFORM(Destination.End)
						This.CostMatrix_0(i,j) = 100000
					ELSE
						This.CostMatrix_0(i,j) = m.AdjustedCost
					ENDIF
				ENDIF
				
			ENDFOR && j
		ENDFOR && i
		* * * Finished calculating initial cost matrix * * *
	ENDPROC
	
ENDDEFINE && Class Route

DEFINE CLASS Booking as Custom
	Lat = 0
	Lng = 0
	Index = 0
	Address = ""
	Start = DATETIME()
	End = DATETIME()
ENDDEFINE

DEFINE CLASS RouteLeg as Custom
	Origin = NULL 		&& NEWOBJECT("Booking")
	Destination = NULL 	&& NEWOBJECT("Booking")	
	ArrivalTime = DATETIME()
	DepartureTime = DATETIME()
	TimeUntilExpires = 0
	Cost = 0
	DrivingCost = 0
	WaitingCost = 0
	OptimumIndex = 0
	BookingIndex = 0
ENDDEFINE

PROCEDURE ReleasePublicObjects
	RELEASE myRoute 
	RELEASE avgDrivingSpeed, averageTimeSpentOnOneJob, canBeLate
	RELEASE searchDepth, bookingsSearchDepth, startingPointAsCursorIndex, arriveAtFirstJobAtExactStartTime
	RELEASE startingPointSearchDepth, timeOnJobDecreaseStep, drvSpeedIncreaseStep
	RELEASE maxDrivingSpeed, minTimeOnJob
ENDPROC

 */