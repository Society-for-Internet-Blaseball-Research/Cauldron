using Cauldron;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CauldronVisualizer
{
	internal class VisualizerVm : INotifyPropertyChanged
	{
		private static float COUNT_DELAY = 2.0f;

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		private static JsonSerializerOptions s_diskOptions;
		private static JsonSerializerOptions s_displayOptions;

		public ObservableCollectionEx<GameUpdateVm> GameUpdates { get; set; }

		public int GameUpdatesDelayedCount 
		{
			get { return GameUpdates.Count; }
		}

		public GameUpdateVm SelectedGameUpdate 
		{
			get { return m_selectedGameUpdate; }
			set
			{
				m_selectedGameUpdate = value;
				OnPropertyChanged(nameof(SelectedGameUpdate));
			}
		}
		private GameUpdateVm m_selectedGameUpdate;
		public ObservableCollectionEx<GameEventVm> GameEvents { get; set; }
		public int GameEventsDelayedCount
		{
			get { return GameEvents.Count; }
		}

		public GameEventVm SelectedGameEvent
		{
			get { return m_selectedGameEvent; }
			set
			{
				m_selectedGameEvent = value;
				OnPropertyChanged(nameof(SelectedGameEvent));
			}
		}
		private GameEventVm m_selectedGameEvent;

		private Stopwatch m_stopwatch;

		public bool UpdatesDisabled
		{
			get { return m_updatesDisabled; }
			set
			{
				m_updatesDisabled = value;
				OnPropertyChanged(nameof(UpdatesDisabled));
			}
		}
		private bool m_updatesDisabled;

		public bool EventsDisabled
		{
			get { return m_eventsDisabled; }
			set
			{
				m_eventsDisabled = value;
				OnPropertyChanged(nameof(EventsDisabled));
			}
		}
		private bool m_eventsDisabled;

		public bool LoadSaveEnabled
		{
			get { return m_loadSaveEnabled; }
			set
			{
				m_loadSaveEnabled = value;
				OnPropertyChanged(nameof(LoadSaveEnabled));
}
		}
		private bool m_loadSaveEnabled;

		public ICommand LoadUpdatesCommand => m_loadUpdatesCommand;
		DelegateCommand m_loadUpdatesCommand;

		public ICommand LoadEventsCommand => m_loadEventsCommand;
		DelegateCommand m_loadEventsCommand;

		public ICommand SaveUpdatesCommand => m_saveUpdatesCommand;
		DelegateCommand m_saveUpdatesCommand;

		public ICommand SaveEventsCommand => m_saveEventsCommand;
		DelegateCommand m_saveEventsCommand;

		public ICommand ConvertCommand => m_convertCommand;
		DelegateCommand m_convertCommand;

		public ICommand ShowJsonCommand => m_showJsonCommand;
		DelegateCommand m_showJsonCommand;
		#region Filtering
		public ICommand FilterCommand => m_filterCommand;
		DelegateCommand m_filterCommand;

		public ICommand FilterToGameCommand => m_filterToGameCommand;
		DelegateCommand m_filterToGameCommand;

		public ICommand ClearFilterCommand => m_clearFilterCommand;
		DelegateCommand m_clearFilterCommand;
		public string FilterText
		{
			get { return m_filterText; }
			set 
			{
				m_filterText = value;
				OnPropertyChanged(nameof(FilterText));
			}
		}
		private string m_filterText;

		ICollectionView m_updatesCv;
		ICollectionView m_eventsCv;

		public int FilteredUpdates
		{
			get
			{ 
				return m_updatesCv.Cast<object>().Count();
			} 
		}

		public int FilteredEvents
		{
			get
			{
				return m_eventsCv.Cast<object>().Count();
			}
		}
		#endregion

		Dictionary<string, Team> m_teamLookup;

		static VisualizerVm()
		{
			s_diskOptions = new JsonSerializerOptions();
			s_diskOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

			s_displayOptions = new JsonSerializerOptions();
			s_displayOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			s_displayOptions.WriteIndented = true;
		}

		public VisualizerVm()
		{

			GameUpdates = new ObservableCollectionEx<GameUpdateVm>();
			GameEvents = new ObservableCollectionEx<GameEventVm>();

			m_convertCommand = new DelegateCommand(ConvertUpdates, CanConvertUpdates);
			m_showJsonCommand = new DelegateCommand(ShowJson);

			m_loadUpdatesCommand = new DelegateCommand(ChooseLoadUpdateFile);
			m_loadEventsCommand = new DelegateCommand(ChooseLoadEventsFile);
			m_saveUpdatesCommand = new DelegateCommand(ChooseSaveUpdatesFile);
			m_saveEventsCommand = new DelegateCommand(ChooseSaveEventsFile);

			m_filterCommand = new DelegateCommand(Filter);
			m_filterToGameCommand = new DelegateCommand(FilterToGame);
			m_clearFilterCommand = new DelegateCommand(ClearFilter);
			m_filterText = null;
			m_updatesCv = CollectionViewSource.GetDefaultView(GameUpdates);
			m_updatesCv.Filter = FilterGameUpdates;
			m_eventsCv = CollectionViewSource.GetDefaultView(GameEvents);
			m_eventsCv.Filter = FilterGameEvents;

			LoadSaveEnabled = true;
			EventsDisabled = false;
			UpdatesDisabled = false;
		}

		private void ShowJson(object obj)
		{
			if(obj != null)
			{
				string json = JsonSerializer.Serialize(obj, s_displayOptions);
				MessageBox.Show(json, "Behind the Curtain");
			}
		}

		private bool CanConvertUpdates(object obj)
		{
			return m_updatesCv.Cast<object>().Count() > 0;
		}

		private async Task AsyncConvertUpdates()
		{
			Processor processor = new Processor();
			GameEvents.Clear();
			GameEvents.SupressNotification = true;
			foreach (GameUpdateVm vm in m_updatesCv)
			{
				GameEvent newEvent = processor.ProcessGame(vm.Update, vm.Update.timestamp);
				if (newEvent != null)
				{
					GameEvents.Add(new GameEventVm(newEvent, m_teamLookup));
				}
			}
			GameEvents.SupressNotification = false;
		}

		private async Task ConvertUpdates()
		{
			EventsDisabled = true;
			UpdatesDisabled = true;
			LoadSaveEnabled = false;
			await AsyncConvertUpdates();
			LoadSaveEnabled = true;
			UpdatesDisabled = false;
			EventsDisabled = false;
		}

		private void ConvertUpdates(object obj)
		{
			ConvertUpdates();
		}

		public void BuildTeamLookup()
		{
			// TODO get from endpoint
			m_teamLookup = new Dictionary<string, Team>();

			string teamInfo = File.ReadAllText("Data/teams.json");

			List<Team> allTeams = JsonSerializer.Deserialize<List<Team>>(teamInfo, s_diskOptions);

			foreach(var team in allTeams)
			{
				m_teamLookup[team._id] = team;
			}
		}

		public bool FilterGameUpdates(object item)
		{
			if (m_filterText == null || m_filterText == "") return true;

			GameUpdateVm updateVm = item as GameUpdateVm;

			return updateVm?.Update?._id.Contains(m_filterText) ?? false;
		}

		public bool FilterGameEvents(object item)
		{
			if (m_filterText == null || m_filterText == "") return true;

			GameEventVm eventVm = item as GameEventVm;

			return eventVm?.Event?.gameId.Contains(m_filterText) ?? false;
		}

		public void FilterToGame(object param)
		{
			FilterText = param as string;
			Filter(null);
		}

		public void ClearFilter(object param)
		{
			FilterText = "";
			Filter(null);
		}

		public void Filter(object param)
		{
			m_updatesCv.Refresh();
			m_eventsCv.Refresh();

			OnPropertyChanged(nameof(FilteredEvents));
			OnPropertyChanged(nameof(FilteredUpdates));
			m_convertCommand.RaiseCanExecuteChanged();
		}

		public void ChooseLoadUpdateFile(object param)
		{
			// TODO: not great for non windows
			OpenFileDialog dialog = new OpenFileDialog();
			if(dialog.ShowDialog() == true)
			{
				LoadUpdates(dialog.FileName);
			}
		}

		public void ChooseSaveUpdatesFile(object param)
		{
			SaveFileDialog dialog = new SaveFileDialog();
			if(dialog.ShowDialog() == true)
			{
				SaveUpdates(dialog.FileName);
			}
		}

		public void ChooseLoadEventsFile(object param)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			if (dialog.ShowDialog() == true)
			{
				LoadEvents(dialog.FileName);
			}
		}

		public void ChooseSaveEventsFile(object param)
		{
			SaveFileDialog dialog = new SaveFileDialog();
			if (dialog.ShowDialog() == true)
			{
				SaveEvents(dialog.FileName);
			}
		}

		internal async Task AsyncLoadUpdates(string file)
		{
			try
			{
				GameUpdates.SupressNotification = true;
				m_stopwatch = new Stopwatch();
				m_stopwatch.Start();
				OnPropertyChanged(nameof(GameUpdatesDelayedCount));
				using (StreamReader sr = new StreamReader(file))
				{
					while (!sr.EndOfStream)
					{
						string obj = await sr.ReadLineAsync();
						Update u = JsonSerializer.Deserialize<Update>(obj, s_diskOptions);
						foreach (var s in u.Schedule)
						{
							s.timestamp = u.clientMeta.timestamp;
							GameUpdates.Add(new GameUpdateVm(s, m_teamLookup));
						}
						if (m_stopwatch.Elapsed > TimeSpan.FromSeconds(COUNT_DELAY))
						{
							OnPropertyChanged(nameof(GameUpdatesDelayedCount));
							m_stopwatch.Restart();
						}

					}
				}
				m_stopwatch.Stop();

				GameUpdates.SupressNotification = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Something invalid happened.", "BLASPHEMY");
			}
		}

		private async Task LoadUpdates(string updatesFile)
		{
			GameUpdates.Clear();

			LoadSaveEnabled = false;
			UpdatesDisabled = true;
			Mouse.OverrideCursor = Cursors.Wait;
			await AsyncLoadUpdates(updatesFile);
			Mouse.OverrideCursor = null;
			UpdatesDisabled = false;
			LoadSaveEnabled = true;

			m_convertCommand.RaiseCanExecuteChanged();
			OnPropertyChanged(nameof(FilteredUpdates));
		}

		private void SaveUpdates(string file)
		{
			using (StreamWriter sw = new StreamWriter(file))
			{
				foreach (GameUpdateVm obj in m_updatesCv)
				{
					// Construct a lousy Update
					Update u = new Update();
					u.Schedule = new List<Game>();
					u.Schedule.Add(obj.Update);
					u.clientMeta = new ClientMeta();
					u.clientMeta.timestamp = obj.Update.timestamp;

					string json = JsonSerializer.Serialize(u, s_diskOptions);
					sw.WriteLine(json);
				}
			}
		}

		internal async Task AsyncLoadEvents(string eventsFile)
		{
			try
			{
				GameEvents.SupressNotification = true;
				m_stopwatch = new Stopwatch();
				m_stopwatch.Start();
				OnPropertyChanged(nameof(GameEventsDelayedCount));
				using (StreamReader sr = new StreamReader(eventsFile))
				{
					while (!sr.EndOfStream)
					{
						string obj = await sr.ReadLineAsync();
						GameEvent e = JsonSerializer.Deserialize<GameEvent>(obj, s_diskOptions);
						GameEvents.Add(new GameEventVm(e, m_teamLookup));
						if(m_stopwatch.Elapsed > TimeSpan.FromSeconds(COUNT_DELAY))
						{
							OnPropertyChanged(nameof(GameEventsDelayedCount));
							m_stopwatch.Restart();
						}
					}
				}
				m_stopwatch.Stop();

				GameEvents.SupressNotification = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Something invalid happened.", "BLASPHEMY");
			}
		}

		private async Task LoadEvents(string eventsFile)
		{
			GameEvents.Clear();

			LoadSaveEnabled = false;
			EventsDisabled = true;
			Mouse.OverrideCursor = Cursors.Wait;
			await AsyncLoadEvents(eventsFile);
			Mouse.OverrideCursor = null;
			EventsDisabled = false;
			LoadSaveEnabled = true;

			OnPropertyChanged(nameof(FilteredEvents));
		}

		private void SaveEvents(string file)
		{
			using (StreamWriter sw = new StreamWriter(file))
			{
				foreach (GameEventVm obj in m_eventsCv)
				{
					string json = JsonSerializer.Serialize(obj.Event, s_diskOptions);
					sw.WriteLine(json);
				}
			}
		}

		/// <summary>
		/// TODO: Actually pick the files to load from
		/// </summary>
		public async void Load()
		{
			BuildTeamLookup();

			//string updatesFile = "SampleData/updates.json";
			//string eventsFile = "SampleData/events.json";

			//await LoadUpdates(updatesFile);
			//await LoadEvents(eventsFile);

			this.PropertyChanged += VisualizerVm_PropertyChanged;
		}

		private GameEventVm FindEvent(string gameId, DateTime timestamp)
		{
			foreach(var obj in m_eventsCv)
			{
				GameEventVm vm = obj as GameEventVm;

				if(vm?.Event.gameId == gameId && vm?.Event.firstPerceivedAt <= timestamp && vm?.Event.lastPerceivedAt >= timestamp)
				{
					return vm;
				}
			}

			return null;
		}

		private GameUpdateVm FindUpdate(string gameId, DateTime timestamp)
		{
			foreach (var obj in m_updatesCv)
			{
				GameUpdateVm vm = obj as GameUpdateVm;

				if(vm?.Update._id == gameId && vm?.Update.timestamp == timestamp)
				{
					return vm;
				}
			}

			return null;
		}

		private bool m_responding = false;


		private void VisualizerVm_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch(e.PropertyName)
			{
				case nameof(SelectedGameEvent):
					if(!m_responding && SelectedGameEvent != null)
					{
						m_responding = true;
						SelectedGameUpdate = FindUpdate(SelectedGameEvent.Event.gameId, SelectedGameEvent.Event.firstPerceivedAt);
						m_responding = false;
					}
					break;
				case nameof(SelectedGameUpdate):
					if(!m_responding && SelectedGameUpdate != null)
					{
						m_responding = true;
						SelectedGameEvent = FindEvent(SelectedGameUpdate.Update._id, SelectedGameUpdate.Update.timestamp);
						m_responding = false;
					}
					break;
			}
		}
	}
}
