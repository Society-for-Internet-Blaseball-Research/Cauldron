using Cauldron;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace CauldronVisualizer
{
	internal class VisualizerVm : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		private JsonSerializerOptions m_serializerOptions;

		public ObservableCollection<GameUpdateVm> GameUpdates { get; set; }
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
		public ObservableCollection<GameEventVm> GameEvents { get; set; }
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

		#region Filtering
		public ICommand FilterCommand => m_filterCommand;
		DelegateCommand m_filterCommand;

		public ICommand LoadUpdatesCommand => m_loadUpdatesCommand;
		DelegateCommand m_loadUpdatesCommand;

		public ICommand LoadEventsCommand => m_loadEventsCommand;
		DelegateCommand m_loadEventsCommand;

		public ICommand SaveUpdatesCommand => m_saveUpdatesCommand;
		DelegateCommand m_saveUpdatesCommand;

		public ICommand SaveEventsCommand => m_saveEventsCommand;
		DelegateCommand m_saveEventsCommand;

		string m_filterString;

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

		public VisualizerVm()
		{
			m_serializerOptions = new JsonSerializerOptions();
			m_serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

			GameUpdates = new ObservableCollection<GameUpdateVm>();
			GameEvents = new ObservableCollection<GameEventVm>();

			m_filterCommand = new DelegateCommand(Filter);
			m_filterString = null;

			m_loadUpdatesCommand = new DelegateCommand(ChooseLoadUpdateFile);
			m_loadEventsCommand = new DelegateCommand(ChooseLoadEventsFile);
			m_saveUpdatesCommand = new DelegateCommand(ChooseSaveUpdatesFile);
			m_saveEventsCommand = new DelegateCommand(ChooseSaveEventsFile);

			m_updatesCv = CollectionViewSource.GetDefaultView(GameUpdates);
			m_updatesCv.Filter = FilterGameUpdates;

			m_eventsCv = CollectionViewSource.GetDefaultView(GameEvents);
			m_eventsCv.Filter = FilterGameEvents;
		}

		public void BuildTeamLookup()
		{
			// TODO get from endpoint
			m_teamLookup = new Dictionary<string, Team>();

			string teamInfo = File.ReadAllText("Data/teams.json");

			List<Team> allTeams = JsonSerializer.Deserialize<List<Team>>(teamInfo, m_serializerOptions);

			foreach(var team in allTeams)
			{
				m_teamLookup[team._id] = team;
			}
		}

		public bool FilterGameUpdates(object item)
		{
			if (m_filterString == null || m_filterString == "") return true;

			GameUpdateVm updateVm = item as GameUpdateVm;

			return updateVm?.Update?._id.Contains(m_filterString) ?? false;
		}

		public bool FilterGameEvents(object item)
		{
			if (m_filterString == null || m_filterString == "") return true;

			GameEventVm eventVm = item as GameEventVm;

			return eventVm?.Event?.gameId.Contains(m_filterString) ?? false;
		}

		public void Filter(object param)
		{
			m_filterString = param as string;

			CollectionViewSource.GetDefaultView(GameUpdates).Refresh();
			CollectionViewSource.GetDefaultView(GameEvents).Refresh();
			OnPropertyChanged(nameof(FilteredEvents));
			OnPropertyChanged(nameof(FilteredUpdates));
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
			using (StreamReader sr = new StreamReader(file))
			{
				while (!sr.EndOfStream)
				{
					string obj = await sr.ReadLineAsync();
					Update u = JsonSerializer.Deserialize<Update>(obj, m_serializerOptions);
					foreach (var s in u.Schedule)
					{
						s.timestamp = u.clientMeta.timestamp;
						GameUpdates.Add(new GameUpdateVm(s, m_teamLookup));
					}
				}
			}
		}

		private async Task LoadUpdates(string updatesFile)
		{
			GameUpdates.Clear(); 

			Mouse.OverrideCursor = Cursors.Wait;
			await AsyncLoadUpdates(updatesFile);
			Mouse.OverrideCursor = null;

			OnPropertyChanged(nameof(FilteredUpdates));
		}

		private void SaveUpdates(string file)
		{
			StreamWriter sw = new StreamWriter(file);
			var view = CollectionViewSource.GetDefaultView(GameUpdates);
			foreach(GameUpdateVm obj in view)
			{
				string json = JsonSerializer.Serialize(obj.Update, m_serializerOptions);
				sw.WriteLine(json);
			}
		}

		internal async Task AsyncLoadEvents(string eventsFile)
		{
			using (StreamReader sr = new StreamReader(eventsFile))
			{
				while (!sr.EndOfStream)
				{
					string obj = await sr.ReadLineAsync();
					GameEvent e = JsonSerializer.Deserialize<GameEvent>(obj, m_serializerOptions);
					GameEvents.Add(new GameEventVm(e, m_teamLookup));
				}
			}
		}

		private async Task LoadEvents(string eventsFile)
		{
			GameEvents.Clear();

			Mouse.OverrideCursor = Cursors.Wait;
			await AsyncLoadEvents(eventsFile);
			Mouse.OverrideCursor = null;

			OnPropertyChanged(nameof(FilteredEvents));
		}

		private void SaveEvents(string file)
		{
			StreamWriter sw = new StreamWriter(file);
			var view = CollectionViewSource.GetDefaultView(GameEvents);
			foreach (GameEventVm obj in view)
			{
				string json = JsonSerializer.Serialize(obj.Event, m_serializerOptions);
				sw.WriteLine(json);
			}
		}

		/// <summary>
		/// TODO: Actually pick the files to load from
		/// </summary>
		public async void Load()
		{
			BuildTeamLookup();

			string updatesFile = "SampleData/updates.json";
			string eventsFile = "SampleData/events.json";

			await LoadUpdates(updatesFile);
			await LoadEvents(eventsFile);

			this.PropertyChanged += VisualizerVm_PropertyChanged;
		}

		private GameEventVm FindEvent(string gameId, DateTime timestamp)
		{
			foreach(var obj in CollectionViewSource.GetDefaultView(GameEvents))
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
			foreach (var obj in CollectionViewSource.GetDefaultView(GameUpdates))
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
