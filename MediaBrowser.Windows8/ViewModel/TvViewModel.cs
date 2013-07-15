﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Windows8.Model;
using MetroLog;
using Windows.UI.Xaml;

namespace MediaBrowser.Windows8.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class TvViewModel : ViewModelBase
    {
        private readonly ExtendedApiClient _apiClient;
        private readonly NavigationService _navigationService;
        private readonly ILogger _logger;

        public bool ShowDataLoaded, SeasonDataLoaded;
        private BaseItemDto _episode;

        /// <summary>
        /// Initializes a new instance of the TvViewModel class.
        /// </summary>
        public TvViewModel(ExtendedApiClient apiClient, NavigationService navigationService)
        {
            _apiClient = apiClient;
            _navigationService = navigationService;

            Reset();
            PropertyChanged += (sender, args) =>
                                   {
                                       if (args.PropertyName.Equals("SelectedTvSeries"))
                                       {
                                           if (!string.IsNullOrEmpty(SelectedTvSeries.SortName) && SelectedTvSeries.SortName.Equals(Constants.Messages.GetTvInformationMsg))
                                           {
                                               //Utils.CopyItem(item, vm.SelectedTvSeries);
                                               SelectedTvSeries.Name = SelectedTvSeries.Name.Substring(0, SelectedTvSeries.Name.LastIndexOf("(", StringComparison.Ordinal) - 1);
                                           }
                                       }
                                   };
            if (IsInDesignMode)
            {
                SelectedTvSeries = new BaseItemDto
                                       {
                                           Name = "How I Met Your Mother",
                                           Id = "03105446fe99160cbe4bb8961169bd12",
                                           Type = "Series"
                                       };
                var seasons = new[]
                                  {
                                      new BaseItemDto
                                          {
                                              Name = "Season 1",
                                              Id = "dbcc0ea1773acd3437f42ecc747bc322",
                                              Type = "Season",

                                          },
                                      new BaseItemDto
                                          {
                                              Name = "Season 2",
                                              Id = "deb31bfe3b691845bf9b582a2c4b2169",
                                              Type = "Season"
                                          }

                                  };
                var episodes = new[]
                                   {
                                       new BaseItemDto
                                           {
                                               Name = "Belly Full of Turkey",
                                               IndexNumber = 9,
                                               ParentIndexNumber = 1,
                                               Id = "c8bbcbb889058fb7909ed82a09df413c",
                                               Overview =
                                                   "It's Thanksgiving and while Lily and Marshall visit his family in Minnesota, Robin and Ted want to volunteer at a homeless shelter kitchen. Surprisingly, Robin and Ted meet Barney at the kitchen, who has been doing volunteer work for the past few years. At the kitchen, Ted meets Amanda, who seems to be a quite nice girl until everything turns out to be different. Lily has some issues with Marshall's family and is unsure about how her future with Marshall will look like.",
                                               Type = "Episode",
                                               SeriesId = "dbcc0ea1773acd3437f42ecc747bc322",
                                               SeriesName = "How I Met Your Mother"

                                           },
                                       new BaseItemDto
                                           {
                                               Name = "Nothing Good Happens After 2 AM",
                                               IndexNumber = 18,
                                               Type = "Episode",
                                               Id = "e6e4c8acefc3eac543c67831e3ed88f1",
                                               Overview =
                                                   "After having quite a rough day and being a little drunk, Robin calls Ted in the middle of the night and asks him if he wants to come over to her place. Ted, of course, says that he will come. On his way to Robin's apartment, Ted starts to ask himself if it is okay to go to Robin since he still has got his girlfriend Victoria and asks his friends for advice."
                                           }
                                   };
                SeasonsAndRecent[0].Items.Add(new BaseItemDto
                {
                    Name = "How I Met Your Mother",
                    Id = "03105446fe99160cbe4bb8961169bd12",
                    Type = "Series"
                });
                foreach (var season in seasons)
                {
                    SeasonsAndRecent[1].Items.Add(season);
                }
                SelectedSeason = seasons[0];
                Episodes = episodes.ToList();
                foreach (var episode in episodes)
                {
                    SeasonsAndRecent[2].Items.Add(episode);
                }
                SelectedEpisode = episodes[0];

                _logger = new DesignLogger();
            }
            else
            {
                WireMessages();
                WireCommands();
                _logger = LogManagerFactory.DefaultLogManager.GetLogger<TvViewModel>();
            }
        }

        private void Reset()
        {
            RecentItems = new ObservableCollection<BaseItemDto>();
            Episodes = new List<BaseItemDto>();
            Seasons = new List<BaseItemDto>();
            SeasonsAndRecent = new List<Group<BaseItemDto>>
                                   {
                                       new Group<BaseItemDto>{ Title = ((char)8203).ToString()},
                                       new Group<BaseItemDto>{ Title = "Seasons"},
                                       new Group<BaseItemDto>{ Title = "Recent"}
                                   };
        }

        private void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage>(this, async m =>
            {
                if (m.Notification.Equals(Constants.Messages.TvShowPageLoadedMsg))
                {
                    if (_navigationService.IsNetworkAvailable && !ShowDataLoaded)
                    {
                        var id = (string) m.Sender;
                        if (SelectedTvSeries != null && SelectedTvSeries.Id == id)
                        {
                            SeasonsAndRecent[0].Items.Add(SelectedTvSeries);
                            ProgressText = "Getting show information...";
                            ProgressVisibility = Visibility.Visible;

                            bool seasonsLoaded = await GetSeasons();

                            ProgressText = "Getting recent items...";

                            bool recentItems = await GetRecentItems();

                            ProgressText = string.Empty;
                            ProgressVisibility = Visibility.Collapsed;
                            ShowDataLoaded = (seasonsLoaded && recentItems);

                        }
                    }
                }
                if (m.Notification.Equals(Constants.Messages.ClearTvSeriesMsg))
                {
                    Reset();
                }
                if (m.Notification.Equals(Constants.Messages.TvSeasonSelectedMsg))
                {
                    SelectedSeason = (BaseItemDto)m.Sender;
                }
                if (m.Notification.Equals(Constants.Messages.TvSeasonPageLoadedMsg))
                {
                    if (_navigationService.IsNetworkAvailable && !SeasonDataLoaded)
                    {
                        if (SelectedSeason != null)
                        {
                            ProgressVisibility = Visibility.Visible;
                            ProgressText = "Getting episodes...";

                            SeasonDataLoaded = await GetEpisodes();

                            ProgressText = string.Empty;
                            ProgressVisibility = Visibility.Collapsed;
                        }
                    }
                }
                if (m.Notification.Equals(Constants.Messages.TvEpisodePageLoadedMsg))
                {
                    if (SelectedSeason == null && _navigationService.IsNetworkAvailable)
                    {
                        ProgressText = "Getting extra information...";
                        ProgressVisibility = Visibility.Visible;

                        SelectedSeason = new BaseItemDto
                        {
                            Id = Episode.ParentId
                        };

                        await GetEpisodes();

                        ProgressText = string.Empty;
                        ProgressVisibility = Visibility.Collapsed;
                    }
                    SelectedEpisode = Episodes[Episode.IndexNumber.Value - 1];

                }
                if (m.Notification.Equals(Constants.Messages.ClearEpisodesMsg))
                {
                    Episodes = null;
                    SelectedSeason = null;
                    SeasonDataLoaded = false;
                }
                if (m.Notification.Equals(Constants.Messages.ClearEverythingMsg))
                {
                    Reset();
                }
            });
        }

        private async Task<bool> GetEpisodes()
        {
            try
            {
                var query = new ItemQuery
                                {
                                    UserId = App.Settings.LoggedInUser.Id,
                                    ParentId = SelectedSeason.Id,
                                    Fields = new[]
                                                 {
                                                     ItemFields.SeriesInfo,
                                                     ItemFields.ParentId,
                                                     ItemFields.DateCreated, 
                                                     ItemFields.Overview, 
                                                 }
                                };

                _logger.Info("Getting episodes for Season [{0}] ({1}) of TV Show [{2}] ({3})", SelectedTvSeries.Name, SelectedTvSeries.Id);

                var episodes = await _apiClient.GetItemsAsync(query);
                                                                                                                           
                Episodes = episodes.Items.OrderBy(x => x.IndexNumber).ToList();
                return true;
            }
            catch (HttpException ex)
            {
                _logger.Fatal(ex.Message, ex);
                return false;
            }
        }

        private async Task<bool> GetEpisode()
        {
            try
            {
                _logger.Info("Getting information for episode [{0}] ({1})", SelectedEpisode.Name, SelectedEpisode.Id);

                var episode = await _apiClient.GetItemAsync(SelectedEpisode.Id, App.Settings.LoggedInUser.Id);

                return true;
            }
            catch (HttpException ex)
            {
                _logger.Fatal(ex.Message, ex);

                return false;
            }
        }

        private async Task<bool> GetRecentItems()
        {
            try
            {
                var query = new ItemQuery
                {
                    UserId = App.Settings.LoggedInUser.Id,
                    ParentId = SelectedTvSeries.Id,
                    Filters = new [] {ItemFilter.IsRecentlyAdded, ItemFilter.IsNotFolder, },
                    Fields = new[]
                                                 {
                                                     ItemFields.SeriesInfo,
                                                     ItemFields.ParentId
                                                 },
                                                 Recursive = true
                };

                _logger.Info("Getting recent items for TV Show [{0}] ({1})", SelectedTvSeries.Name, SelectedTvSeries.Id);

                var recent = await _apiClient.GetItemsAsync(query);
                RecentItems.Clear();
                var items = recent.Items.OrderBy(x => x.DateCreated)
                    .Take(12)
                    .ToList();
                foreach (var item in items)
                {
                    SeasonsAndRecent[2].Items.Add(item);
                }
                return true;
            }
            catch (HttpException ex)
            {
                _logger.Fatal(ex.Message, ex);
                return false;
            }
        }

        private async Task<bool> GetSeasons()
        {
            try
            {
                var query = new ItemQuery
                {
                    UserId = App.Settings.LoggedInUser.Id,
                    ParentId = SelectedTvSeries.Id,
                    Fields = new[]
                                                 {
                                                     ItemFields.SeriesInfo,
                                                     ItemFields.ParentId
                                                 }
                };

                _logger.Info("Getting seasons for TV Show [{0}] ({1})", SelectedTvSeries.Name, SelectedTvSeries.Id);

                var seasons = await _apiClient.GetItemsAsync(query);
                if (!string.IsNullOrEmpty(SelectedTvSeries.SortName) && SelectedTvSeries.SortName.Equals(Constants.Messages.GetTvInformationMsg))
                {
                    SelectedTvSeries = await _apiClient.GetItemAsync(SelectedTvSeries.Id, App.Settings.LoggedInUser.Id);
                }
                foreach (var season in seasons.Items.OrderBy(x => x.IndexNumber))
                {
                    SeasonsAndRecent[1].Items.Add(season);
                }

                // TODO: Cast and crew
                
                return true;
            }
            catch (HttpException ex)
            {
                _logger.Fatal(ex.Message, ex);
                return false;
            }
        }

        private void WireCommands()
        {

        }

        public string ProgressText { get; set; }
        public Visibility ProgressVisibility { get; set; }

        //public List<Group<BaseItemPerson>> CastAndCrew { get; set; }
        public RelayCommand<BaseItemDto> NavigateToPage { get; set; }

        public List<Group<BaseItemDto>> SeasonsAndRecent { get; set; }
        public BaseItemDto SelectedTvSeries { get; set; }
        public List<BaseItemDto> Seasons { get; set; }
        public List<BaseItemDto> Episodes { get; set; }
        public BaseItemDto SelectedEpisode { get; set; }
        public BaseItemDto SelectedSeason { get; set; }
        public BaseItemDto Episode { get; set; }
        public ObservableCollection<BaseItemDto> RecentItems { get; set; }
        public BaseItemDto DummyFolder { get; set; }
    }
}