using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.Controls;
using TEKLauncher.Servers;
using TEKLauncher.Steam;
using TEKLauncher.Tabs;

namespace TEKLauncher.Windows;

/// <summary>Window that represents a Steam client task for a DLC or a mod.</summary>
partial class UpdaterWindow : TEKWindow
{
	/// <summary>New status of DLC/mod that will be set when the window is closed; -1 means that last status should be kept.</summary>
	int _newStatus = -1;
	unsafe TEKSteamClient.AmItemDesc* _desc = null;
	/// <summary>Indicator of Steam client task's current stage.</summary>
	StageIndicator? _currentStage;
	/// <summary>Steam client task thread.</summary>
	Thread _taskThread;
	/// <summary>Indicates whether validation should be preferred over update.</summary>
	bool _validate;
	/// <summary>The last status that DLC/mod had before creating the window.</summary>
	readonly int _lastStatus;
	/// <summary>Initializes a new Updater window for specified item.</summary>
	/// <param name="item">The item (DLC, mod or list of mods) to be processed by the window's Steam client task.</param>
	/// <param name="validate">Indicates whether validation should be preferred over update.</param>
	UpdaterWindow(object item, bool validate)
	{
		InitializeComponent();
		_validate = validate;
		_taskThread = new(TaskProcedure);
		Item = item;
	}
	/// <summary>Initializes a new Updater window for specified DLC.</summary>
	/// <param name="dlc">The DLC to be processed by the window's Steam client task.</param>
	/// <param name="validate">Indicates whether validation should be preferred over update.</param>
	internal UpdaterWindow(DLC dlc, bool validate) : this((object)dlc, validate)
	{
		_lastStatus = (int)dlc.CurrentStatus;
		dlc.CurrentStatus = DLC.Status.Updating;
		Title = string.Format(LocManager.GetString(LocCode.SteamUpdater), dlc.Name);
	}
	/// <summary>Initializes a new Updater window for specified mod.</summary>
	/// <param name="modDetails">Details of the mod to be processed by the window's Steam client task.</param>
	/// <param name="validate">Indicates whether validation should be preferred over update.</param>
	internal UpdaterWindow(in Mod.ModDetails modDetails, bool validate) : this((object)modDetails, validate)
	{
		lock (Mod.List)
		{
			ulong id = modDetails.Id;
			var mod = Mod.List.Find(m => m.Id == id);
			if (mod is not null)
			{
				_lastStatus = (int)mod.CurrentStatus;
				mod.CurrentStatus = Mod.Status.Updating;
			}
		}
		Title = string.Format(LocManager.GetString(LocCode.SteamUpdater), modDetails.Name);
	}
	/// <summary>Initializes a new Updater window for validating specified list of mods.</summary>
	/// <param name="modIds">Array of IDs of the mods that will be validated by the window.</param>
	internal UpdaterWindow(ulong[] modIds) : this(modIds, true)
	{
		if (modIds.Length == 0)
		{
			Close();
			return;
		}
		_newStatus = 0; //_newStatus will be used as index into modDetails
	}
	/// <summary>Gets a value that indicates whether there is an active Steam client task initiated by this window.</summary>
	public bool IsSteamTaskActive => _taskThread.IsAlive;
	/// <summary>The item (DLC, mod or list of mods) to be processed by the window's Steam client task.</summary>
	public object Item { get; private set; }
	/// <summary>Checks whether the window is eligible for closing and sets new DLC/mod status if it is.</summary>
	unsafe void ClosingHandler(object sender, CancelEventArgs e)
	{
		if (_taskThread.IsAlive)
		{
			if (App.ShuttingDown)
				TEKSteamClient.AppManager.PauseJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(_desc));
			else
			{
				e.Cancel = true;
				Messages.Show("Warning", LocManager.GetString(LocCode.PauseRequired));
				return;
			}
		}
		else if (Item is DLC dlc)
			dlc.CurrentStatus = _newStatus == -1 ? (DLC.Status)_lastStatus : (DLC.Status)_newStatus;
		else if (Item is Mod.ModDetails details)
			lock (Mod.List)
			{
				var mod = Mod.List.Find(m => m.Id == details.Id);
				if (mod is not null)
					mod.CurrentStatus = _newStatus == -1 ? (Mod.Status)_lastStatus : (Mod.Status)_newStatus;
			}
	}
	/// <summary>Perform game running check and starts the task thread.</summary>
	void LoadedHandler(object sender, RoutedEventArgs e)
	{
		if (Game.IsRunning)
		{
			Status.Text = LocManager.GetString(LocCode.UpdateFailGameRunning);
			Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
			PauseRetryButton.Content = LocManager.GetString(LocCode.Retry);
			PauseRetryButton.Tag = FindResource("Retry");
		}
		else
		{
			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
			_taskThread.Start();
		}
	}
	/// <summary>Cancels or restarts Steam client task.</summary>
	unsafe void PauseRetry(object sender, RoutedEventArgs e)
	{
		if ((string)PauseRetryButton.Content == LocManager.GetString(LocCode.Pause))
		{
			PauseRetryButton.IsEnabled = false;
			TEKSteamClient.AppManager.PauseJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(_desc));
		}
		else if (Game.IsRunning)
		{
			Status.Text = LocManager.GetString(LocCode.UpdateFailGameRunning);
			Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
		}
		else
		{
			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
			_currentStage = null;
			Stages.Children.Clear();
			_taskThread = new(TaskProcedure);
			PauseRetryButton.Content = LocManager.GetString(LocCode.Pause);
			PauseRetryButton.Tag = FindResource("Pause");
			_taskThread.Start();
		}
	}
	string? _spcMsg = null;
	void UpdHandler(ref TEKSteamClient.AmItemDesc desc, TEKSteamClient.AmUpdType upd_mask)
	{
		if (upd_mask.HasFlag(TEKSteamClient.AmUpdType.DeltaCreated))
		{
			long diskFreeSpace = WinAPI.GetDiskFreeSpace($@"{Game.Path}\tek-sc-data") + 20971520; //Add 20 MB to take NTFS entries into account
			long reqSpace = TEKSteamClient.DeltaEstimateDiskSpace(desc.Job.Delta);
			if (diskFreeSpace < reqSpace)
			{
				_spcMsg = string.Format(LocManager.GetString(LocCode.NotEnoughSpace), LocManager.BytesToString(reqSpace));
				TEKSteamClient.AppManager.PauseJob(ref desc);
			}
		}
		if (upd_mask.HasFlag(TEKSteamClient.AmUpdType.Stage))
		{
			var stage = desc.Job.Stage;
			Dispatcher.Invoke(() =>
			{
				LocCode code = LocCode.NA;
				switch (stage)
				{
					case TEKSteamClient.AmJobStage.FetchingData:
						ProgressBar.Reset(Controls.ProgressBar.Mode.None);
						code = LocCode.FetchingData;
						break;
					case TEKSteamClient.AmJobStage.DwManifest:
						ProgressBar.Reset(Controls.ProgressBar.Mode.Binary);
						code = LocCode.DownloadingManifest;
						break;
					case TEKSteamClient.AmJobStage.DwPatch:
						ProgressBar.Reset(Controls.ProgressBar.Mode.Binary);
						code = LocCode.DownloadingPatch;
						break;
					case TEKSteamClient.AmJobStage.Verifying:
						ProgressBar.Reset(Controls.ProgressBar.Mode.Percentage);
						code = LocCode.Validating;
						break;
					case TEKSteamClient.AmJobStage.Downloading:
						ProgressBar.Reset(Controls.ProgressBar.Mode.Binary);
						code = LocCode.DownloadingFiles;
						break;
					case TEKSteamClient.AmJobStage.Pathcing:
						ProgressBar.Reset(Controls.ProgressBar.Mode.Numbers);
						code = LocCode.Patching;
						break;
					case TEKSteamClient.AmJobStage.Installing:
						ProgressBar.Reset(Controls.ProgressBar.Mode.Numbers);
						code = LocCode.InstallingFiles;
						break;
					case TEKSteamClient.AmJobStage.Deleting:
						ProgressBar.Reset(Controls.ProgressBar.Mode.Numbers);
						code = LocCode.Deleting;
						break;
					case TEKSteamClient.AmJobStage.Finalizing:
						ProgressBar.Reset(Controls.ProgressBar.Mode.None);
						code = LocCode.Finalizing;
						break;
				}
				Status.Text = LocManager.GetString(code);
				Status.Foreground = Brushes.Yellow;
				_currentStage?.Finish(true);
				_currentStage = new(code);
				Stages.Children.Add(_currentStage);
			});
		}
		if (upd_mask.HasFlag(TEKSteamClient.AmUpdType.Progress))
		{
			var current = desc.Job.Stage switch
			{
				TEKSteamClient.AmJobStage.Verifying
				or TEKSteamClient.AmJobStage.Downloading
				or TEKSteamClient.AmJobStage.Installing => Interlocked.Read(ref desc.Job.ProgressCurrent),
				_ => desc.Job.ProgressCurrent
			};
			var total = desc.Job.ProgressTotal;
			Dispatcher.Invoke(delegate
			{
				ProgressBar.Update(current, total);
				TaskbarItemInfo.ProgressValue = ProgressBar.Ratio;
			});

		}
	}
	/// <summary>Executes Steam client tasks and catches its exceptions.</summary>
	/// <exception cref="AggregateException">And error occurred in Steam client task.</exception>
	unsafe void TaskProcedure()
	{
		uint depotId = Item is DLC dlc ? dlc.DepotId : 346110;
		if (Item is ulong[] modIds)
		{
			Status.Text = LocManager.GetString(LocCode.RequestingModDetails);
			Status.Foreground = Brushes.Yellow;
			var details = Steam.CM.Client.GetModDetails(modIds);
			if (details.Length == 0)
				throw new SteamException(LocManager.GetString(LocCode.RequestingModDetailsFail));
			Item = details;
		}
		TEKSteamClient.Error res = new();
		if (Item is Mod.ModDetails[] array)
		{
			for (var itemId = new TEKSteamClient.ItemId { AppId = 346110, DepotId = 346110 }; _newStatus < array.Length; _newStatus++)
			{
				var details = array[_newStatus];
				Dispatcher.Invoke(delegate
				{
					Title = string.Format(LocManager.GetString(LocCode.SteamUpdater), $"{details.Name} [{_newStatus + 1}/{array.Length}]");
					TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
					_currentStage = null;
					Stages.Children.Clear();
				});
				itemId.WorkshopItemId = details.Id;
				res = TEKSteamClient.AppMng!.RunJob(in itemId, 0, _validate, UpdHandler, out _desc);
				if (!res.Success && res.Primary != 82)
					break;
				Dispatcher.Invoke(delegate
				{
					ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
					Status.Text = res.Primary == 82 ? string.Format(LocManager.GetString(LocCode.AlreadyUpToDate), details.Name) : string.Format(LocManager.GetString(LocCode.ModInstallSuccess), details.Name);
					Status.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
				});
				lock (Mod.List)
				{
					if (Mod.List.Find(m => m.Id == details.Id) is null)
						Mod.List.Add(new(details.Id) { Details = details });
					Dispatcher.InvokeAsync(delegate
					{
						var mainWindow = (MainWindow)Application.Current.MainWindow;
						if (mainWindow.TabFrame.Child is ModsTab modsTab)
							modsTab.ReloadList();
						else if (mainWindow.TabFrame.Child is ClusterTab clusterTab)
							foreach (ServerItem item in clusterTab.Servers.Children)
							{
								var server = (Server)item.DataContext;
								if (Array.IndexOf(server.ModIds, details.Id) >= 0)
									foreach (ServerModItem modItem in item.Mods.Children)
										if (modItem.Id == details.Id)
										{
											modItem.SetInstalled();
											break;
										}
							}
					});
				}
			}
		}
		else
		{
			var itemId = new TEKSteamClient.ItemId { AppId = 346110, DepotId = depotId, WorkshopItemId = Item is Mod.ModDetails details ? details.Id : 0 };
			ulong preAquaticaId = depotId switch
			{
				346114 => 5573587184752106093,
				375351 => 8265777340034981821,
				375354 => 7952753366101555648,
				375357 => 1447242805278740772,
				473851 => 2551727096735353757,
				473854 => 847717640995143866,
				473857 => 1054814513659387220,
				1318685 => 8189621638927588129,
				1691801 => 3147973472387347535,
				1887561 => 580528532335699271,
				_ => 0
			};
			res = TEKSteamClient.AppMng!.RunJob(in itemId, Settings.PreAquatica ? preAquaticaId : 0, _validate, UpdHandler, out _desc);
			if (res.Success || res.Primary == 82)
			{
				_newStatus = depotId == 346110 ? (int)Mod.Status.Installed : (Settings.PreAquatica ? (_desc->CurrentManifestId == preAquaticaId ? (int)DLC.Status.Installed : (int)DLC.Status.UpdateAvailable) : (int)DLC.Status.Installed);
				Dispatcher.Invoke(delegate
				{
					ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
					if (Item is Mod.ModDetails details)
						Status.Text = res.Primary == 82 ? string.Format(LocManager.GetString(LocCode.AlreadyUpToDate), details.Name) : string.Format(LocManager.GetString(LocCode.ModInstallSuccess), details.Name);
					else
						Status.Text = res.Primary == 82 ? string.Format(LocManager.GetString(LocCode.AlreadyUpToDate), ((DLC)Item).Name) : LocManager.GetString(LocCode.UpdateFinished);
					Status.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
				});
				if (Item is Mod.ModDetails detail)
					lock (Mod.List)
					{
						if (Mod.List.Find(m => m.Id == detail.Id) is null)
							Mod.List.Add(new(detail.Id) { Details = detail });
						Dispatcher.InvokeAsync(delegate
						{
							var mainWindow = (MainWindow)Application.Current.MainWindow;
							if (mainWindow.TabFrame.Child is ModsTab modsTab)
								modsTab.ReloadList();
							else if (mainWindow.TabFrame.Child is ClusterTab clusterTab)
								foreach (ServerItem item in clusterTab.Servers.Children)
								{
									var server = (Server)item.DataContext;
									if (Array.IndexOf(server.ModIds, detail.Id) >= 0)
										foreach (ServerModItem modItem in item.Mods.Children)
											if (modItem.Id == detail.Id)
											{
												modItem.SetInstalled();
												break;
											}
								}
						});
					}
			}
		}
		if (res.Primary == 65)
		{
			Dispatcher.Invoke(delegate
			{
				if (_spcMsg is null)
				{
					ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
					Status.Text = LocManager.GetString(LocCode.OperationPaused);
					Status.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
					TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
				}
				else
				{
					ProgressBar.Reset(Controls.ProgressBar.Mode.None);
					Status.Text = _spcMsg;
					_spcMsg = null;
					Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
					TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
				}
				_currentStage?.Finish(false);
				PauseRetryButton.IsEnabled = true;
				PauseRetryButton.Content = LocManager.GetString(LocCode.Retry);
				PauseRetryButton.Tag = FindResource("Retry");
			});
		}
		else if (!res.Success && res.Primary != 82)
		{
			if (_desc->Job.Stage == TEKSteamClient.AmJobStage.Pathcing && res.Type == 3 && res.Primary == 6 && (res.Auxiliary == 2 || res.Auxiliary == 38))
			{
				if (res.Uri != 0)
					Marshal.FreeHGlobal(res.Uri);
				res = TEKSteamClient.AppMng!.CancelJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(_desc));
				if (res.Success)
				{
					Dispatcher.Invoke(delegate
					{
						ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
						_currentStage = null;
						Stages.Children.Clear();
					});
					_validate = true;
					TaskProcedure();
					return;
				}
			}
			Dispatcher.Invoke(delegate
			{
				ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
				Status.Text = res.Message;
				if (res.Uri != 0)
					Marshal.FreeHGlobal(res.Uri);
				Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
				TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
				_currentStage?.Finish(false);
				PauseRetryButton.IsEnabled = true;
				PauseRetryButton.Content = LocManager.GetString(LocCode.Retry);
				PauseRetryButton.Tag = FindResource("Retry");
			});
		}
		else
		{
			Dispatcher.Invoke(delegate
			{
				TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
				_currentStage?.Finish(true);
				PauseRetryButton.IsEnabled = false;
			});
		}
	}
	/// <summary>Force cancels Steam client task if it's running.</summary>
	public unsafe void AbortTask()
	{
		if (_taskThread.IsAlive)
			TEKSteamClient.AppManager.PauseJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(_desc));
	}
}